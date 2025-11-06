using System.Diagnostics;
using HullSizingService.Data;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Polly;
using Polly.Extensions.Http;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Shared.Middleware;
using Shared.Services;

// Bootstrap logger for startup errors
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting HullSizingService...");

try
{
    var builder = WebApplication.CreateBuilder(args);

    // Configure Serilog
    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .WriteTo.Console(new CompactJsonFormatter())
            .WriteTo.File(
                path: "logs/hullsizingservice-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 10_485_760,
                rollOnFileSizeLimit: true
            );
    });

    // [STARTUP] Log environment and configuration
    Console.WriteLine($"[STARTUP] ===============================================");
    Console.WriteLine($"[STARTUP] HullSizingService Starting");
    Console.WriteLine($"[STARTUP] ===============================================");
    Console.WriteLine($"[STARTUP] Environment: {builder.Environment.EnvironmentName}");
    Console.WriteLine($"[STARTUP] Machine: {Environment.MachineName}");
    Console.WriteLine($"[STARTUP] OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
    Console.WriteLine($"[STARTUP] Framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

    // Log key configuration (redact sensitive data)
    var connString = builder.Configuration.GetConnectionString("DefaultConnection");
    var safeConnString = connString ?? "NOT SET";
    Console.WriteLine($"[STARTUP] Connection String: {safeConnString}");
    Console.WriteLine($"[STARTUP] Services:DataService: {builder.Configuration["Services:DataService"] ?? "NOT SET"}");
    Console.WriteLine($"[STARTUP] ===============================================");

    Log.Information("[STARTUP] HullSizingService starting - Environment: {Environment}", builder.Environment.EnvironmentName);

    // Add services to the container
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
            options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        options.ReportApiVersions = true;
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);
        options.ApiVersionReader = new Asp.Versioning.UrlSegmentApiVersionReader();
    }).AddMvc();

    // Database - Use snake_case naming convention for PostgreSQL
    builder.Services.AddDbContext<SizingDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            npgsqlOptions.CommandTimeout(60);
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
            npgsqlOptions.MaxBatchSize(100);
            // Explicit migration history table in sizing schema
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "sizing");
        })
        .UseSnakeCaseNamingConvention()
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .EnableDetailedErrors(builder.Environment.IsDevelopment());
    });

    // Memory Cache for JWT key caching and water properties caching
    builder.Services.AddMemoryCache();

    // HttpClient for service-to-service calls
    builder.Services.AddHttpClient();
    builder.Services.AddHttpContextAccessor();

    // DataService HTTP Client with Polly resilience policies
    builder.Services.AddHttpClient<HullSizingService.Services.Integration.IDataServiceClient, HullSizingService.Services.Integration.DataServiceClient>(client =>
    {
        var dataServiceUrl = builder.Configuration["Services:DataService"];
        if (string.IsNullOrEmpty(dataServiceUrl))
        {
            throw new InvalidOperationException("Services:DataService configuration is missing");
        }
        client.BaseAddress = new Uri(dataServiceUrl);
        client.Timeout = TimeSpan.FromSeconds(30); // Overall timeout (allows for retries)
    })
    .AddPolicyHandler(GetRetryPolicy())
    .AddPolicyHandler(GetCircuitBreakerPolicy())
    .AddPolicyHandler(GetTimeoutPolicy());

    // Water Properties Service with caching
    builder.Services.AddScoped<HullSizingService.Services.IWaterPropertiesService, HullSizingService.Services.WaterPropertiesService>();
    Log.Information("Water properties service registered with 12h cache and stale fallback");

    // JWT Service - Use LocalJwtService in development, CognitoJwtService in production
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddSingleton<IJwtService, LocalJwtService>();
        Log.Information("Using LocalJwtService for development");
    }
    else
    {
        builder.Services.AddSingleton<IJwtService, CognitoJwtService>();
        Log.Information("Using CognitoJwtService for production");
    }

    // Unit Conversion Service
    builder.Services.AddSingleton<NavArch.UnitConversion.Services.IUnitConverter>(sp =>
        new NavArch.UnitConversion.Services.UnitConverter(null));
    Log.Information("Unit conversion service registered with default config path");

    // Mission Case Service
    builder.Services.AddScoped<HullSizingService.Services.IMissionCaseService, HullSizingService.Services.MissionCaseService>();
    Log.Information("Mission case service registered");

    // Sizing Run Service
    builder.Services.AddScoped<HullSizingService.Services.ISizingRunService, HullSizingService.Services.SizingRunService>();
    Log.Information("Sizing run service registered");

    // Candidate Design Service
    builder.Services.AddScoped<HullSizingService.Services.ICandidateDesignService, HullSizingService.Services.CandidateDesignService>();
    Log.Information("Candidate design service registered");

    // Hull Family Service
    builder.Services.AddScoped<HullSizingService.Services.IHullFamilyService, HullSizingService.Services.HullFamilyService>();
    Log.Information("Hull family service registered");

    // Solver Services
    // Register solver options (can be configured via appsettings)
    var solverOptions = new HullSizingService.Services.Solver.SolverOptions
    {
        DebugIterations = builder.Environment.IsDevelopment() // Enable debug logging in development
    };
    builder.Services.AddSingleton(solverOptions);

    // Use hybrid displacement closure (Newton + Brent fallback)
    builder.Services.AddScoped<HullSizingService.Services.Solver.IDisplacementClosureService, HullSizingService.Services.Solver.HybridDisplacementClosureService>();
    builder.Services.AddScoped<HullSizingService.Services.Solver.IResistanceService, HullSizingService.Services.Solver.HoltropResistanceService>();
    builder.Services.AddScoped<HullSizingService.Services.Solver.IStabilityScreenService, HullSizingService.Services.Solver.StabilityScreenService>();
    builder.Services.AddScoped<HullSizingService.Services.Solver.IFirstPrinciplesSolver, HullSizingService.Services.Solver.FirstPrinciplesSolver>();
    Log.Information("Solver services registered (hybrid displacement closure with Brent fallback, resistance, stability, first-principles)");

    // Data-Driven Mode Services
    builder.Services.AddScoped<HullSizingService.Services.DataDriven.VesselScalingService>();
    Log.Information("Data-Driven services registered (vessel scaling)");

    // FluentValidation validators
    builder.Services.AddScoped<FluentValidation.IValidator<Shared.DTOs.Sizing.CreateMissionCaseDto>, Shared.Validators.Sizing.CreateMissionCaseDtoValidator>();
    builder.Services.AddScoped<FluentValidation.IValidator<Shared.DTOs.Sizing.UpdateMissionCaseDto>, Shared.Validators.Sizing.UpdateMissionCaseDtoValidator>();
    builder.Services.AddScoped<FluentValidation.IValidator<Shared.DTOs.Sizing.CreateSizingRunDto>, Shared.Validators.Sizing.CreateSizingRunDtoValidator>();
    Log.Information("FluentValidation validators registered");

    // OpenTelemetry Tracing
    builder.Services.AddOpenTelemetry()
        .ConfigureResource(resource => resource.AddService("HullSizingService"))
        .WithTracing(tracerProviderBuilder =>
        {
            tracerProviderBuilder
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddEntityFrameworkCoreInstrumentation()
                .AddSource("HullSizingService");

            // Add console exporter in development
            if (builder.Environment.IsDevelopment())
            {
                tracerProviderBuilder.AddConsoleExporter();
            }
        });

    // Register ActivitySource for custom instrumentation
    builder.Services.AddSingleton(new ActivitySource("HullSizingService"));

    // Swagger/OpenAPI
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Version = "v1",
            Title = "NavArch Studio - Hull Sizing API",
            Description = "API for preliminary hull sizing from mission requirements including " +
                          "first-principles solver, displacement closure, Holtrop-Mennen resistance, and geometry generation.",
            Contact = new Microsoft.OpenApi.Models.OpenApiContact
            {
                Name = "NavArch Studio",
                Email = "support@navarch-studio.com"
            },
            License = new Microsoft.OpenApi.Models.OpenApiLicense
            {
                Name = "MIT License",
                Url = new Uri("https://opensource.org/licenses/MIT")
            }
        });

        // Include XML documentation
        var xmlFile = $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml";
        var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
        if (File.Exists(xmlPath))
        {
            options.IncludeXmlComments(xmlPath);
        }

        options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "Unknown" });
    });

    // CORS - Read allowed origins from configuration
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:3000", "http://localhost:5002" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins(allowedOrigins)
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();
        });
    });

    // Health checks
    builder.Services.AddHealthChecks()
        .AddNpgSql(builder.Configuration.GetConnectionString("DefaultConnection")!);

    // Rate Limiting
    builder.Services.AddRateLimiter(options =>
    {
        options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: clientIp,
                factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = 100,
                    Window = TimeSpan.FromMinutes(1),
                    QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst,
                    QueueLimit = 0
                });
        });

        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;

            TimeSpan? retryAfter = null;
            if (context.Lease.TryGetMetadata(System.Threading.RateLimiting.MetadataName.RetryAfter, out var retryAfterValue))
            {
                retryAfter = retryAfterValue;
                context.HttpContext.Response.Headers.RetryAfter = retryAfterValue.TotalSeconds.ToString();
            }

            await context.HttpContext.Response.WriteAsJsonAsync(new
            {
                error = "Too many requests",
                message = "Rate limit exceeded. Please try again later.",
                retryAfter = retryAfter?.TotalSeconds
            }, cancellationToken);
        };
    });

    var app = builder.Build();

    // Run migrations synchronously before starting the service
    Console.WriteLine("[MIGRATION] Starting database migration check...");
    Log.Information("[MIGRATION] Starting database migration check...");

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<SizingDbContext>();

        try
        {
            Console.WriteLine("[MIGRATION] Checking database connectivity...");
            var canConnect = await dbContext.Database.CanConnectAsync();
            Console.WriteLine($"[MIGRATION] Database connection successful: {canConnect}");

            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

            Console.WriteLine($"[MIGRATION] Migration status - Applied: {appliedMigrations.Count()}, Pending: {pendingMigrations.Count()}");

            if (pendingMigrations.Any())
            {
                if (app.Environment.EnvironmentName != "Development")
                {
                    Console.WriteLine($"[MIGRATION] Auto-applying {pendingMigrations.Count()} pending migrations...");
                    await dbContext.Database.MigrateAsync();
                    Console.WriteLine("[MIGRATION] Migrations applied successfully!");
                }
                else
                {
                    Console.WriteLine("[MIGRATION] Development mode: Run 'dotnet ef database update' manually.");
                }
            }
            else
            {
                Console.WriteLine("[MIGRATION] Database schema is up to date");
            }

            // Seed reference data
            Console.WriteLine("[SEED] Starting seed data import...");
            Log.Information("[SEED] Starting seed data import...");

            try
            {
                var seeder = new HullSizingService.Data.Seeds.CsvDataSeeder(dbContext, scope.ServiceProvider.GetRequiredService<ILogger<HullSizingService.Data.Seeds.CsvDataSeeder>>());
                await seeder.SeedAllAsync();
                Console.WriteLine("[SEED] Seed data import complete");
                Log.Information("[SEED] Seed data import complete");
            }
            catch (Exception seedEx)
            {
                Console.WriteLine($"[SEED] ERROR: {seedEx.Message}");
                Log.Error(seedEx, "[SEED] Seed data import failed");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MIGRATION] ERROR: {ex.Message}");
            Log.Error(ex, "[MIGRATION] Migration check failed");
        }
    }

    // Add Correlation ID middleware (FIRST)
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Global Exception Handler (SECOND)
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // Security Headers (THIRD)
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // Rate Limiting (FOURTH)
    app.UseRateLimiter();

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
        };
    });

    // Configure the HTTP request pipeline
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseCors("AllowAll");

    // JWT Authentication Middleware
    app.UseMiddleware<JwtAuthenticationMiddleware>();

    // Claims Forwarding Middleware (AFTER JWT authentication)
    app.UseMiddleware<ClaimsForwardingMiddleware>();

    // Unit Conversion Middleware
    app.UseMiddleware<UnitConversionMiddleware>();

    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health").DisableRateLimiting();

    Log.Information("HullSizingService started successfully on port 5004");
    Console.WriteLine("[STARTUP] HullSizingService ready on port 5004");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "HullSizingService failed to start!");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Polly Policies for DataService HTTP Client
static IAsyncPolicy<HttpResponseMessage> GetRetryPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<TimeoutException>()
        .WaitAndRetryAsync(
            retryCount: 3,
            sleepDurationProvider: retryAttempt =>
                TimeSpan.FromMilliseconds(200 + Random.Shared.Next(0, 400)), // Jittered backoff: 200-600ms
            onRetry: (outcome, timespan, retryCount, context) =>
            {
                Log.Warning(
                    "[POLLY] Retry {RetryCount} after {Delay}ms due to {Reason}",
                    retryCount,
                    timespan.TotalMilliseconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
            });
}

static IAsyncPolicy<HttpResponseMessage> GetCircuitBreakerPolicy()
{
    return HttpPolicyExtensions
        .HandleTransientHttpError()
        .Or<TimeoutException>()
        .CircuitBreakerAsync(
            handledEventsAllowedBeforeBreaking: 5,
            durationOfBreak: TimeSpan.FromSeconds(30),
            onBreak: (outcome, breakDelay) =>
            {
                Log.Error(
                    "[POLLY] Circuit breaker opened for {BreakDelay}s due to {Reason}",
                    breakDelay.TotalSeconds,
                    outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString() ?? "Unknown");
            },
            onReset: () =>
            {
                Log.Information("[POLLY] Circuit breaker reset - DataService is healthy again");
            },
            onHalfOpen: () =>
            {
                Log.Information("[POLLY] Circuit breaker half-open - testing DataService");
            });
}

static IAsyncPolicy<HttpResponseMessage> GetTimeoutPolicy()
{
    return Policy.TimeoutAsync<HttpResponseMessage>(
        TimeSpan.FromSeconds(2), // Individual request timeout
        onTimeoutAsync: (context, timespan, task) =>
        {
            Log.Warning("[POLLY] Request timed out after {Timeout}s", timespan.TotalSeconds);
            return Task.CompletedTask;
        });
}

// Make Program accessible for integration tests
public partial class Program { }

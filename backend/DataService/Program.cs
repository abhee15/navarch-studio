using Microsoft.EntityFrameworkCore;
using DataService.Data;
using DataService.Services;
using Shared.Services;
using Shared.Middleware;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using FluentValidation;

// Bootstrap logger for startup errors
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting DataService...");

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
                path: "logs/dataservice-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 10_485_760,
                rollOnFileSizeLimit: true
            );
    });

    // [STARTUP] Log environment and configuration
    Console.WriteLine($"[STARTUP] ===============================================");
    Console.WriteLine($"[STARTUP] DataService Starting");
    Console.WriteLine($"[STARTUP] ===============================================");
    Console.WriteLine($"[STARTUP] Environment: {builder.Environment.EnvironmentName}");
    Console.WriteLine($"[STARTUP] Machine: {Environment.MachineName}");
    Console.WriteLine($"[STARTUP] OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");
    Console.WriteLine($"[STARTUP] Framework: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}");

    // Log key configuration (redact sensitive data)
    var connString = builder.Configuration.GetConnectionString("DefaultConnection");
    var dbPassword = builder.Configuration["DatabasePassword"];
    var safeConnString = connString ?? "NOT SET";

    // Only redact password if it's not null or empty
    if (!string.IsNullOrEmpty(dbPassword) && !string.IsNullOrEmpty(connString))
    {
        safeConnString = connString.Replace(dbPassword, "***");
    }

    Console.WriteLine($"[STARTUP] Connection String: {safeConnString}");
    Console.WriteLine($"[STARTUP] DatabaseHost: {builder.Configuration["DatabaseHost"] ?? "NOT SET"}");
    Console.WriteLine($"[STARTUP] DatabaseName: {builder.Configuration["DatabaseName"] ?? "NOT SET"}");
    Console.WriteLine($"[STARTUP] CognitoUserPoolId: {builder.Configuration["CognitoUserPoolId"] ?? "NOT SET"}");
    Console.WriteLine($"[STARTUP] CognitoRegion: {builder.Configuration["CognitoRegion"] ?? "NOT SET"}");
    Console.WriteLine($"[STARTUP] ===============================================");

    Log.Information("[STARTUP] DataService starting - Environment: {Environment}", builder.Environment.EnvironmentName);

    // Add services to the container.
    builder.Services.AddControllers(options =>
    {
        // Add global filter for automatic unit conversion
        options.Filters.Add<Shared.Filters.UnitConversionFilter>();
    })
    .AddJsonOptions(options =>
    {
        // Handle circular references in entity relationships
        options.JsonSerializerOptions.ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.IgnoreCycles;
        // Use camelCase for JSON serialization (matches JavaScript convention)
        options.JsonSerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
    });

    // API Versioning
    builder.Services.AddApiVersioning(options =>
    {
        // Report API versions in response headers
        options.ReportApiVersions = true;

        // Default version if client doesn't specify
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.DefaultApiVersion = new Asp.Versioning.ApiVersion(1, 0);

        // Read version from URL path (e.g., /api/v1/users)
        options.ApiVersionReader = new Asp.Versioning.UrlSegmentApiVersionReader();
    }).AddMvc();

    // Database - Use snake_case naming convention for PostgreSQL
    builder.Services.AddDbContext<DataDbContext>(options =>
    {
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            // Set command timeout to 60 seconds (default is 30)
            npgsqlOptions.CommandTimeout(60);

            // Enable retry on failure for transient errors
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);

            // Use connection pooling (default, but explicit for clarity)
            npgsqlOptions.MaxBatchSize(100);
        })
        .UseSnakeCaseNamingConvention()
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .EnableDetailedErrors(builder.Environment.IsDevelopment());
    });

    // Memory Cache for JWT key caching
    builder.Services.AddMemoryCache();

    // HttpClient for Cognito JWKS requests
    builder.Services.AddHttpClient();

    // Services
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

    // Unit Conversion Service (NavArch.UnitConversion)
    // Pass null to use default path (config/unit-systems.xml) which is where the file is copied
    builder.Services.AddSingleton<NavArch.UnitConversion.Services.IUnitConverter>(sp =>
        new NavArch.UnitConversion.Services.UnitConverter(null));
    Log.Information("Unit conversion service registered with default config path");

    builder.Services.AddScoped<IUnitConversionService, UnitConversionService>();

    // Hydrostatics Services
    builder.Services.AddScoped<DataService.Services.Hydrostatics.IValidationService, DataService.Services.Hydrostatics.ValidationService>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.IVesselService, DataService.Services.Hydrostatics.VesselService>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.IGeometryService, DataService.Services.Hydrostatics.GeometryService>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.ILoadcaseService, DataService.Services.Hydrostatics.LoadcaseService>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.IIntegrationEngine, DataService.Services.Hydrostatics.IntegrationEngine>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.IHydroCalculator, DataService.Services.Hydrostatics.HydroCalculator>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.ICsvParserService, DataService.Services.Hydrostatics.CsvParserService>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.ICurvesGenerator, DataService.Services.Hydrostatics.CurvesGenerator>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.ITrimSolver, DataService.Services.Hydrostatics.TrimSolver>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.IExportService, DataService.Services.Hydrostatics.ExportService>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.IStabilityCalculator, DataService.Services.Hydrostatics.StabilityCalculator>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.IStabilityCriteriaChecker, DataService.Services.Hydrostatics.StabilityCriteriaChecker>();

    // FluentValidation - Register all validators from Shared assembly
    // Note: Add validators from Shared assembly as needed
    // builder.Services.AddValidatorsFromAssembly(typeof(Shared.Models.Vessel).Assembly);

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
        {
            Version = "v1",
            Title = "NavArch Studio - Hydrostatics API",
            Description = "API for naval architecture hydrostatic calculations including vessel geometry management, " +
                          "loadcase definitions, hydrostatic computations, curves generation, and trim solving.",
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

        // Group endpoints by controller
        options.TagActionsBy(api => new[] { api.GroupName ?? api.ActionDescriptor.RouteValues["controller"] ?? "Unknown" });
    });

    // CORS - Read allowed origins from configuration
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:3000", "http://localhost:5002" };

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowAll", policy =>
        {
            policy.WithOrigins(allowedOrigins)  // Only allow configured origins
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
        // Global rate limit: 100 requests per minute per IP
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
    // Health check timeout is set to 30 seconds in Terraform to allow migrations to complete
    Console.WriteLine("[MIGRATION] Starting database migration check...");
    Log.Information("[MIGRATION] Starting database migration check...");

    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<DataDbContext>();

        try
        {
            Console.WriteLine("[MIGRATION] Checking database connectivity...");
            Log.Information("[MIGRATION] Checking database connectivity...");

            var canConnect = await dbContext.Database.CanConnectAsync();
            Console.WriteLine($"[MIGRATION] Database connection successful: {canConnect}");
            Log.Information("[MIGRATION] Database connection successful: {CanConnect}", canConnect);

            // Get pending migrations
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

            Console.WriteLine($"[MIGRATION] Migration status - Applied: {appliedMigrations.Count()}, Pending: {pendingMigrations.Count()}");
            Log.Information("[MIGRATION] Migration status - Applied: {Applied}, Pending: {Pending}",
                appliedMigrations.Count(), pendingMigrations.Count());

            if (pendingMigrations.Any())
            {
                Console.WriteLine($"[MIGRATION] Pending migrations: {string.Join(", ", pendingMigrations)}");
                Log.Warning("[MIGRATION] Pending migrations: {Migrations}",
                    string.Join(", ", pendingMigrations));

                // In Staging/Production, apply migrations automatically
                // In Development, just warn
                if (app.Environment.EnvironmentName != "Development")
                {
                    Console.WriteLine($"[MIGRATION] Auto-applying {pendingMigrations.Count()} pending migrations in {app.Environment.EnvironmentName} environment...");
                    Log.Information("[MIGRATION] Auto-applying {Count} pending migrations in {Environment} environment...",
                        pendingMigrations.Count(), app.Environment.EnvironmentName);

                    await dbContext.Database.MigrateAsync();

                    Console.WriteLine("[MIGRATION] Migrations applied successfully!");
                    Log.Information("[MIGRATION] Migrations applied successfully!");
                }
                else
                {
                    Console.WriteLine("[MIGRATION] Development mode: Skipping auto-migration. Run 'dotnet ef database update' manually.");
                    Log.Warning("[MIGRATION] Development mode: Skipping auto-migration. Run 'dotnet ef database update' manually.");
                }
            }
            else
            {
                Console.WriteLine("[MIGRATION] Database schema is up to date (no pending migrations)");
                Log.Information("[MIGRATION] Database schema is up to date (no pending migrations)");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MIGRATION] ERROR: Migration check failed: {ex.Message}");
            Console.WriteLine($"[MIGRATION] Stack trace: {ex.StackTrace}");
            Log.Error(ex, "[MIGRATION] Migration check failed: {Message}", ex.Message);
            // Don't throw - let the app start and health checks will catch the issue
        }
    }

    Console.WriteLine("[MIGRATION] Database migration check complete");
    Log.Information("[MIGRATION] Database migration check complete");

    // Add Correlation ID middleware (FIRST - so all logs have correlation ID)
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Security Headers (SECOND - add security headers to all responses)
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // Rate Limiting (THIRD - block abusive requests early)
    app.UseRateLimiter();

    // Add Serilog request logging
    app.UseSerilogRequestLogging(options =>
    {
        options.MessageTemplate = "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";
        options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        {
            diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
            diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
            diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
        };
    });

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    // Don't use HTTPS redirection in production - App Runner handles HTTPS termination
    // app.UseHttpsRedirection();
    app.UseCors("AllowAll");

    // JWT Authentication Middleware
    app.UseMiddleware<JwtAuthenticationMiddleware>();

    // Unit Conversion Middleware (after JWT so we have user context)
    app.UseMiddleware<UnitConversionMiddleware>();

    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health").DisableRateLimiting();

    Log.Information("DataService started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "DataService failed to start!");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program accessible for integration tests
public partial class Program { }

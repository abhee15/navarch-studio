using FluentValidation;
using IdentityService.Data;
using IdentityService.Services;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Shared.Middleware;
using Shared.Models;
using Shared.Services;
using Shared.Utilities;

// Bootstrap logger for startup errors
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting IdentityService...");

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
                path: "logs/identityservice-.log",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 10_485_760,
                rollOnFileSizeLimit: true
            );
    });

    // Add services to the container.
    builder.Services.AddControllers()
        .AddJsonOptions(options =>
        {
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
    builder.Services.AddDbContext<IdentityDbContext>(options =>
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

            // Use a separate schema for IdentityService migrations history to avoid conflicts with DataService
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "identity");
        })
        .UseSnakeCaseNamingConvention()  // Use PostgreSQL standard snake_case naming
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .EnableDetailedErrors(builder.Environment.IsDevelopment());
    });

    // Memory Cache for JWT key caching
    builder.Services.AddMemoryCache();

    // HttpClient for Cognito JWKS requests
    builder.Services.AddHttpClient();

    // Services
    builder.Services.AddScoped<IUserService, UserService>();

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
    var xmlPath = Path.Combine(AppContext.BaseDirectory, "unit-systems.xml");
    builder.Services.AddSingleton<NavArch.UnitConversion.Services.IUnitConverter>(sp =>
        new NavArch.UnitConversion.Services.UnitConverter(xmlPath));
    Log.Information("Unit conversion service registered with config: {XmlPath}", xmlPath);

    // FluentValidation - Register all validators from Shared assembly
    builder.Services.AddValidatorsFromAssemblyContaining<Shared.Validators.CreateUserDtoValidator>();

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

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
    Log.Information("🔄 Starting database migration check...");
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityService.Data.IdentityDbContext>();

        try
        {
            Log.Information("Checking database connectivity...");
            var canConnect = await dbContext.Database.CanConnectAsync();
            Log.Information("✅ Database connection successful: {CanConnect}", canConnect);

            // Get pending migrations
            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync();

            Log.Information("📊 Migration status - Applied: {Applied}, Pending: {Pending}",
                appliedMigrations.Count(), pendingMigrations.Count());

            if (pendingMigrations.Any())
            {
                Log.Warning("⚠️ Pending migrations: {Migrations}",
                    string.Join(", ", pendingMigrations));

                // In Staging/Production, apply migrations automatically
                // In Development, just warn
                if (app.Environment.EnvironmentName != "Development")
                {
                    Log.Information("🔄 Auto-applying {Count} pending migrations in {Environment} environment...",
                        pendingMigrations.Count(), app.Environment.EnvironmentName);
                    await dbContext.Database.MigrateAsync();
                    Log.Information("✅ Migrations applied successfully!");
                }
                else
                {
                    Log.Warning("⚠️ Development mode: Skipping auto-migration. Run 'dotnet ef database update' manually.");
                }
            }
            else
            {
                Log.Information("✅ Database schema is up to date (no pending migrations)");
            }

            // Seed essential data in non-Development environments
            // Development uses manual seeding via database/seeds/dev-seed.sql
            if (app.Environment.EnvironmentName != "Development")
            {
                Console.WriteLine("[SEED] Checking for essential users...");
                Log.Information("[SEED] Checking for essential users...");

                try
                {
                    await SeedEssentialUsersAsync(dbContext);
                    Console.WriteLine("[SEED] Essential user seeding completed.");
                    Log.Information("[SEED] Essential user seeding completed.");
                }
                catch (Exception seedEx)
                {
                    Console.WriteLine($"[SEED] WARNING: Failed to seed essential users: {seedEx.Message}");
                    Log.Warning(seedEx, "[SEED] Failed to seed essential users: {Message}", seedEx.Message);
                    // Don't throw - seeding is optional, but log warning for monitoring
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error(ex, "❌ Migration check failed: {Message}", ex.Message);
            // Don't throw - let the app start and health checks will catch the issue
        }
    }
    Log.Information("✅ Database migration check complete");

    // Add Correlation ID middleware (FIRST - so all logs have correlation ID)
    app.UseMiddleware<CorrelationIdMiddleware>();

    // Global Exception Handler (SECOND - catch exceptions and return consistent error responses)
    app.UseMiddleware<GlobalExceptionHandlerMiddleware>();

    // Security Headers (THIRD - add security headers to all responses)
    app.UseMiddleware<SecurityHeadersMiddleware>();

    // Rate Limiting (FOURTH - block abusive requests early)
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

    Log.Information("IdentityService started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "IdentityService failed to start!");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make Program accessible for integration tests
public partial class Program
{
    /// <summary>
    /// Seeds essential users in cloud environments (idempotent).
    /// Creates admin user if it doesn't exist.
    /// </summary>
    private static async Task SeedEssentialUsersAsync(IdentityDbContext dbContext)
    {
        // Default admin user email - can be overridden via configuration in the future
        const string adminEmail = "admin@navarch-studio.com";
        const string adminName = "System Administrator";
        const string defaultPassword = "ChangeMe123!"; // Should be changed on first login

        // Check if admin user already exists
        var adminExists = await dbContext.Users
            .AnyAsync(u => u.Email == adminEmail);

        if (!adminExists)
        {
            Log.Information("[SEED] Creating admin user: {Email}", adminEmail);

            var adminUser = new User
            {
                Id = Guid.NewGuid().ToString(),
                Email = adminEmail,
                Name = adminName,
                PasswordHash = PasswordHasher.Hash(defaultPassword),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            dbContext.Users.Add(adminUser);
            await dbContext.SaveChangesAsync();

            Log.Information("[SEED] Admin user created successfully: {Email}", adminEmail);
            Console.WriteLine($"[SEED] Admin user created: {adminEmail} (password: {defaultPassword})");
        }
        else
        {
            Log.Information("[SEED] Admin user already exists. Skipping seed.");
            Console.WriteLine("[SEED] Admin user already exists. Skipping seed.");
        }
    }
}

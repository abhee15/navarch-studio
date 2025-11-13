using DataService.Data;
using DataService.Data.Seeds;
using DataService.Services;
using DataService.Services.ShipD;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
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

    // Log key configuration (redact sensitive data from connection string)
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

    // AWS S3 + HTTP client for ingestion (skip S3 services in development)
    builder.Services.AddHttpClient();
    if (!builder.Environment.IsDevelopment())
    {
        builder.Services.AddSingleton<Amazon.S3.IAmazonS3>(_ => new Amazon.S3.AmazonS3Client());
        builder.Services.AddScoped<DataService.Services.IBenchmarkIngestionService, DataService.Services.BenchmarkIngestionService>();
        Log.Information("AWS S3 client and ingestion services registered (production mode)");
    }
    else
    {
        Log.Information("Skipping AWS S3 client registration (development mode - not needed for local testing)");
    }
    builder.Services.AddScoped<DataService.Services.BenchmarkSeedService>();
    builder.Services.AddScoped<DataService.Services.BenchmarkValidationService>();

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

            // Explicit migration history table in data schema (matches IdentityService using identity schema)
            npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "data");
        })
        .UseSnakeCaseNamingConvention()  // Use PostgreSQL standard snake_case naming
        .EnableSensitiveDataLogging(builder.Environment.IsDevelopment())
        .EnableDetailedErrors(builder.Environment.IsDevelopment());
    });

    // Memory Cache for JWT key caching
    builder.Services.AddMemoryCache();

    // Redis Distributed Cache for ML catalog caching
    var redisConnection = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnection;
        options.InstanceName = "NavArchML_";  // Prefix for all cache keys
    });
    Log.Information("Redis distributed cache registered: {RedisConnection}", redisConnection);

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
    builder.Services.AddScoped<DataService.Services.Hydrostatics.IHullProjectionsService, DataService.Services.Hydrostatics.HullProjectionsService>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.SampleVesselSeedService>();

    // Lines Plan Services
    builder.Services.AddScoped<DataService.Services.Hydrostatics.IDigonalsService, DataService.Services.Hydrostatics.DiagonalsService>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.ISectionAreaCurveService, DataService.Services.Hydrostatics.SectionAreaCurveService>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.IFairingQualityService, DataService.Services.Hydrostatics.FairingQualityService>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.ILinesPlanPdfService, DataService.Services.Hydrostatics.LinesPlanPdfService>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.IIgesExportService, DataService.Services.Hydrostatics.IgesExportService>();
    builder.Services.AddScoped<DataService.Services.Hydrostatics.ITemplateVesselSeeder, DataService.Services.Hydrostatics.TemplateVesselSeeder>();

    // Resistance calculation services
    builder.Services.AddScoped<DataService.Services.Resistance.WaterPropertiesService>();
    builder.Services.AddScoped<DataService.Services.Resistance.IResistanceCalculationService, DataService.Services.Resistance.ResistanceCalculationService>();
    builder.Services.AddScoped<DataService.Services.Resistance.HoltropMennenService>();
    builder.Services.AddScoped<DataService.Services.Resistance.PowerCalculationService>();
    builder.Services.AddScoped<DataService.Services.Resistance.KcsBenchmarkService>();
    builder.Services.AddScoped<DataService.Services.Resistance.SpeedDraftMatrixService>();
    builder.Services.AddScoped<DataService.Services.Resistance.IDefaultValuesService, DataService.Services.Resistance.DefaultValuesService>();

    // Seakeeping services
    builder.Services.AddScoped<DataService.Services.Seakeeping.IStripTheoryEngine, DataService.Services.Seakeeping.StripTheoryEngine>();
    builder.Services.AddScoped<DataService.Services.Seakeeping.IRaoCalculator, DataService.Services.Seakeeping.RaoCalculator>();
    builder.Services.AddScoped<DataService.Services.Seakeeping.IWaveSpectrumService, DataService.Services.Seakeeping.WaveSpectrumService>();
    builder.Services.AddScoped<DataService.Services.Seakeeping.IMotionAnalysisService, DataService.Services.Seakeeping.MotionAnalysisService>();
    builder.Services.AddScoped<DataService.Services.Seakeeping.IExceedanceCalculator, DataService.Services.Seakeeping.ExceedanceCalculator>();
    builder.Services.AddScoped<DataService.Services.Seakeeping.ISeakeepingExportService, DataService.Services.Seakeeping.SeakeepingExportService>();

    // Comparison service
    builder.Services.AddScoped<ComparisonService>();

    // Catalog services
    builder.Services.AddScoped<DataService.Data.Seeds.CatalogSeeder>();
    builder.Services.AddScoped<ShipDMetadataSeeder>();
    builder.Services.AddScoped<DataService.Services.Catalog.CatalogWaterService>();
    builder.Services.AddScoped<DataService.Services.Catalog.VesselCatalogImporter>();
    builder.Services.AddScoped<DataService.Services.Catalog.CatalogVesselSeeder>();
    builder.Services.AddScoped<DataService.Services.Catalog.IVesselTypeMapper, DataService.Services.Catalog.VesselTypeMapper>();
    builder.Services.AddScoped<DataService.Services.Catalog.RealWorldKnnService>();
    builder.Services.AddScoped<DataService.Services.Catalog.CatalogTaxonomySeeder>();
    builder.Services.AddScoped<DataService.Services.Catalog.ParametricCatalogImporter>();
    builder.Services.AddScoped<DataService.Services.Catalog.ParametricCatalogSeeder>();
    builder.Services.AddScoped<DataService.Services.Catalog.ParametricDemoDataGenerator>();
    builder.Services.AddScoped<DataService.Services.Catalog.ParametricKnnService>();
    builder.Services.AddScoped<IShipDMetadataService, ShipDMetadataService>();
    builder.Services.AddScoped<DataService.Services.Catalog.BenchmarkHullImporter>();
    builder.Services.AddScoped<DataService.Services.Catalog.BenchmarkTestImporter>();
    builder.Services.AddSingleton<DataService.Services.Catalog.WageningenBSeriesService>();
    Log.Information("Benchmark and propeller services registered");

    // Background services for catalog management
    builder.Services.AddHostedService<DataService.Services.Catalog.ParametricImportBackgroundService>();
    Log.Information("Parametric import background service registered");

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
        // Adjust rate limits based on environment
        // Development: Very high limits to avoid issues during testing
        // Production: Standard limits for protection
        var isDevelopment = builder.Environment.IsDevelopment();
        var permitLimit = isDevelopment ? 10000 : 100; // Much higher limit for development

        // Global rate limit: 100 requests per minute per IP (or 10000 in dev)
        options.GlobalLimiter = System.Threading.RateLimiting.PartitionedRateLimiter.Create<HttpContext, string>(context =>
        {
            var clientIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            return System.Threading.RateLimiting.RateLimitPartition.GetFixedWindowLimiter(
                partitionKey: clientIp,
                factory: _ => new System.Threading.RateLimiting.FixedWindowRateLimiterOptions
                {
                    PermitLimit = permitLimit,
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

            // Log last applied migration for easy verification
            var lastMigration = appliedMigrations.LastOrDefault();
            if (lastMigration != null)
            {
                Console.WriteLine($"[MIGRATION] ✅ Last applied migration: {lastMigration}");
                Log.Information("[MIGRATION] Last applied migration: {Migration}", lastMigration);
            }
            else
            {
                Console.WriteLine("[MIGRATION] ⚠️  No migrations have been applied yet (empty database)");
                Log.Warning("[MIGRATION] No migrations have been applied yet");
            }

            if (pendingMigrations.Any())
            {
                Console.WriteLine($"[MIGRATION] Pending migrations: {string.Join(", ", pendingMigrations)}");
                Log.Warning("[MIGRATION] Pending migrations: {Migrations}",
                    string.Join(", ", pendingMigrations));

                // Auto-apply migrations in ALL environments (Development, Staging, Production)
                Console.WriteLine($"[MIGRATION] Auto-applying {pendingMigrations.Count()} pending migrations in {app.Environment.EnvironmentName} environment...");
                Log.Information("[MIGRATION] Auto-applying {Count} pending migrations in {Environment} environment...",
                    pendingMigrations.Count(), app.Environment.EnvironmentName);

                await dbContext.Database.MigrateAsync();

                Console.WriteLine("[MIGRATION] Migrations applied successfully!");
                Log.Information("[MIGRATION] Migrations applied successfully!");
            }
            else
            {
                Console.WriteLine("[MIGRATION] Database schema is up to date (no pending migrations)");
                Log.Information("[MIGRATION] Database schema is up to date (no pending migrations)");
            }

            // Seed parametric catalog (runs in all environments if empty)
            // Validate schema after migrations
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("[VALIDATION] Validating database schema...");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Log.Information("[VALIDATION] Starting schema validation");

            try
            {
                var validator = new DataService.Data.MigrationValidator(
                    dbContext,
                    scope.ServiceProvider.GetRequiredService<ILogger<DataService.Data.MigrationValidator>>());

                var validationResult = await validator.ValidateAsync();

                if (validationResult.Errors.Count > 0)
                {
                    Console.WriteLine($"❌ [VALIDATION] {validationResult.Errors.Count} critical error(s) found:");
                    foreach (var error in validationResult.Errors)
                    {
                        Console.WriteLine($"   - {error}");
                        Log.Error("[VALIDATION] {Error}", error);
                    }
                }

                if (validationResult.Warnings.Count > 0)
                {
                    Console.WriteLine($"⚠️  [VALIDATION] {validationResult.Warnings.Count} warning(s) found:");
                    foreach (var warning in validationResult.Warnings)
                    {
                        Console.WriteLine($"   - {warning}");
                        Log.Warning("[VALIDATION] {Warning}", warning);
                    }
                }

                if (validationResult.IsValid && !validationResult.HasWarnings)
                {
                    Console.WriteLine("✅ [VALIDATION] All schema validation checks passed");
                    Log.Information("[VALIDATION] Schema validation passed");
                }

                // FAIL STARTUP if critical errors found
                if (!validationResult.IsValid)
                {
                    Console.WriteLine("═══════════════════════════════════════════════════════════");
                    Console.WriteLine("❌ [VALIDATION] CRITICAL: Schema validation failed!");
                    Console.WriteLine("═══════════════════════════════════════════════════════════");
                    Log.Fatal("[VALIDATION] Schema validation failed - ABORTING STARTUP");
                    throw new InvalidOperationException(
                        $"Database schema validation failed with {validationResult.Errors.Count} error(s). " +
                        "See logs for details. Service cannot start with incorrect schema.");
                }
            }
            catch (InvalidOperationException)
            {
                throw; // Re-throw validation failures
            }
            catch (Exception validationEx)
            {
                Console.WriteLine($"⚠️  [VALIDATION] ERROR: {validationEx.Message}");
                Log.Error(validationEx, "[VALIDATION] Schema validation failed with exception");
            }

            // Seed parametric hull catalog (runs in all environments)
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Console.WriteLine("[SEED] Checking for parametric hull catalog...");
            Console.WriteLine("═══════════════════════════════════════════════════════════");
            Log.Information("[SEED] Checking for parametric hull catalog...");
            try
            {
                var parametricSeeder = scope.ServiceProvider.GetRequiredService<DataService.Services.Catalog.ParametricCatalogSeeder>();
                await parametricSeeder.SeedParametricCatalogAsync();
                Console.WriteLine("[SEED] Parametric catalog check complete");
                Log.Information("[SEED] Parametric catalog check complete");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SEED] ERROR seeding parametric catalog: {ex.Message}");
                Log.Error(ex, "[SEED] Failed to seed parametric catalog");
                // Don't fail startup on seeding errors
            }

            // Seed template vessels (runs in all environments)
            Console.WriteLine("[SEED] Checking for template vessels...");
            Log.Information("[SEED] Checking for template vessels...");
            try
            {
                var templateSeeder = scope.ServiceProvider.GetRequiredService<DataService.Services.Hydrostatics.ITemplateVesselSeeder>();
                await templateSeeder.SeedHydrostaticsTemplateAsync();
                Console.WriteLine("[SEED] Template vessel seeding completed.");
                Log.Information("[SEED] Template vessel seeding completed.");
            }
            catch (Exception seedEx)
            {
                Console.WriteLine($"[SEED] WARNING: Failed to seed template vessel: {seedEx.Message}");
                Log.Warning(seedEx, "[SEED] Failed to seed template vessel: {Message}", seedEx.Message);
                // Don't throw - seeding is optional, but log warning for monitoring
            }

            // Seed ShipD metadata (runs in all environments)
            Console.WriteLine("[SEED] Checking for ShipD metadata...");
            Log.Information("[SEED] Checking for ShipD metadata...");
            try
            {
                var shipdSeeder = scope.ServiceProvider.GetRequiredService<ShipDMetadataSeeder>();
                await shipdSeeder.SeedAsync(CancellationToken.None);
                Console.WriteLine("[SEED] ShipD metadata seeding completed.");
                Log.Information("[SEED] ShipD metadata seeding completed.");

                // Seed catalog taxonomy after ShipD metadata is available
                var catalogTaxonomySeeder = scope.ServiceProvider.GetRequiredService<DataService.Services.Catalog.CatalogTaxonomySeeder>();
                await catalogTaxonomySeeder.SeedAsync(CancellationToken.None);
                Console.WriteLine("[SEED] Catalog taxonomy seeding completed.");
                Log.Information("[SEED] Catalog taxonomy seeding completed.");
            }
            catch (Exception seedEx)
            {
                Console.WriteLine($"[SEED] WARNING: Failed to seed ShipD metadata or catalog taxonomy: {seedEx.Message}");
                Log.Warning(seedEx, "[SEED] Failed to seed ShipD metadata or catalog taxonomy: {Message}", seedEx.Message);
            }

            // Seed catalog data (runs in all environments)
            Console.WriteLine("[SEED] Checking for catalog data...");
            Log.Information("[SEED] Checking for catalog data...");
            try
            {
                var catalogSeeder = scope.ServiceProvider.GetRequiredService<DataService.Data.Seeds.CatalogSeeder>();
                await catalogSeeder.SeedAllAsync();
                Console.WriteLine("[SEED] Catalog seeding completed.");
                Log.Information("[SEED] Catalog seeding completed.");
            }
            catch (Exception seedEx)
            {
                Console.WriteLine($"[SEED] WARNING: Failed to seed catalog: {seedEx.Message}");
                Log.Warning(seedEx, "[SEED] Failed to seed catalog: {Message}", seedEx.Message);
                // Don't throw - seeding is optional, but log warning for monitoring
            }

            // Seed real-world vessel catalog (600 vessels for Data-Driven mode)
            Console.WriteLine("[SEED] Checking for real-world vessel catalog...");
            Log.Information("[SEED] Checking for real-world vessel catalog...");
            try
            {
                var vesselSeeder = scope.ServiceProvider.GetRequiredService<DataService.Services.Catalog.CatalogVesselSeeder>();
                await vesselSeeder.SeedRealWorldCatalogAsync();
                Console.WriteLine("[SEED] Real-world vessel catalog seeding completed.");
                Log.Information("[SEED] Real-world vessel catalog seeding completed.");
            }
            catch (Exception seedEx)
            {
                Console.WriteLine($"[SEED] WARNING: Failed to seed vessel catalog: {seedEx.Message}");
                Log.Warning(seedEx, "[SEED] Failed to seed vessel catalog: {Message}", seedEx.Message);
                // Don't throw - seeding is optional
            }

            // Seed parametric hull catalog (5K from MIT ShipD for ML/Parametric mode - Phase 2A)
            Console.WriteLine("[SEED] Checking for ML/Parametric hull catalog...");
            Log.Information("[SEED] Checking for ML/Parametric hull catalog...");
            try
            {
                var parametricSeeder = scope.ServiceProvider.GetRequiredService<DataService.Services.Catalog.ParametricCatalogSeeder>();
                await parametricSeeder.SeedParametricCatalogAsync();
                Console.WriteLine("[SEED] ML/Parametric catalog seeding completed.");
                Log.Information("[SEED] ML/Parametric catalog seeding completed.");
            }
            catch (Exception seedEx)
            {
                Console.WriteLine($"[SEED] WARNING: Failed to seed parametric catalog: {seedEx.Message}");
                Log.Warning(seedEx, "[SEED] Failed to seed parametric catalog: {Message}", seedEx.Message);
                // Don't throw - seeding is optional
            }

            // Auto-seed template vessel in ALL environments (required for proper functioning)
            Console.WriteLine("[SEED] Checking for template vessel...");
            Log.Information("[SEED] Checking for template vessel...");

            try
            {
                var seedService = scope.ServiceProvider.GetRequiredService<DataService.Services.Hydrostatics.SampleVesselSeedService>();

                // Seed template vessel (with fixed ID) - always run to ensure geometry exists
                await seedService.SeedTemplateVesselAsync();

                Console.WriteLine("[SEED] Template vessel check complete!");
                Log.Information("[SEED] Template vessel check complete!");
            }
            catch (Exception seedEx)
            {
                Console.WriteLine($"[SEED] WARNING: Failed to seed template vessel: {seedEx.Message}");
                Log.Warning(seedEx, "[SEED] Failed to seed template vessel: {Message}", seedEx.Message);
                // Don't throw - seeding is optional
            }

            // Auto-seed sample vessels in development
            if (app.Environment.IsDevelopment())
            {
                Console.WriteLine("[SEED] Checking for sample vessels in development...");
                Log.Information("[SEED] Checking for sample vessels in development...");

                try
                {
                    var seedService = scope.ServiceProvider.GetRequiredService<DataService.Services.Hydrostatics.SampleVesselSeedService>();

                    // Check if sample vessels already exist for default user
                    var defaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
                    var hasSamples = await dbContext.Vessels
                        .AnyAsync(v => v.UserId == defaultUserId &&
                                      (v.Name.Contains("KCS") || v.Name.Contains("Wigley")));

                    if (!hasSamples)
                    {
                        Console.WriteLine("[SEED] No sample vessels found. Seeding KCS and Wigley...");
                        Log.Information("[SEED] No sample vessels found. Seeding KCS and Wigley...");

                        await seedService.SeedAllSampleVesselsAsync(defaultUserId);

                        Console.WriteLine("[SEED] Sample vessels seeded successfully!");
                        Log.Information("[SEED] Sample vessels seeded successfully!");
                    }
                    else
                    {
                        Console.WriteLine("[SEED] Sample vessels already exist. Skipping seed.");
                        Log.Information("[SEED] Sample vessels already exist. Skipping seed.");
                    }
                }
                catch (Exception seedEx)
                {
                    Console.WriteLine($"[SEED] WARNING: Failed to seed sample vessels: {seedEx.Message}");
                    Log.Warning(seedEx, "[SEED] Failed to seed sample vessels: {Message}", seedEx.Message);
                    // Don't throw - seeding is optional
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[MIGRATION] ❌ ERROR: Migration check failed: {ex.Message}");
            Console.WriteLine($"[MIGRATION] Stack trace: {ex.StackTrace}");
            Console.WriteLine($"[MIGRATION] ⚠️  Service will start but database may be incomplete!");
            Log.Error(ex, "[MIGRATION] Migration check failed: {Message}", ex.Message);

            // In production/staging, fail startup if migrations fail
            // This ensures we catch schema issues immediately
            if (!app.Environment.IsDevelopment())
            {
                Console.WriteLine($"[MIGRATION] ❌ CRITICAL: Failing service startup due to migration failure");
                Log.Fatal("[MIGRATION] CRITICAL: Failing service startup - migrations must succeed in {Environment}", app.Environment.EnvironmentName);
                throw new InvalidOperationException(
                    $"Database migrations failed in {app.Environment.EnvironmentName} environment. " +
                    $"Service cannot start with incomplete schema. Error: {ex.Message}", ex);
            }
            // In development, allow startup to continue (developer can fix manually)
        }
    }

    Console.WriteLine("[MIGRATION] Database migration check complete");
    Log.Information("[MIGRATION] Database migration check complete");

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

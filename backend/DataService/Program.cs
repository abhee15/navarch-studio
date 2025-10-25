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

    // Add services to the container.
    builder.Services.AddControllers();

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

    // Database
    builder.Services.AddDbContext<DataDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

    // Memory Cache for JWT key caching
    builder.Services.AddMemoryCache();

    // HttpClient for Cognito JWKS requests
    builder.Services.AddHttpClient();

    // Services
    builder.Services.AddScoped<IProductService, ProductService>();
    builder.Services.AddSingleton<IJwtService, CognitoJwtService>();

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

    // FluentValidation - Register all validators from Shared assembly
    builder.Services.AddValidatorsFromAssemblyContaining<Shared.Validators.CreateProductDtoValidator>();

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






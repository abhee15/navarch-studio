using ApiGateway.Services;
using Shared.Services;
using Shared.Middleware;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Microsoft.AspNetCore.RateLimiting;
using FluentValidation;

// Bootstrap logger for startup errors
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console()
    .CreateBootstrapLogger();

Log.Information("Starting ApiGateway...");

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
                path: "logs/apigateway-.log",
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

    // HTTP Context Accessor (needed for forwarding headers)
    builder.Services.AddHttpContextAccessor();

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

    // Memory Cache for JWT key caching
    builder.Services.AddMemoryCache();

    // HTTP Client - Configure with generous timeout for backend operations
    builder.Services.AddHttpClient<IHttpClientService, HttpClientService>(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(60); // 60 seconds for backend operations
    });
    builder.Services.AddHttpClient();

    // Services
    builder.Services.AddScoped<IHttpClientService, HttpClientService>();

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

    // FluentValidation - Register all validators from Shared assembly
    builder.Services.AddValidatorsFromAssemblyContaining<Shared.Validators.LoginDtoValidator>();

    // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();


    // CORS - Read allowed origins from configuration (merges appsettings + env vars)
    // appsettings.json: Cors:AllowedOrigins[0, 1] = localhost origins
    // Env var: Cors__AllowedOrigins__10 = CloudFront URL (in deployment)
    var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
        ?? new[] { "http://localhost:3000" };  // Fallback for local dev

    builder.Services.AddCors(options =>
    {
        options.AddPolicy("AllowFrontend", policy =>
        {
            policy.WithOrigins(allowedOrigins)  // Only allow configured origins
                .AllowAnyMethod()
                .AllowAnyHeader()
                .AllowCredentials();  // Required for cookies/auth headers
        });
    });

    // Health checks
    builder.Services.AddHealthChecks();

    // Rate Limiting (built into .NET 8)
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
                    QueueLimit = 0  // Don't queue requests, reject immediately
                });
        });

        // Policy for login endpoint: 5 attempts per 15 minutes
        options.AddFixedWindowLimiter("login", limiterOptions =>
        {
            limiterOptions.PermitLimit = 5;
            limiterOptions.Window = TimeSpan.FromMinutes(15);
            limiterOptions.QueueProcessingOrder = System.Threading.RateLimiting.QueueProcessingOrder.OldestFirst;
            limiterOptions.QueueLimit = 0;
        });

        // Customize rejection response
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
    app.UseCors("AllowFrontend");

    // JWT Authentication Middleware
    app.UseMiddleware<JwtAuthenticationMiddleware>();

    // Unit Conversion Middleware (after JWT so we have user context)
    app.UseMiddleware<UnitConversionMiddleware>();

    app.UseAuthorization();

    app.MapControllers();
    app.MapHealthChecks("/health").DisableRateLimiting();  // Health checks should not be rate limited

    Log.Information("ApiGateway started successfully");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "ApiGateway failed to start!");
    throw;
}
finally
{
    Log.CloseAndFlush();
}






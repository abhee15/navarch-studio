using AIAgentService.Configuration;
using AIAgentService.Middleware;
using AIAgentService.Services;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Configure Serilog
Log.Logger = new LoggerConfiguration()
    .ReadFrom.Configuration(builder.Configuration)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File("logs/aiagent-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

builder.Host.UseSerilog();

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new() { Title = "AI Agent Service", Version = "v1" });
});

// Configure OpenAI settings
builder.Services.Configure<OpenAISettings>(
    builder.Configuration.GetSection("OpenAI"));

// Add memory cache
builder.Services.AddMemoryCache();

// Register AI services
builder.Services.AddScoped<IPromptTemplateService, PromptTemplateService>();
builder.Services.AddSingleton<ICachingService, CachingService>();
builder.Services.AddSingleton<CostTrackingService>();
builder.Services.AddScoped<INLToMissionService, NLToMissionService>();

// Add CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll",
        builder => builder
            .AllowAnyOrigin()
            .AllowAnyMethod()
            .AllowAnyHeader());
});

// Add health checks
builder.Services.AddHealthChecks();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseSerilogRequestLogging();

// Rate limiting middleware
app.UseMiddleware<AIRateLimitMiddleware>();

app.UseCors("AllowAll");

app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

Log.Information("AIAgentService starting up on port 5005");

app.Run();

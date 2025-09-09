using PspRouter.Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Versioning;
using System.Reflection;
using DotNetEnv;

// Load .env file first, before other configuration sources
try
{
    // Try multiple locations for .env file
    var envPaths = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), ".env"),
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env"),
        Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location) ?? "", ".env")
    };

    bool envLoaded = false;
    foreach (var envPath in envPaths)
    {
        if (File.Exists(envPath))
        {
            Env.Load(envPath);
            Console.WriteLine($"✅ .env file loaded successfully from: {envPath}");
            envLoaded = true;
            break;
        }
    }

    if (!envLoaded)
    {
        Console.WriteLine("⚠️ .env file not found in any of these locations:");
        foreach (var path in envPaths)
        {
            Console.WriteLine($"   - {path}");
        }
    }
}
catch (Exception ex)
{
    Console.WriteLine($"❌ Error loading .env file: {ex.Message}");
}

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

// Configure API versioning
builder.Services.AddApiVersioning(opt =>
{
    opt.DefaultApiVersion = new ApiVersion(1, 0);
    opt.AssumeDefaultVersionWhenUnspecified = true;
    opt.ApiVersionReader = ApiVersionReader.Combine(
        new UrlSegmentApiVersionReader(),
        new QueryStringApiVersionReader("version"),
        new HeaderApiVersionReader("X-Version"),
        new MediaTypeApiVersionReader("ver")
    );
});

builder.Services.AddVersionedApiExplorer(setup =>
{
    setup.GroupNameFormat = "'v'VVV";
    setup.SubstituteApiVersionInUrl = true;
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    var assembly = Assembly.GetExecutingAssembly();
    var version = assembly.GetName().Version?.ToString() ?? throw new InvalidOperationException("Assembly version is required");
    var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;
    
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PSP Router API",
        Version = $"v1 ({informationalVersion})",
        Description = "Payment Service Provider routing API for intelligent transaction routing"
    });
});

// Get ML model path from configuration
var mlModelPath = builder.Configuration["ML:ModelPath"] ?? throw new InvalidOperationException("ML:ModelPath environment variable is required");

// === Register ML Services ===
builder.Services.AddSingleton<IPredictionService>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<PredictionService>>();
    var service = new PredictionService(logger, mlModelPath);
    
    // Load the model asynchronously on startup
    _ = Task.Run(async () =>
    {
        try
        {
            await service.LoadModel();
            logger.LogInformation("ML model loaded successfully during startup");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to load ML model during startup");
        }
    });
    
    return service;
});

// === Register PSP Candidate Provider ===
builder.Services.AddSingleton<IPspCandidateProvider, PspCandidateProvider>();

// === Register ML Router ===
builder.Services.AddScoped(provider =>
{
    var predictionService = provider.GetRequiredService<IPredictionService>();
    var candidateProvider = provider.GetRequiredService<IPspCandidateProvider>();
    var logger = provider.GetRequiredService<ILogger<Router>>();

    var routingSettings = new RoutingSettings();
    builder.Configuration.GetSection("PspRouter:Routing").Bind(routingSettings);
    
    return new Router(predictionService, candidateProvider, logger, routingSettings);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

var logger = app.Services.GetRequiredService<ILogger<Program>>();

Console.WriteLine("=== PSP Router API Ready ===");
Console.WriteLine("API endpoints available at:");
Console.WriteLine("  POST /api/v1/routing/route - Route a transaction");
Console.WriteLine("  GET  /api/v1/routing/health - Check service health");

// Get URLs from configuration
var urls = builder.Configuration["urls"] ?? throw new InvalidOperationException("urls environment variable is required");
var urlList = urls.Split(';', StringSplitOptions.RemoveEmptyEntries);

// Print the URLs to console and logs
foreach (var url in urlList)
{
    var trimmedUrl = url.Trim();
    Console.WriteLine($"  🌐 Application running at: {trimmedUrl}");
    logger.LogInformation("Application running at: {Url}", trimmedUrl);
    
    // Generate Swagger URL for HTTPS URLs
    if (trimmedUrl.StartsWith("https://"))
    {
        var swaggerUrl = $"{trimmedUrl}/swagger";
        Console.WriteLine($"  📚 Swagger UI: {swaggerUrl}");
        logger.LogInformation("Swagger UI available at: {SwaggerUrl}", swaggerUrl);
    }
}

app.Run();

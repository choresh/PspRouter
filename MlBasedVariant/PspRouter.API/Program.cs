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
    var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
    var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;
    
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PSP Router API",
        Version = $"v1 ({informationalVersion})",
        Description = "Payment Service Provider routing API for intelligent transaction routing"
    });
});

// Get .env file variables
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new InvalidOperationException("OPENAI_API_KEY environment variable is required");
var ftModel = Environment.GetEnvironmentVariable("OPENAI_FT_MODEL") ?? throw new InvalidOperationException("OPENAI_FT_MODEL environment variable is required");

// === Register Core Services ===
builder.Services.AddSingleton<IChatClient>(provider => 
    new OpenAIChatClient(apiKey, model: ftModel));

// === Register Router as Scoped (for request-based operations) ===
builder.Services.AddScoped(provider =>
{
    var chat = provider.GetRequiredService<IChatClient>();
    var logger = provider.GetRequiredService<ILogger<PspRouter.Lib.PspRouter>>();

    var routingSettings = new RoutingSettings();
    builder.Configuration.GetSection("PspRouter:Routing").Bind(routingSettings);
    
    return new PspRouter.Lib.PspRouter(chat, logger, routingSettings);
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
var urls = builder.Configuration["urls"] ?? "http://localhost:5174;https://localhost:7149";
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

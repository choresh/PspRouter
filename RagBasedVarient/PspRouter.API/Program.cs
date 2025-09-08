using PspRouter.Lib;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Microsoft.AspNetCore.Mvc.Versioning;
using System.Reflection;

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
    var assembly = System.Reflection.Assembly.GetExecutingAssembly();
    var version = assembly.GetName().Version?.ToString() ?? "1.0.0";
    var informationalVersion = assembly.GetCustomAttribute<System.Reflection.AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? version;
    
    c.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "PSP Router API",
        Version = $"v1 ({informationalVersion})",
        Description = "Payment Service Provider routing API for intelligent transaction routing"
    });
});

// Get .env file variables
var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
var pgConn = Environment.GetEnvironmentVariable("PGVECTOR_CONNSTR"); // "Host=localhost;Username=postgres;Password=postgres;Database=psp_router";

// === Register Core Services ===
builder.Services.AddSingleton<IHealthProvider, PspRouter.API.DummyHealthProvider>();
builder.Services.AddSingleton<IFeeQuoteProvider, PspRouter.API.DummyFeeProvider>();
builder.Services.AddSingleton<IChatClient>(provider => 
    new OpenAIChatClient(apiKey, model: "gpt-4.1"));
builder.Services.AddSingleton<OpenAIEmbeddings>(provider => 
    new OpenAIEmbeddings(apiKey, model: "text-embedding-3-large"));
builder.Services.AddSingleton<IVectorMemory>(provider => 
    new PgVectorMemory(pgConn, "psp_lessons"));
builder.Services.AddSingleton<IContextualBandit>(provider =>
{
    var logger = provider.GetRequiredService<ILogger<ContextualEpsilonGreedyBandit>>();
    return new ContextualEpsilonGreedyBandit(epsilon: 0.1, logger: logger);
});

// === Register Router as Scoped (for request-based operations) ===
builder.Services.AddScoped<PspRouter.Lib.PspRouter>(provider =>
{
    var chat = provider.GetRequiredService<IChatClient>();
    var health = provider.GetRequiredService<IHealthProvider>();
    var fees = provider.GetRequiredService<IFeeQuoteProvider>();
    var bandit = provider.GetRequiredService<IContextualBandit>();
    var memory = provider.GetRequiredService<IVectorMemory>();
    var logger = provider.GetRequiredService<ILogger<PspRouter.Lib.PspRouter>>();
    
    var tools = new List<IAgentTool>
    {
        new GetHealthTool(health),
        new GetFeeQuoteTool(fees, () => new RouteInput("", "", "", "", 0, PaymentMethod.Card))
    };

    return new PspRouter.Lib.PspRouter(chat, health, fees, tools, bandit, memory, logger);
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

// === Initialize Database Schema ===
using (var scope = app.Services.CreateScope())
{
    var memory = scope.ServiceProvider.GetRequiredService<IVectorMemory>();
    try
    {
        await memory.EnsureSchemaAsync(CancellationToken.None);
        Console.WriteLine("✅ Database schema initialized successfully");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"⚠️  Database initialization failed: {ex.Message}");
        Console.WriteLine("   Continuing with in-memory fallback...");
    }
}

var logger = app.Services.GetRequiredService<ILogger<Program>>();

Console.WriteLine("=== PSP Router API Ready ===");
Console.WriteLine("API endpoints available at:");
Console.WriteLine("  POST /api/v1/routing/route - Route a transaction");
Console.WriteLine("  POST /api/v1/routing/outcome - Update transaction outcome");
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

using PspRouter.Lib;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure PSP Router services
var configuration = builder.Configuration;
var apiKey = configuration["OPENAI_API_KEY"] ?? "sk-...";
var pgConn = configuration["PGVECTOR_CONNSTR"] 
     ?? "Host=localhost;Username=postgres;Password=postgres;Database=psp_router";

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

Console.WriteLine("=== PSP Router API Ready ===");
Console.WriteLine("API endpoints available at:");
Console.WriteLine("  POST /api/routing/route - Route a transaction");
Console.WriteLine("  POST /api/routing/outcome - Update transaction outcome");
Console.WriteLine("  GET  /api/routing/health - Check service health");
Console.WriteLine("  Swagger UI: https://localhost:7xxx/swagger");

app.Run();

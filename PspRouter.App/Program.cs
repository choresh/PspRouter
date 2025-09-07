using System.Text.Json;
using PspRouter.Lib;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                config.AddEnvironmentVariables();
            })
            .ConfigureServices((context, services) =>
            {
                var configuration = context.Configuration;
                var apiKey = configuration["OPENAI_API_KEY"] ?? "sk-...";
                var pgConn = configuration["PGVECTOR_CONNSTR"] 
                     ?? "Host=localhost;Username=postgres;Password=postgres;Database=psp_router";

                // === Register Core Services ===
                services.AddSingleton<IHealthProvider, PspRouter.App.DummyHealthProvider>();
                services.AddSingleton<IFeeQuoteProvider, PspRouter.App.DummyFeeProvider>();
                services.AddSingleton<IChatClient>(provider => 
                    new OpenAIChatClient(apiKey, model: "gpt-4.1"));
                services.AddSingleton<OpenAIEmbeddings>(provider => 
                    new OpenAIEmbeddings(apiKey, model: "text-embedding-3-large"));
                services.AddSingleton<IVectorMemory>(provider => 
                    new PgVectorMemory(pgConn, "psp_lessons"));
                services.AddSingleton<IContextualBandit>(provider =>
                {
                    var logger = provider.GetRequiredService<ILogger<ContextualEpsilonGreedyBandit>>();
                    return new ContextualEpsilonGreedyBandit(epsilon: 0.1, logger: logger);
                });

                // === Register Router as Scoped (for request-based operations) ===
                services.AddScoped<PspRouter.Lib.PspRouter>(provider =>
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
            })
            .ConfigureLogging(logging =>
            {
                logging.AddConsole();
                logging.AddDebug();
            })
            .Build();

        // === Initialize Database Schema ===
        using (var scope = host.Services.CreateScope())
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

        // === Application Ready ===
        Console.WriteLine("=== PSP Router Ready ===");
        Console.WriteLine("The PSP Router is now running and ready to process routing requests.");
        Console.WriteLine("Use dependency injection to access the router in your application.");
        Console.WriteLine("Press Ctrl+C to exit...");
        
        // === Keep Running ===
        await host.RunAsync();
    }
}
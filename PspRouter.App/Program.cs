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

        // === Simple Demo Usage ===
        using (var scope = host.Services.CreateScope())
        {
            var router = scope.ServiceProvider.GetRequiredService<PspRouter.Lib.PspRouter>();
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            
            Console.WriteLine("=== PSP Router Ready ===");
            Console.WriteLine("The PSP Router is now running and ready to process routing requests.");
            Console.WriteLine("Use dependency injection to access the router in your application.");
            Console.WriteLine();
            
            // Example usage
            var sampleTx = new RouteInput("M001", "US", "IL", "USD", 100.00m, PaymentMethod.Card, CardScheme.Visa, false, 10, "411111");
            var candidates = new List<PspSnapshot>
            {
                new("Adyen", true, "healthy", 0.89, 50, 200m, true, true),
                new("Stripe", true, "healthy", 0.87, 45, 180m, true, true)
            };
            var context = new RouteContext(sampleTx, candidates, new Dictionary<string, string>(), new Dictionary<string, double>());
            
            try
            {
                var decision = await router.DecideAsync(context, CancellationToken.None);
                logger.LogInformation("Sample routing decision: {PSP} - {Reasoning}", decision.Candidate, decision.Reasoning);
            }
            catch (Exception ex)
            {
                logger.LogWarning("Sample routing failed: {Error}", ex.Message);
            }
        }

        // === Keep Running ===
        Console.WriteLine("Press Ctrl+C to exit...");
        await host.RunAsync();
    }
}
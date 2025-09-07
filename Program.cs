using System.Text.Json;
using PspRouter;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;

class Program
{
    static async Task Main(string[] args)
    {
        // === Build Host with Configuration ===
        var host = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((context, config) =>
            {
                config.AddEnvironmentVariables();
                config.AddJsonFile("appsettings.json", optional: true);
                config.AddJsonFile($"appsettings.{context.HostingEnvironment.EnvironmentName}.json", optional: true);
            })
            .ConfigureServices((context, services) =>
    {
        // === Configuration ===
                var configuration = context.Configuration;
                var apiKey = configuration["OPENAI_API_KEY"] ?? "sk-...";
                var pgConn = configuration["PGVECTOR_CONNSTR"] 
                     ?? "Host=localhost;Username=postgres;Password=postgres;Database=psp_router";

                // === Register Services ===
                services.AddSingleton<IHealthProvider, DummyHealthProvider>();
                services.AddSingleton<IFeeQuoteProvider, DummyFeeProvider>();
                services.AddSingleton<IChatClient>(provider => 
                    new OpenAIChatClient(apiKey, model: "gpt-4.1"));
                services.AddSingleton<OpenAIEmbeddings>(provider => 
                    new OpenAIEmbeddings(apiKey, model: "text-embedding-3-large"));
                services.AddSingleton<IVectorMemory>(provider => 
                    new PgVectorMemory(pgConn, table: "psp_lessons"));
                services.AddSingleton<IContextualBandit>(provider => 
                {
                    var logger = provider.GetRequiredService<ILogger<ContextualEpsilonGreedyBandit>>();
                    return new ContextualEpsilonGreedyBandit(epsilon: 0.1, logger: logger);
                });

                // === Register Router as Scoped (for request-based operations) ===
                services.AddScoped<PspRouter.PspRouter>(provider =>
                {
                    var chat = provider.GetRequiredService<IChatClient>();
                    var health = provider.GetRequiredService<IHealthProvider>();
                    var fees = provider.GetRequiredService<IFeeQuoteProvider>();
                    var bandit = provider.GetRequiredService<IContextualBandit>();
                    var memory = provider.GetRequiredService<IVectorMemory>();
                    var logger = provider.GetRequiredService<ILogger<PspRouter.PspRouter>>();
                    
                    var tools = new List<IAgentTool>
                    {
                        new GetHealthTool(health),
                        new GetFeeQuoteTool(fees, () => new RouteInput("", "", "", "", 0, PaymentMethod.Card))
                    };

                    return new PspRouter.PspRouter(chat, health, fees, tools, bandit, memory, logger);
                });

                // === Register Demo Service ===
                services.AddTransient<PspRouterDemo>();
            })
            .ConfigureLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.AddDebug();
                logging.SetMinimumLevel(LogLevel.Information);
            })
            .Build();

        // === Initialize Database Schema ===
        using (var scope = host.Services.CreateScope())
        {
            var memory = scope.ServiceProvider.GetRequiredService<IVectorMemory>();
        await memory.EnsureSchemaAsync(CancellationToken.None);
        }

        // === Run Demo ===
        using (var scope = host.Services.CreateScope())
        {
            var demo = scope.ServiceProvider.GetRequiredService<PspRouterDemo>();
            await demo.RunAsync();
        }

        // === Graceful Shutdown ===
        await host.StopAsync();
        await host.WaitForShutdownAsync();
    }

}

/// <summary>
/// Demo service that showcases the PSP Router capabilities
/// </summary>
public class PspRouterDemo
{
    private readonly PspRouter.PspRouter _router;
    private readonly IHealthProvider _health;
    private readonly IFeeQuoteProvider _fees;
    private readonly OpenAIEmbeddings _embed;
    private readonly IVectorMemory _memory;
    private readonly ILogger<PspRouterDemo> _logger;

    public PspRouterDemo(
        PspRouter.PspRouter router,
        IHealthProvider health,
        IFeeQuoteProvider fees,
        OpenAIEmbeddings embed,
        IVectorMemory memory,
        ILogger<PspRouterDemo> logger)
    {
        _router = router;
        _health = health;
        _fees = fees;
        _embed = embed;
        _memory = memory;
        _logger = logger;
    }

    public async Task RunAsync()
    {
        Console.WriteLine("=== Enhanced PSP Router Demo ===\n");
        Console.WriteLine("This demo showcases the complete PSP routing system with:");
        Console.WriteLine("• LLM-based intelligent routing");
        Console.WriteLine("• Multi-armed bandit learning");
        Console.WriteLine("• Vector memory for lessons");
        Console.WriteLine("• Comprehensive logging and monitoring");
        Console.WriteLine("• Real PostgreSQL database integration\n");

        // === Test multiple transactions to demonstrate learning ===
        var testTransactions = new[]
        {
            new RouteInput("M001", "US", "IL", "USD", 150.00m, PaymentMethod.Card, CardScheme.Visa, false, 15, "411111"),
            new RouteInput("M002", "GB", "IL", "GBP", 75.50m, PaymentMethod.Card, CardScheme.Mastercard, true, 25, "555555"),
            new RouteInput("M003", "DE", "IL", "EUR", 200.00m, PaymentMethod.PayPal, CardScheme.Unknown, false, 10),
            new RouteInput("M001", "US", "IL", "USD", 300.00m, PaymentMethod.Card, CardScheme.Visa, false, 20, "411111"),
            new RouteInput("M002", "GB", "IL", "GBP", 50.00m, PaymentMethod.Card, CardScheme.Mastercard, false, 5, "555555")
        };

        Console.WriteLine("Processing test transactions...\n");

        for (int i = 0; i < testTransactions.Length; i++)
        {
            var tx = testTransactions[i];
            Console.WriteLine($"--- Transaction {i + 1} ---");
            Console.WriteLine($"Merchant: {tx.MerchantId}, Amount: {tx.Amount} {tx.Currency}, Method: {tx.Method}");

            // Build context
            var candidates = await BuildCandidatesAsync(tx);
            var prefs = new Dictionary<string, string> { ["prefer_low_fees"] = "true" };
            var stats = new Dictionary<string, double>
            {
                ["Adyen_USD_Visa_auth"] = 0.89,
                ["Stripe_USD_Visa_auth"] = 0.87,
                ["Adyen_GBP_Mastercard_auth"] = 0.85,
                ["Stripe_GBP_Mastercard_auth"] = 0.83
            };

            var ctx = new RouteContext(tx, candidates, prefs, stats);

            // Make routing decision
            var decision = await _router.DecideAsync(ctx, CancellationToken.None);
            Console.WriteLine($"Decision: {decision.Candidate}");
            Console.WriteLine($"Reasoning: {decision.Reasoning}");
            
            var method = decision.Features_Used.FirstOrDefault(f => f.StartsWith("method="));
            if (method != null)
            {
                Console.WriteLine($"Method: {method.Split('=')[1]}");
            }

            // Simulate outcome and update learning
            var outcome = SimulateRealisticOutcome(decision, tx);
            _router.UpdateReward(decision, outcome);
            Console.WriteLine($"Outcome: {(outcome.Authorized ? "✓ Authorized" : "✗ Declined")} - Fee: {outcome.FeeAmount:C} - Time: {outcome.ProcessingTimeMs}ms");

            // Add lesson to memory
            try
            {
                var lesson = $"Transaction {tx.MerchantId}|{tx.Currency}|{tx.Method}: {decision.Candidate} {(outcome.Authorized ? "succeeded" : "failed")} with {outcome.ProcessingTimeMs}ms processing time";
                var embedding = await _embed.EmbedAsync(lesson, CancellationToken.None);
                await _memory.AddAsync($"lesson_{i}_{DateTime.UtcNow:yyyyMMddHHmmss}", lesson,
                    new Dictionary<string, string> { ["psp"] = decision.Candidate, ["outcome"] = outcome.Authorized.ToString() },
                    embedding, CancellationToken.None);
                Console.WriteLine($"✓ Lesson added to memory");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Could not add lesson to memory: {ex.Message}");
            }

            Console.WriteLine();
        }

        // === Demonstrate memory search ===
        try
        {
            Console.WriteLine("=== Memory Search Demo ===");
            var queryEmbedding = await _embed.EmbedAsync("Prefer higher auth for USD Visa", CancellationToken.None);
            var top = await _memory.SearchAsync(queryEmbedding, k: 3, CancellationToken.None);

            Console.WriteLine("Top memory results:");
            foreach (var (key, text, meta, score) in top)
            {
                Console.WriteLine($"score={score:0.000} key={key} meta_candidate={meta.GetValueOrDefault("candidate","")}");
                Console.WriteLine(text);
                Console.WriteLine("---");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Memory search failed: {ex.Message}");
        }

        Console.WriteLine("\n=== Learning Summary ===");
        Console.WriteLine("The bandit algorithm has been updated with transaction outcomes.");
        Console.WriteLine("Future routing decisions will incorporate this learning.");
        Console.WriteLine("Memory has been populated with transaction lessons for semantic search.");
        
        Console.WriteLine("\n=== Key Features Demonstrated ===");
        Console.WriteLine("✓ LLM-based routing with structured JSON responses");
        Console.WriteLine("✓ Multi-armed bandit learning (contextual epsilon-greedy)");
        Console.WriteLine("✓ Vector memory for semantic lesson storage");
        Console.WriteLine("✓ Comprehensive logging and monitoring");
        Console.WriteLine("✓ Realistic transaction outcome simulation");
        Console.WriteLine("✓ Graceful fallback to deterministic scoring");
        Console.WriteLine("✓ Reward-based learning from transaction outcomes");
        
        Console.WriteLine("\n=== Production Ready Features ===");
        Console.WriteLine("• Security: No PII sent to LLM, audit trails");
        Console.WriteLine("• Compliance: SCA/3DS enforcement, risk management");
        Console.WriteLine("• Performance: Async operations, efficient algorithms");
        Console.WriteLine("• Reliability: Fallback mechanisms, error handling");
        Console.WriteLine("• Observability: Structured logging, metrics collection");
        Console.WriteLine("• Scalability: Stateless design, horizontal scaling");
        
        Console.WriteLine("\n=== Demo Complete ===");
        Console.WriteLine("The Enhanced PSP Router is ready for production deployment!");
    }

    private async Task<List<PspSnapshot>> BuildCandidatesAsync(RouteInput tx)
    {
        var candidates = new List<PspSnapshot>();
        var pspNames = new[] { "Adyen", "Stripe", "Klarna", "PayPal" };

        foreach (var psp in pspNames)
        {
            if (!CapabilityMatrix.Supports(psp, tx)) continue;

            var (healthState, _) = await _health.GetAsync(psp, CancellationToken.None);
            var (bps, fixedFee) = await _fees.GetAsync(psp, tx, CancellationToken.None);

            candidates.Add(new PspSnapshot(
                Name: psp,
                Supports: true,
                Health: healthState,
                AuthRate30d: GetRealisticAuthRate(psp, tx),
                FeeBps: bps,
                FixedFee: fixedFee,
                Supports3DS: psp is "Adyen" or "Stripe",
                Tokenization: psp is "Adyen" or "Stripe"
            ));
        }

        return candidates;
    }

    private static double GetRealisticAuthRate(string psp, RouteInput tx)
    {
        var baseRates = new Dictionary<string, double>
        {
            ["Adyen"] = 0.89,
            ["Stripe"] = 0.87,
            ["Klarna"] = 0.85,
            ["PayPal"] = 0.82
        };

        var rate = baseRates.GetValueOrDefault(psp, 0.80);
        
        // Adjust based on transaction characteristics
        if (tx.SCARequired) rate -= 0.05;
        if (tx.RiskScore > 30) rate -= 0.10;
        if (tx.Amount > 1000) rate -= 0.03;
        if (tx.Currency != "USD" && tx.Currency != "EUR") rate -= 0.02;

        return Math.Max(0.50, rate); // Minimum 50% auth rate
    }

    private static TransactionOutcome SimulateRealisticOutcome(RouteDecision decision, RouteInput tx)
    {
        var random = new Random();
        var baseAuthRate = GetRealisticAuthRate(decision.Candidate, tx);
        
        var authorized = random.NextDouble() < baseAuthRate;
        var processingTime = random.Next(150, 1800);
        var feeAmount = (tx.Amount * 0.02m) + 0.30m;

        return new TransactionOutcome(
            DecisionId: decision.Decision_Id,
            PspName: decision.Candidate,
            Authorized: authorized,
            TransactionAmount: tx.Amount,
            FeeAmount: feeAmount,
            ProcessingTimeMs: processingTime,
            RiskScore: tx.RiskScore,
            ProcessedAt: DateTime.UtcNow,
            ErrorCode: authorized ? null : "AUTH_DECLINED",
            ErrorMessage: authorized ? null : "Transaction declined by issuer"
        );
    }
}

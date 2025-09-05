using System.Text.Json;
using PspRouter;
using Microsoft.Extensions.Logging;

namespace PspRouter;

/// <summary>
/// Enhanced example demonstrating the complete PSP routing system with learning
/// </summary>
public class EnhancedExample
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Enhanced PSP Router Demo ===\n");

        // Setup logging
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<PspRouter>();

        // Configuration
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "sk-dummy-key";
        var pgConn = Environment.GetEnvironmentVariable("PGVECTOR_CONNSTR") 
                     ?? "Host=localhost;Username=postgres;Password=postgres;Database=psp_router";

        // Initialize components
        var health = new DummyHealthProvider();
        var fees = new DummyFeeProvider();
        var chat = new OpenAIChatClient(apiKey, model: "gpt-4.1");
        var embed = new OpenAIEmbeddings(apiKey, model: "text-embedding-3-large");
        var memory = new PgVectorMemory(pgConn, table: "psp_lessons");
        var bandit = new EpsilonGreedyBandit(epsilon: 0.15); // 15% exploration

        try
        {
            await memory.EnsureSchemaAsync(CancellationToken.None);
            Console.WriteLine("✓ Database schema ensured");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Database connection failed: {ex.Message}");
            Console.WriteLine("Continuing with in-memory components only...\n");
        }

        // Create router with all components
        var tools = new List<IAgentTool>
        {
            new GetHealthTool(health),
            new GetFeeQuoteTool(fees, () => new RouteInput("", "", "", "", 0, PaymentMethod.Card))
        };

        var router = new PspRouter(chat, health, fees, tools, bandit, memory, logger);

        // Test multiple transactions to demonstrate learning
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
            var candidates = await BuildCandidatesAsync(tx, health, fees);
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
            var decision = await router.DecideAsync(ctx, CancellationToken.None);
            Console.WriteLine($"Decision: {decision.Candidate}");
            Console.WriteLine($"Reasoning: {decision.Reasoning}");
            Console.WriteLine($"Method: {string.Join(", ", decision.Features_Used.Where(f => f.StartsWith("method=")).Select(f => f.Split('=')[1]))}");

            // Simulate outcome and update learning
            var outcome = SimulateRealisticOutcome(decision, tx);
            router.UpdateReward(decision, outcome);
            Console.WriteLine($"Outcome: {(outcome.Authorized ? "✓ Authorized" : "✗ Declined")} - Fee: {outcome.FeeAmount:C} - Time: {outcome.ProcessingTimeMs}ms");

            // Add lesson to memory
            try
            {
                var lesson = $"Transaction {tx.MerchantId}|{tx.Currency}|{tx.Method}: {decision.Candidate} {(outcome.Authorized ? "succeeded" : "failed")} with {outcome.ProcessingTimeMs}ms processing time";
                var embedding = await embed.EmbedAsync(lesson, CancellationToken.None);
                await memory.AddAsync($"lesson_{i}_{DateTime.UtcNow:yyyyMMddHHmmss}", lesson,
                    new Dictionary<string, string> { ["psp"] = decision.Candidate, ["outcome"] = outcome.Authorized.ToString() },
                    embedding, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Could not add lesson to memory: {ex.Message}");
            }

            Console.WriteLine();
        }

        Console.WriteLine("=== Learning Summary ===");
        Console.WriteLine("The bandit algorithm has been updated with transaction outcomes.");
        Console.WriteLine("Future routing decisions will incorporate this learning.");
        Console.WriteLine("Memory has been populated with transaction lessons for semantic search.");
    }

    private static async Task<List<PspSnapshot>> BuildCandidatesAsync(RouteInput tx, IHealthProvider health, IFeeQuoteProvider fees)
    {
        var candidates = new List<PspSnapshot>();
        var pspNames = new[] { "Adyen", "Stripe", "Klarna", "PayPal" };

        foreach (var psp in pspNames)
        {
            if (!CapabilityMatrix.Supports(psp, tx)) continue;

            var (healthState, _) = await health.GetAsync(psp, CancellationToken.None);
            var (bps, fixedFee) = await fees.GetAsync(psp, tx, CancellationToken.None);

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

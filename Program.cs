using System.Text.Json;
using PspRouter;
using Microsoft.Extensions.Logging;

class Program
{
    static async Task Main()
    {
        // === Configuration ===
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "sk-...";
        var pgConn = Environment.GetEnvironmentVariable("PGVECTOR_CONNSTR") 
                     ?? "Host=localhost;Username=postgres;Password=postgres;Database=psp_router";

        // === Logging Setup ===
        using var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        var logger = loggerFactory.CreateLogger<PspRouter.PspRouter>();

        // === Providers & Clients ===
        var health = new DummyHealthProvider();   // replace with real metrics
        var fees   = new DummyFeeProvider();      // replace with real fee tables
        var chat   = new OpenAIChatClient(apiKey, model: "gpt-4.1");
        var embed  = new OpenAIEmbeddings(apiKey, model: "text-embedding-3-large");
        var memory = new PgVectorMemory(pgConn, table: "psp_lessons");
        var bandit = new ContextualEpsilonGreedyBandit(epsilon: 0.1, logger: loggerFactory.CreateLogger<ContextualEpsilonGreedyBandit>()); // 10% exploration
        
        await memory.EnsureSchemaAsync(CancellationToken.None);

        // === Example transaction ===
        var tx = new RouteInput(
            MerchantId: "M123",
            BuyerCountry: "US",
            MerchantCountry: "IL",
            Currency: "USD",
            Amount: 120.00m,
            Method: PaymentMethod.Card,
            Scheme: CardScheme.Visa,
            SCARequired: false,
            RiskScore: 18,
            Bin: "411111"
        );

        // Build candidates from capability matrix + live provider data
        var candidateNames = new[] { "Adyen", "Stripe", "Klarna", "PayPal" };
        var snapshots = new List<PspSnapshot>();
        foreach (var psp in candidateNames)
        {
            bool supports = CapabilityMatrix.Supports(psp, tx);
            if (!supports) continue;

            var (healthState, _) = await health.GetAsync(psp, CancellationToken.None);
            var (bps, fixedFee)  = await fees.GetAsync(psp, tx, CancellationToken.None);
            snapshots.Add(new PspSnapshot(
                Name: psp,
                Supports: supports,
                Health: healthState,
                AuthRate30d: 0.0,      // plug your rolling metric here
                FeeBps: bps,
                FixedFee: fixedFee,
                Supports3DS: psp is "Adyen" or "Stripe",
                Tokenization: psp is "Adyen" or "Stripe"
            ));
        }

        var prefs = new Dictionary<string,string> { ["prefer_low_fees"] = "true" };
        var stats = new Dictionary<string,double> {
            ["Adyen_USD_Visa_auth"] = 0.89,
            ["Stripe_USD_Visa_auth"] = 0.87
        };

        var ctx = new RouteContext(tx, snapshots, prefs, stats);

        // === Tools exposed to the LLM ===
        var tools = new List<IAgentTool>
        {
            new GetHealthTool(health),
            new GetFeeQuoteTool(fees, () => tx)
        };

        // === Route decision ===
        var router = new PspRouter.PspRouter(chat, health, fees, tools, bandit, memory, logger);
        var decision = await router.DecideAsync(ctx, CancellationToken.None);
        Console.WriteLine("Decision:");
        Console.WriteLine(JsonSerializer.Serialize(decision, new JsonSerializerOptions { WriteIndented = true }));

        // === Simulate transaction outcome and update learning ===
        var outcome = SimulateTransactionOutcome(decision, tx);
        router.UpdateReward(decision, outcome);
        Console.WriteLine($"\nSimulated outcome: Authorized={outcome.Authorized}, Fee={outcome.FeeAmount:C}, ProcessingTime={outcome.ProcessingTimeMs}ms");

        // === Learning: add a 'lesson' to pgvector and query it ===
        try
        {
            var lessonText = $"""Segment {tx.MerchantId}|{tx.MerchantCountry}|{tx.Currency}|{tx.Scheme}: Chose {decision.Candidate} with auth bias over fees; consider 3DS when SCA required.""";
            var emb = await embed.EmbedAsync(lessonText, CancellationToken.None);
            await memory.AddAsync(Guid.NewGuid().ToString("N"), lessonText,
                new Dictionary<string,string> { ["candidate"] = decision.Candidate }, emb, CancellationToken.None);

            var queryEmbedding = await embed.EmbedAsync("Prefer higher auth for USD Visa", CancellationToken.None);
            var top = await memory.SearchAsync(queryEmbedding, k: 3, CancellationToken.None);

            Console.WriteLine("\nTop memory results:");
            foreach (var (key, text, meta, score) in top)
            {
                Console.WriteLine($"score={score:0.000} key={key} meta_candidate={meta.GetValueOrDefault("candidate","")}");
                Console.WriteLine(text);
                Console.WriteLine("---");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Memory operations failed: {ex.Message}");
        }

        Console.WriteLine("\n=== PSP Router Ready ===");
        Console.WriteLine("The enhanced PSP Router is now running with:");
        Console.WriteLine("• LLM-based intelligent routing");
        Console.WriteLine("• Multi-armed bandit learning");
        Console.WriteLine("• Vector memory for lessons");
        Console.WriteLine("• Comprehensive logging and monitoring");
        Console.WriteLine("• Real PostgreSQL database integration");
    }

    private static TransactionOutcome SimulateTransactionOutcome(RouteDecision decision, RouteInput tx)
    {
        // Simulate realistic transaction outcomes based on PSP choice
        var random = new Random();
        var baseAuthRate = decision.Candidate switch
        {
            "Adyen" => 0.89,
            "Stripe" => 0.87,
            "Klarna" => 0.85,
            "PayPal" => 0.82,
            _ => 0.80
        };

        // Adjust auth rate based on transaction characteristics
        var adjustedAuthRate = baseAuthRate;
        if (tx.SCARequired) adjustedAuthRate -= 0.05; // SCA reduces auth rate
        if (tx.RiskScore > 30) adjustedAuthRate -= 0.10; // High risk reduces auth rate
        if (tx.Amount > 1000) adjustedAuthRate -= 0.03; // Large amounts reduce auth rate

        var authorized = random.NextDouble() < adjustedAuthRate;
        var processingTime = random.Next(200, 2000); // 200ms to 2s
        var feeAmount = (tx.Amount * 0.02m) + 0.30m; // 2% + 30 cents

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

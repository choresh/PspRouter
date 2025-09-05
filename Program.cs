using System.Text.Json;
using PspRouter;

class Program
{
    static async Task Main()
    {
        // === Configuration ===
        var apiKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? "sk-...";
        var pgConn = Environment.GetEnvironmentVariable("PGVECTOR_CONNSTR") 
                     ?? "Host=localhost;Username=postgres;Password=postgres;Database=psp_router";

        // === Providers & Clients ===
        var health = new DummyHealthProvider();   // replace with real metrics
        var fees   = new DummyFeeProvider();      // replace with real fee tables
        var chat   = new OpenAIChatClient(apiKey, model: "gpt-4.1");
        var embed  = new OpenAIEmbeddings(apiKey, model: "text-embedding-3-large");
        var memory = new PgVectorMemory(pgConn, table: "psp_lessons");
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
        var router = new PspRouter.PspRouter(chat, health, fees, tools);
        var decision = await router.DecideAsync(ctx, CancellationToken.None);
        Console.WriteLine("Decision:");
        Console.WriteLine(JsonSerializer.Serialize(decision, new JsonSerializerOptions { WriteIndented = true }));

        // === Learning: add a 'lesson' to pgvector and query it ===
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
}

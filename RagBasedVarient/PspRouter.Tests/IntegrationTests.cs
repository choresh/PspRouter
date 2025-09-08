using PspRouter.Lib;
using Microsoft.Extensions.Logging;

namespace PspRouter.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task TestCompleteRoutingFlow()
    {
        // This test demonstrates the complete PSP routing system
        // with LLM-based intelligent routing, bandit learning, and vector memory
        
        // Arrange - Create mock services
        var healthProvider = new PspRouter.API.DummyHealthProvider();
        var feeProvider = new PspRouter.API.DummyFeeProvider();
        var chatClient = new PspRouter.API.DummyChatClient();
        var memory = new MockVectorMemory();
        var logger = new MockLogger<PspRouter.Lib.PspRouter>();
        var bandit = new ContextualEpsilonGreedyBandit(epsilon: 0.1, logger: new MockLogger<ContextualEpsilonGreedyBandit>());
        
        var tools = new List<IAgentTool>
        {
            new GetHealthTool(healthProvider),
            new GetFeeQuoteTool(feeProvider, () => new RouteInput("", "", "", "", 0, PaymentMethod.Card))
        };

        var router = new PspRouter.Lib.PspRouter(chatClient, healthProvider, feeProvider, tools, bandit, memory, logger);

        // Test multiple transactions to demonstrate learning
        var testTransactions = new[]
        {
            new RouteInput("M001", "US", "IL", "USD", 150.00m, PaymentMethod.Card, CardScheme.Visa, false, 15, "411111"),
            new RouteInput("M002", "GB", "IL", "GBP", 75.50m, PaymentMethod.Card, CardScheme.Mastercard, true, 25, "555555"),
            new RouteInput("M003", "DE", "IL", "EUR", 200.00m, PaymentMethod.PayPal, CardScheme.Unknown, false, 10),
            new RouteInput("M001", "US", "IL", "USD", 300.00m, PaymentMethod.Card, CardScheme.Visa, false, 20, "411111"),
            new RouteInput("M002", "GB", "IL", "GBP", 50.00m, PaymentMethod.Card, CardScheme.Mastercard, false, 5, "555555")
        };

        // Act & Assert - Process each transaction
        for (int i = 0; i < testTransactions.Length; i++)
        {
            var tx = testTransactions[i];
            
            // Build context
            var candidates = await BuildCandidatesAsync(tx, healthProvider);
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
            
            // Verify decision was made
            Assert.NotNull(decision);
            Assert.NotNull(decision.Candidate);
            Assert.NotNull(decision.Reasoning);
            
            // Simulate transaction outcome
            var outcome = SimulateRealisticOutcome(decision, tx);
            
            // Update learning
            router.UpdateReward(decision, outcome);
            
            // Verify learning update succeeded (no exceptions)
            Assert.True(true); // If we get here, the update succeeded
        }
    }

    private async Task<List<PspSnapshot>> BuildCandidatesAsync(RouteInput tx, IHealthProvider health)
    {
        var candidates = new List<PspSnapshot>();
        
        // Add supported PSPs based on capability matrix
        if (CapabilityMatrix.Supports("Adyen", tx))
        {
            var (healthStatus, latency) = await health.GetAsync("Adyen", CancellationToken.None);
            candidates.Add(new("Adyen", true, healthStatus, GetRealisticAuthRate(tx, "Adyen"), latency, GetRealisticFeeBps(tx, "Adyen"), true, true));
        }
        
        if (CapabilityMatrix.Supports("Stripe", tx))
        {
            var (healthStatus, latency) = await health.GetAsync("Stripe", CancellationToken.None);
            candidates.Add(new("Stripe", true, healthStatus, GetRealisticAuthRate(tx, "Stripe"), latency, GetRealisticFeeBps(tx, "Stripe"), true, true));
        }
        
        if (CapabilityMatrix.Supports("Klarna", tx))
        {
            var (healthStatus, latency) = await health.GetAsync("Klarna", CancellationToken.None);
            candidates.Add(new("Klarna", true, healthStatus, GetRealisticAuthRate(tx, "Klarna"), latency, GetRealisticFeeBps(tx, "Klarna"), true, true));
        }
        
        return candidates;
    }

    private int GetRealisticFeeBps(RouteInput tx, string psp)
    {
        // Simulate realistic fee structures
        return psp switch
        {
            "Adyen" => tx.Method == PaymentMethod.Card ? 200 : 300,
            "Stripe" => tx.Method == PaymentMethod.Card ? 180 : 250,
            "Klarna" => tx.Method == PaymentMethod.KlarnaPayLater ? 400 : 350,
            _ => 250
        };
    }

    private double GetRealisticAuthRate(RouteInput tx, string psp)
    {
        // Simulate realistic authorization rates
        var baseRate = psp switch
        {
            "Adyen" => 0.89,
            "Stripe" => 0.87,
            "Klarna" => 0.85,
            _ => 0.80
        };
        
        // Adjust based on transaction characteristics
        if (tx.SCARequired) baseRate -= 0.05; // SCA reduces auth rate
        if (tx.RiskScore > 20) baseRate -= 0.03; // High risk reduces auth rate
        if (tx.Amount > 1000) baseRate -= 0.02; // Large amounts reduce auth rate
        
        return Math.Max(0.70, baseRate); // Minimum 70% auth rate
    }

    private TransactionOutcome SimulateRealisticOutcome(RouteDecision decision, RouteInput tx)
    {
        var random = new Random();
        var authRate = GetRealisticAuthRate(tx, decision.Candidate);
        var feeBps = GetRealisticFeeBps(tx, decision.Candidate);
        
        var authorized = random.NextDouble() < authRate;
        var processingTime = random.Next(50, 200); // 50-200ms
        var riskScore = tx.RiskScore + random.Next(-5, 6); // Add some variance
        
        return new TransactionOutcome(
            DecisionId: Guid.NewGuid().ToString(),
            PspName: decision.Candidate,
            Authorized: authorized,
            TransactionAmount: tx.Amount,
            FeeAmount: tx.Amount * (feeBps / 10000m),
            ProcessingTimeMs: processingTime,
            RiskScore: Math.Max(0, Math.Min(100, riskScore)),
            ProcessedAt: DateTime.UtcNow,
            ErrorCode: authorized ? null : "AUTH_DECLINED",
            ErrorMessage: authorized ? null : "Transaction declined by PSP"
        );
    }
}

// Mock implementations for testing
public class MockVectorMemory : IVectorMemory
{
    public Task EnsureSchemaAsync(CancellationToken ct) => Task.CompletedTask;
    public Task AddAsync(string key, string text, Dictionary<string, string> meta, float[] embedding, CancellationToken ct) => Task.CompletedTask;
    public Task<IReadOnlyList<(string key, string text, Dictionary<string, string> meta, double score)>> SearchAsync(float[] query, int limit, CancellationToken ct) => 
        Task.FromResult<IReadOnlyList<(string, string, Dictionary<string, string>, double)>>(new List<(string, string, Dictionary<string, string>, double)>());
}

public class MockLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}

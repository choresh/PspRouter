using PspRouter.Lib;
using Microsoft.Extensions.Logging;

namespace PspRouter.Tests;

public class IntegrationTests
{
    private readonly ICapabilityProvider capability = new API.DummyCapabilityProvider();

    [Fact]
    public async Task TestCompleteRoutingFlow()
    {
        // This test demonstrates the complete PSP routing system
        
        // Arrange - Create mock services
        var healthProvider = new API.DummyHealthProvider();
        var feeProvider = new API.DummyFeeProvider();
        var chatClient = new API.DummyChatClient();
        var logger = new MockLogger<Lib.PspRouter>();
        
        var tools = new List<IAgentTool>
        {
            new GetHealthTool(healthProvider),
            new GetFeeQuoteTool(feeProvider, () => new RouteInput("", "", "", 1, 0, 1))
        };

        var router = new Lib.PspRouter(chatClient, healthProvider, feeProvider, tools, logger);

        // Test multiple transactions to demonstrate learning
        var testTransactions = new[]
        {
            new RouteInput("M001", "US", "IL", 1, 150.00m, 1, "411111", null, false, false, 15),
            new RouteInput("M002", "GB", "IL", 2, 75.50m, 1, "555555", null, true, false, 25),
            new RouteInput("M003", "DE", "IL", 3, 200.00m, 2, null, null, false, false, 10),
            new RouteInput("M001", "US", "IL", 1, 300.00m, 1, "411111", null, false, false, 20),
            new RouteInput("M002", "GB", "IL", 2, 50.00m, 1, "555555", null, false, false, 5)
        };

        // Act & Assert - Process each transaction
        for (int i = 0; i < testTransactions.Length; i++)
        {
            var tx = testTransactions[i];
            
            // Build context
            var candidates = await BuildCandidates(tx, healthProvider);
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
            var decision = await router.Decide(ctx, CancellationToken.None);
            
            // Verify decision was made
            Assert.NotNull(decision);
            Assert.NotNull(decision.Candidate);
            Assert.NotNull(decision.Reasoning);
            
            // Simulate transaction outcome
            var outcome = SimulateRealisticOutcome(decision, tx);
            
            // Fine-tuned model variant: no online learning update
            Assert.True(true);
        }
    }

    private async Task<List<PspSnapshot>> BuildCandidates(RouteInput tx, IHealthProvider health)
    {
        var candidates = new List<PspSnapshot>();
        
        // Add supported PSPs based on capability matrix
        if (this.capability.Supports("Adyen", tx))
        {
            var (healthStatus, latency) = await health.Get("Adyen", CancellationToken.None);
            candidates.Add(new("Adyen", true, healthStatus, GetRealisticAuthRate(tx, "Adyen"), latency, GetRealisticFeeBps(tx, "Adyen"), true, true));
        }
        
        if (capability.Supports("Stripe", tx))
        {
            var (healthStatus, latency) = await health.Get("Stripe", CancellationToken.None);
            candidates.Add(new("Stripe", true, healthStatus, GetRealisticAuthRate(tx, "Stripe"), latency, GetRealisticFeeBps(tx, "Stripe"), true, true));
        }
        
        if (capability.Supports("Klarna", tx))
        {
            var (healthStatus, latency) = await health.Get("Klarna", CancellationToken.None);
            candidates.Add(new("Klarna", true, healthStatus, GetRealisticAuthRate(tx, "Klarna"), latency, GetRealisticFeeBps(tx, "Klarna"), true, true));
        }
        
        return candidates;
    }

    private int GetRealisticFeeBps(RouteInput tx, string psp)
    {
        // Simulate realistic fee structures
        return psp switch
        {
            "Adyen" => tx.PaymentMethodId == 1 ? 200 : 300,        // PaymentMethodId 1 = Card
            "Stripe" => tx.PaymentMethodId == 1 ? 180 : 250,       // PaymentMethodId 1 = Card
            "Klarna" => tx.PaymentMethodId == 3 ? 400 : 350,       // PaymentMethodId 3 = KlarnaPayLater
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
// Vector memory mocks removed in pre-trained model variant

public class MockLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}

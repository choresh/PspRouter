using PspRouter.Lib;
using Microsoft.Extensions.Logging;
using DotNetEnv;

namespace PspRouter.Tests;

public class IntegrationTests
{
    [Fact]
    public async Task TestCompleteRoutingFlow()
    {
        // This test demonstrates the complete ML-enhanced PSP routing system
        // with realistic routing calls and feedback loops
        
        // Load environment variables from .env file
        try
        {
            Env.Load();
        }
        catch (Exception)
        {
            // If .env file doesn't exist, set default values
            Environment.SetEnvironmentVariable("PSPROUTER_DB_CONNECTION", 
                "Server=localhost;Database=PaymentDB;Trusted_Connection=true;TrustServerCertificate=true;");
        }
        
        // 1. Setup the system with console logging
        var loggerFactory = LoggerFactory.Create(builder =>
            builder.AddConsole().SetMinimumLevel(LogLevel.Information));
        
        var logger = loggerFactory.CreateLogger<IntegrationTests>();
        var settings = new PspCandidateSettings
        {
            TrustServerCertificate = true,
            QueryTimeoutSeconds = 30,
            EnableRetry = true,
            MaxRetryAttempts = 3,
            RetryDelayMs = 1000
        };
        
        // Create ML performance predictor
        var performancePredictor = new PspPerformancePredictor(loggerFactory.CreateLogger<PspPerformancePredictor>(), settings);
        
        // Create ML retraining service
        var retrainingService = new MLModelRetrainingService(loggerFactory.CreateLogger<MLModelRetrainingService>(), performancePredictor);
        
        // Create PSP candidate provider with ML enhancement
        var candidateProvider = new PspCandidateProvider(loggerFactory.CreateLogger<PspCandidateProvider>(), settings, performancePredictor, retrainingService);
        
        // Create prediction service (mock for now)
        var predictionService = new MockPredictionService();
        
        // Create router
        var router = new Router(predictionService, candidateProvider, loggerFactory.CreateLogger<Router>());
        
        logger.LogInformation("🚀 Starting ML-Enhanced PSP Routing Integration Test");
        
        // 2. Simulate multiple routing scenarios
        var routingScenarios = CreateRealisticRoutingScenarios();
        var allOutcomes = new List<TransactionOutcome>();
        
        foreach (var scenario in routingScenarios)
        {
            logger.LogInformation("📋 Processing scenario: {Scenario}", scenario.Description);
            
            // Get routing decision
            var decision = await router.Decide(scenario.Transaction, CancellationToken.None);
            
            logger.LogInformation("🎯 Routing decision: {Psp} (Schema: {Schema}, Reasoning: {Reasoning})", 
                decision.Candidate, decision.Schema_Version, decision.Reasoning);
            
            // Simulate realistic transaction outcome
            var outcome = SimulateRealisticOutcome(decision, scenario.Transaction);
            allOutcomes.Add(outcome);
            
            logger.LogInformation("💳 Transaction outcome: {Authorized} in {ProcessingTime}ms", 
                outcome.Authorized ? "✅ Authorized" : "❌ Declined", outcome.ProcessingTimeMs);
            
            // Process feedback to improve future routing
            var feedback = new TransactionFeedback(
                DecisionId: outcome.DecisionId,
                MerchantId: scenario.Transaction.MerchantId,
                PspName: outcome.PspName,
                Authorized: outcome.Authorized,
                TransactionAmount: outcome.TransactionAmount,
                FeeAmount: outcome.FeeAmount,
                ProcessingTimeMs: outcome.ProcessingTimeMs,
                RiskScore: outcome.RiskScore,
                ProcessedAt: outcome.ProcessedAt,
                ErrorCode: outcome.ErrorCode,
                ErrorMessage: outcome.ErrorMessage,
                RoutingMethod: "ml"
            );
            
            await candidateProvider.ProcessFeedback(feedback);
            
            logger.LogInformation("🔄 Feedback processed for PSP {PspName}", outcome.PspName);
            
            // Small delay to simulate real-world timing
            await Task.Delay(100);
        }
        
        // 3. Analyze results
        logger.LogInformation("📊 Integration Test Results:");
        logger.LogInformation("   Total Transactions: {Count}", allOutcomes.Count);
        logger.LogInformation("   Success Rate: {SuccessRate:P2}", 
            (double)allOutcomes.Count(o => o.Authorized) / allOutcomes.Count);
        logger.LogInformation("   Average Processing Time: {AvgTime:F0}ms", 
            allOutcomes.Average(o => o.ProcessingTimeMs));
        
        // 4. Test ML model retraining
        logger.LogInformation("🧠 Testing ML model retraining...");
        
        if (retrainingService.ShouldRetrainModels())
        {
            logger.LogInformation("   Models need retraining - triggering retraining...");
            await retrainingService.RetrainAllModelsAsync();
            logger.LogInformation("   ✅ ML models retrained successfully");
        }
        else
        {
            logger.LogInformation("   Models are up-to-date, no retraining needed");
        }
        
        // 5. Test improved routing after feedback
        logger.LogInformation("🎯 Testing improved routing after feedback...");
        
        var testTransaction = new RouteInput(
            MerchantId: "MERCHANT_001",
            BuyerCountry: "US",
            MerchantCountry: "US",
            CurrencyId: 1,
            Amount: 150.00m,
            PaymentMethodId: 1,
            PaymentCardBin: "411111",
            RiskScore: 25,
            SCARequired: false
        );
        
        var improvedDecision = await router.Decide(testTransaction, CancellationToken.None);
        logger.LogInformation("   Improved routing decision: {Psp} (Schema: {Schema})", 
            improvedDecision.Candidate, improvedDecision.Schema_Version);
        
        // 6. Verify system state
        var allCandidates = await candidateProvider.GetAllCandidates();
        logger.LogInformation("📈 Final PSP Performance Summary:");
        
        foreach (var candidate in allCandidates)
        {
            logger.LogInformation("   {PspName}: Auth Rate {AuthRate:P2}, Avg Time {AvgTime}ms, Health {Health}", 
                candidate.Name, candidate.CurrentAuthRate, candidate.AverageProcessingTime, candidate.Health);
        }
        
        logger.LogInformation("✅ ML-Enhanced PSP Routing Integration Test completed successfully!");
        
        // Assertions
        Assert.True(allOutcomes.Count > 0, "Should have processed transactions");
        Assert.True(allOutcomes.Any(o => o.Authorized), "Should have some successful transactions");
        Assert.True(allOutcomes.Average(o => o.ProcessingTimeMs) < 5000, "Average processing time should be reasonable");
    }
    
    private List<RoutingScenario> CreateRealisticRoutingScenarios()
    {
        return new List<RoutingScenario>
        {
            new("Low Risk Card Transaction", new RouteInput(
                MerchantId: "MERCHANT_001",
                BuyerCountry: "US",
                MerchantCountry: "US",
                CurrencyId: 1, // USD
                Amount: 25.99m,
                PaymentMethodId: 1, // Card
                PaymentCardBin: "411111",
                RiskScore: 15,
                SCARequired: false
            )),
            
            new("High Risk Large Transaction", new RouteInput(
                MerchantId: "MERCHANT_002",
                BuyerCountry: "UK",
                MerchantCountry: "US",
                CurrencyId: 1, // USD
                Amount: 2500.00m,
                PaymentMethodId: 1, // Card
                PaymentCardBin: "555555",
                RiskScore: 75,
                SCARequired: true
            )),
            
            new("Klarna Pay Later", new RouteInput(
                MerchantId: "MERCHANT_003",
                BuyerCountry: "DE",
                MerchantCountry: "US",
                CurrencyId: 1, // USD
                Amount: 150.00m,
                PaymentMethodId: 3, // KlarnaPayLater
                PaymentCardBin: null,
                RiskScore: 30,
                SCARequired: false
            )),
            
            new("Medium Risk International", new RouteInput(
                MerchantId: "MERCHANT_004",
                BuyerCountry: "FR",
                MerchantCountry: "US",
                CurrencyId: 2, // EUR
                Amount: 500.00m,
                PaymentMethodId: 1, // Card
                PaymentCardBin: "400000",
                RiskScore: 45,
                SCARequired: true
            )),
            
            new("Low Amount Quick Transaction", new RouteInput(
                MerchantId: "MERCHANT_005",
                BuyerCountry: "US",
                MerchantCountry: "US",
                CurrencyId: 1, // USD
                Amount: 9.99m,
                PaymentMethodId: 1, // Card
                PaymentCardBin: "411111",
                RiskScore: 10,
                SCARequired: false
            )),
            
            new("High Risk Declined Pattern", new RouteInput(
                MerchantId: "MERCHANT_006",
                BuyerCountry: "XX", // High-risk country
                MerchantCountry: "US",
                CurrencyId: 1, // USD
                Amount: 1200.00m,
                PaymentMethodId: 1, // Card
                PaymentCardBin: "600000",
                RiskScore: 85,
                SCARequired: true
            )),
            
            new("Repeat Customer", new RouteInput(
                MerchantId: "MERCHANT_001", // Same merchant as first transaction
                BuyerCountry: "US",
                MerchantCountry: "US",
                CurrencyId: 1, // USD
                Amount: 75.50m,
                PaymentMethodId: 1, // Card
                PaymentCardBin: "411111",
                RiskScore: 20,
                SCARequired: false
            )),
            
            new("Weekend Transaction", new RouteInput(
                MerchantId: "MERCHANT_007",
                BuyerCountry: "US",
                MerchantCountry: "US",
                CurrencyId: 1, // USD
                Amount: 200.00m,
                PaymentMethodId: 1, // Card
                PaymentCardBin: "411111",
                RiskScore: 35,
                SCARequired: false
            ))
        };
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
public class MockLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}

/// <summary>
/// Represents a routing scenario for testing
/// </summary>
public record RoutingScenario(string Description, RouteInput Transaction);

/// <summary>
/// Represents the outcome of a transaction
/// </summary>
public record TransactionOutcome(
    string DecisionId,
    string PspName,
    bool Authorized,
    decimal TransactionAmount,
    decimal FeeAmount,
    int ProcessingTimeMs,
    int RiskScore,
    DateTime ProcessedAt,
    string? ErrorCode = null,
    string? ErrorMessage = null
);

/// <summary>
/// Mock prediction service for testing
/// </summary>
public class MockPredictionService : IPredictionService
{
    public Task<MLRoutingPrediction?> PredictBestPsp(RouteContext context, CancellationToken cancellationToken = default)
    {
        // Return a mock prediction
        var prediction = new MLRoutingPrediction
        {
            RecommendedPsp = "Adyen",
            SuccessProbability = 0.89f,
            ModelVersion = "1.0.0",
            PredictionTimestamp = DateTime.UtcNow
        };
        return Task.FromResult<MLRoutingPrediction?>(prediction);
    }

    public bool IsModelLoaded => true;
}

/// <summary>
/// Mock PSP candidate provider for testing
/// </summary>
public class MockPspCandidateProvider : IPspCandidateProvider
{
    private readonly List<PspCandidate> _mockCandidates;

    public MockPspCandidateProvider()
    {
        _mockCandidates = new List<PspCandidate>
        {
            new PspCandidate(
                Name: "Adyen",
                Supports: true,
                Health: "green",
                AuthRate30d: 0.89,
                FeeBps: 200,
                FixedFee: 0.25m,
                Supports3DS: true,
                Tokenization: true,
                AverageProcessingTimeMs: 1200
            ),
            new PspCandidate(
                Name: "Stripe",
                Supports: true,
                Health: "green",
                AuthRate30d: 0.87,
                FeeBps: 180,
                FixedFee: 0.30m,
                Supports3DS: true,
                Tokenization: true,
                AverageProcessingTimeMs: 1100
            ),
            new PspCandidate(
                Name: "Klarna",
                Supports: true,
                Health: "yellow",
                AuthRate30d: 0.85,
                FeeBps: 400,
                FixedFee: 0.00m,
                Supports3DS: false,
                Tokenization: false,
                AverageProcessingTimeMs: 1500
            )
        };
    }

    public async Task<IReadOnlyList<PspSnapshot>> GetCandidates(RouteInput transaction, CancellationToken cancellationToken = default)
    {
        // Return mock candidates based on transaction
        var candidates = _mockCandidates
            .Where(c => c.Supports)
            .Where(c => !(transaction.SCARequired && transaction.PaymentMethodId == 1 && !c.Supports3DS))
            .Select(c => new PspSnapshot(
                c.Name,
                c.Supports,
                c.Health,
                c.CurrentAuthRate,
                c.FeeBps,
                c.FixedFee,
                c.Supports3DS,
                c.Tokenization
            ))
            .ToList();

        return await Task.FromResult(candidates);
    }

    public async Task ProcessFeedback(TransactionFeedback feedback, CancellationToken cancellationToken = default)
    {
        // Mock feedback processing - just log it
        await Task.CompletedTask;
    }

    public async Task<IReadOnlyList<PspCandidate>> GetAllCandidates(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult<IReadOnlyList<PspCandidate>>(_mockCandidates);
    }

    public async Task<PspCandidate?> GetCandidate(string pspName, CancellationToken cancellationToken = default)
    {
        var candidate = _mockCandidates.FirstOrDefault(c => c.Name == pspName);
        return await Task.FromResult(candidate);
    }
}

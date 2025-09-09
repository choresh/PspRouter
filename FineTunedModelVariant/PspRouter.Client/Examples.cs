using Microsoft.Extensions.Logging;

namespace PspRouter.Client;

/// <summary>
/// Example usage of PspRouterClient for sender-side integration
/// </summary>
public static class PspRouterClientExamples
{
    /// <summary>
    /// Example: Basic usage with connection string
    /// </summary>
    public static async Task<List<PspSnapshot>> GetAvailablePspsExample(string connectionString)
    {
        // Create client with connection string
        var client = PspRouterClientFactory.Create(connectionString);
        
        // Get all available PSPs
        var psps = await client.GetAvailablePspsAsync();
        
        Console.WriteLine($"Retrieved {psps.Count} PSPs:");
        foreach (var psp in psps)
        {
            Console.WriteLine($"- {psp.Name}: {psp.Health} health, {psp.AuthRate30d:P2} auth rate, {psp.FeeBps}bps fee");
        }
        
        return psps;
    }

    /// <summary>
    /// Example: Get PSPs for specific transaction requirements
    /// </summary>
    public static async Task<List<PspSnapshot>> GetPspsForTransactionExample(
        string connectionString, 
        long currencyId, 
        long paymentMethodId)
    {
        var client = PspRouterClientFactory.Create(connectionString);
        
        var psps = await client.GetPspsForTransactionAsync(currencyId, paymentMethodId);
        
        Console.WriteLine($"PSPs for CurrencyId={currencyId}, PaymentMethodId={paymentMethodId}:");
        foreach (var psp in psps)
        {
            Console.WriteLine($"- {psp.Name}: {psp.Health} health, {psp.AuthRate30d:P2} auth rate");
        }
        
        return psps;
    }

    /// <summary>
    /// Example: Make deterministic routing decision
    /// </summary>
    public static async Task<RouteDecision> MakeDeterministicDecisionExample(string connectionString)
    {
        var client = PspRouterClientFactory.Create(connectionString);
        
        // Create a sample transaction
        var transaction = new RouteInput(
            MerchantId: "M123",
            BuyerCountry: "US",
            MerchantCountry: "US",
            CurrencyId: 1, // USD
            Amount: 150.00m,
            PaymentMethodId: 1, // Card
            PaymentCardBin: "411111",
            SCARequired: false,
            RiskScore: 25
        );
        
        // Make deterministic routing decision
        var decision = await client.MakeDeterministicDecisionAsync(transaction);
        
        Console.WriteLine($"Routing decision: {decision.Candidate}");
        Console.WriteLine($"Reasoning: {decision.Reasoning}");
        Console.WriteLine($"Alternates: {string.Join(", ", decision.Alternates)}");
        
        return decision;
    }

    /// <summary>
    /// Example: Make routing decision with fine-tuned model
    /// </summary>
    public static async Task<RouteDecision> MakeModelDecisionExample(
        string connectionString, 
        string modelApiUrl, 
        string apiKey)
    {
        var client = PspRouterClientFactory.Create(connectionString);
        
        // Create a sample transaction
        var transaction = new RouteInput(
            MerchantId: "M456",
            BuyerCountry: "GB",
            MerchantCountry: "GB",
            CurrencyId: 2, // GBP
            Amount: 75.50m,
            PaymentMethodId: 1, // Card
            PaymentCardBin: "555555",
            SCARequired: true,
            RiskScore: 85
        );
        
        // Make routing decision using fine-tuned model
        var decision = await client.MakeRoutingDecisionAsync(transaction, modelApiUrl, apiKey);
        
        Console.WriteLine($"Model routing decision: {decision.Candidate}");
        Console.WriteLine($"Reasoning: {decision.Reasoning}");
        Console.WriteLine($"Guardrail: {decision.Guardrail}");
        Console.WriteLine($"Features used: {string.Join(", ", decision.Features_Used)}");
        
        return decision;
    }

    /// <summary>
    /// Example: Get detailed PSP performance metrics
    /// </summary>
    public static async Task<PspPerformanceMetrics?> GetPspMetricsExample(
        string connectionString, 
        string pspName)
    {
        var client = PspRouterClientFactory.Create(connectionString);
        
        var metrics = await client.GetPspDetailsAsync(pspName);
        
        if (metrics != null)
        {
            Console.WriteLine($"PSP: {metrics.PspName}");
            Console.WriteLine($"30-day auth rate: {metrics.AuthRate30d:F2}%");
            Console.WriteLine($"7-day auth rate: {metrics.AuthRate7d:F2}%");
            Console.WriteLine($"Total transactions (30d): {metrics.TotalTransactions30d}");
            Console.WriteLine($"Successful transactions (30d): {metrics.SuccessfulTransactions30d}");
            Console.WriteLine($"Average fee (bps): {metrics.AverageFeeBps:F2}");
            Console.WriteLine($"Average fixed fee: {metrics.AverageFixedFee:C}");
            Console.WriteLine($"Health status: {metrics.HealthStatus}");
            Console.WriteLine($"Last updated: {metrics.LastUpdated}");
        }
        else
        {
            Console.WriteLine($"No metrics found for PSP: {pspName}");
        }
        
        return metrics;
    }

    /// <summary>
    /// Example: Complete routing workflow
    /// </summary>
    public static async Task<RouteDecision> CompleteRoutingWorkflowExample(
        string connectionString,
        string? modelApiUrl = null,
        string? apiKey = null)
    {
        var client = PspRouterClientFactory.Create(connectionString);
        
        // Create a high-risk transaction requiring 3DS
        var transaction = new RouteInput(
            MerchantId: "M789",
            BuyerCountry: "DE",
            MerchantCountry: "DE",
            CurrencyId: 3, // EUR
            Amount: 500.00m,
            PaymentMethodId: 1, // Card
            PaymentCardBin: "400000",
            SCARequired: true,
            RiskScore: 90
        );
        
        Console.WriteLine($"Processing transaction: {transaction.MerchantId}, Amount: {transaction.Amount} EUR");
        Console.WriteLine($"Risk Score: {transaction.RiskScore}, SCA Required: {transaction.SCARequired}");
        
        // Get 3DS-capable PSPs
        var threeDSPsps = await client.Get3DSCapablePspsAsync();
        Console.WriteLine($"Found {threeDSPsps.Count} PSPs with 3DS support");
        
        // Build route context
        var context = await client.BuildRouteContextAsync(transaction);
        Console.WriteLine($"Route context built with {context.Candidates.Count} candidates");
        
        // Make routing decision
        RouteDecision decision;
        if (!string.IsNullOrEmpty(modelApiUrl) && !string.IsNullOrEmpty(apiKey))
        {
            Console.WriteLine("Using fine-tuned model for routing decision...");
            decision = await client.MakeRoutingDecisionAsync(transaction, modelApiUrl, apiKey);
        }
        else
        {
            Console.WriteLine("Using deterministic scoring for routing decision...");
            decision = await client.MakeDeterministicDecisionAsync(transaction);
        }
        
        Console.WriteLine($"\n=== ROUTING DECISION ===");
        Console.WriteLine($"Selected PSP: {decision.Candidate}");
        Console.WriteLine($"Reasoning: {decision.Reasoning}");
        Console.WriteLine($"Guardrail: {decision.Guardrail}");
        Console.WriteLine($"Must use 3DS: {decision.Constraints.Must_Use_3ds}");
        Console.WriteLine($"Retry window: {decision.Constraints.Retry_Window_Ms}ms");
        Console.WriteLine($"Max retries: {decision.Constraints.Max_Retries}");
        Console.WriteLine($"Alternates: {string.Join(", ", decision.Alternates)}");
        Console.WriteLine($"Features used: {string.Join(", ", decision.Features_Used)}");
        
        return decision;
    }

    /// <summary>
    /// Example: Batch processing multiple transactions
    /// </summary>
    public static async Task<List<RouteDecision>> BatchProcessingExample(string connectionString)
    {
        var client = PspRouterClientFactory.Create(connectionString);
        
        var transactions = new[]
        {
            new RouteInput("M001", "US", "US", 1, 25.00m, 1, "411111", SCARequired: false, RiskScore: 10),
            new RouteInput("M002", "GB", "GB", 2, 100.00m, 1, "555555", SCARequired: true, RiskScore: 60),
            new RouteInput("M003", "DE", "DE", 3, 250.00m, 1, "400000", SCARequired: true, RiskScore: 85),
            new RouteInput("M004", "FR", "FR", 3, 50.00m, 2, null, SCARequired: false, RiskScore: 20), // PayPal
        };
        
        var decisions = new List<RouteDecision>();
        
        Console.WriteLine("Processing batch of transactions...");
        
        foreach (var transaction in transactions)
        {
            try
            {
                var decision = await client.MakeDeterministicDecisionAsync(transaction);
                decisions.Add(decision);
                
                Console.WriteLine($"Transaction {transaction.MerchantId}: {decision.Candidate} - {decision.Reasoning}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Transaction {transaction.MerchantId}: ERROR - {ex.Message}");
            }
        }
        
        Console.WriteLine($"\nBatch processing complete: {decisions.Count}/{transactions.Length} successful");
        
        return decisions;
    }
}

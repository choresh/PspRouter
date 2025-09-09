namespace PspRouter.Lib;

public record RouteInput(
    string MerchantId,
    string BuyerCountry,
    string MerchantCountry,
    long CurrencyId,
    decimal Amount,
    long PaymentMethodId,
    string? PaymentCardBin = null,
    long? ThreeDSTypeId = null,
    bool IsTokenized = false,
    bool SCARequired = false,
    int RiskScore = 0
);

public record PspSnapshot(
    string Name,
    bool Supports,
    string Health,
    double AuthRate30d,
    int FeeBps,
    decimal FixedFee,
    bool Supports3DS,
    bool Tokenization
);

public record RouteContext(
    RouteInput Tx,
    IReadOnlyList<PspSnapshot> Candidates
);

public record RouteConstraints(bool Must_Use_3ds, int Retry_Window_Ms, int Max_Retries);

public record RouteDecision(
    string Schema_Version,
    string Decision_Id,
    string Candidate,
    IReadOnlyList<string> Alternates,
    string Reasoning,
    string Guardrail,
    RouteConstraints Constraints,
    IReadOnlyList<string> Features_Used
);

public sealed class RoutingSettings
{
    public ScoringWeights Weights { get; init; } = new();
    public string[] AllowedHealthStatuses { get; init; } = new[] { "green", "yellow" };

    public static RoutingSettings Default => new();
}

public sealed class ScoringWeights
{
    public double AuthWeight { get; init; } = 1.0;
    public double FeeBpsWeight { get; init; } = 1.0;
    public double FixedFeeWeight { get; init; } = 1.0;
    public double Supports3DSBonusWhenSCARequired { get; init; } = 0.0;
    public double RiskScorePenaltyPerPoint { get; init; } = 0.0; // 0..1 per risk point (0-100)
    public double HealthYellowPenalty { get; init; } = 0.0;
    public double BusinessBiasWeight { get; init; } = 0.0;
    public Dictionary<string, double> BusinessBias { get; init; } = new();

}

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
/// Feedback from client about transaction execution results
/// </summary>
public record TransactionFeedback(
    string DecisionId,
    string MerchantId,
    string PspName,
    bool Authorized,
    decimal TransactionAmount,
    decimal FeeAmount,
    int ProcessingTimeMs,
    int RiskScore,
    DateTime ProcessedAt,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    string? RoutingMethod = null // "ml", "llm", "deterministic"
);

/// <summary>
/// Simplified routing request without candidates (candidates provided internally)
/// </summary>
public record SimpleRouteRequest(
    RouteInput Transaction
);

/// <summary>
/// PSP candidate with performance metrics for internal storage
/// </summary>
public record PspCandidate(
    string Name,
    bool Supports,
    string Health,
    double AuthRate30d,
    int FeeBps,
    decimal FixedFee,
    bool Supports3DS,
    bool Tokenization,
    // Performance tracking
    int TotalTransactions = 0,
    int SuccessfulTransactions = 0,
    double AverageProcessingTimeMs = 0.0,
    DateTime LastUpdated = default,
    Dictionary<string, double> PerformanceByCountry = default!,
    Dictionary<string, double> PerformanceByPaymentMethod = default!
)
{
    public double CurrentAuthRate => TotalTransactions > 0 ? (double)SuccessfulTransactions / TotalTransactions : AuthRate30d;
    public double AverageProcessingTime => AverageProcessingTimeMs;
    
    public PspCandidate WithPerformanceUpdate(bool authorized, int processingTimeMs, string country, long paymentMethodId)
    {
        var newTotal = TotalTransactions + 1;
        var newSuccessful = SuccessfulTransactions + (authorized ? 1 : 0);
        var newAvgProcessingTime = (AverageProcessingTimeMs * TotalTransactions + processingTimeMs) / newTotal;
        
        var newPerformanceByCountry = new Dictionary<string, double>(PerformanceByCountry ?? new Dictionary<string, double>());
        var newPerformanceByPaymentMethod = new Dictionary<string, double>(PerformanceByPaymentMethod ?? new Dictionary<string, double>());
        
        // Update country performance
        if (newPerformanceByCountry.ContainsKey(country))
        {
            newPerformanceByCountry[country] = (newPerformanceByCountry[country] * (newTotal - 1) + (authorized ? 1.0 : 0.0)) / newTotal;
        }
        else
        {
            newPerformanceByCountry[country] = authorized ? 1.0 : 0.0;
        }
        
        // Update payment method performance
        var paymentMethodKey = paymentMethodId.ToString();
        if (newPerformanceByPaymentMethod.ContainsKey(paymentMethodKey))
        {
            newPerformanceByPaymentMethod[paymentMethodKey] = (newPerformanceByPaymentMethod[paymentMethodKey] * (newTotal - 1) + (authorized ? 1.0 : 0.0)) / newTotal;
        }
        else
        {
            newPerformanceByPaymentMethod[paymentMethodKey] = authorized ? 1.0 : 0.0;
        }
        
        return this with
        {
            TotalTransactions = newTotal,
            SuccessfulTransactions = newSuccessful,
            AverageProcessingTimeMs = newAvgProcessingTime,
            LastUpdated = DateTime.UtcNow,
            PerformanceByCountry = newPerformanceByCountry,
            PerformanceByPaymentMethod = newPerformanceByPaymentMethod
        };
    }
}


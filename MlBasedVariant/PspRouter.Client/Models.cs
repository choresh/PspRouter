namespace PspRouter.Client;

/// <summary>
/// Transaction input for PSP routing
/// </summary>
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

/// <summary>
/// PSP snapshot with current performance metrics
/// </summary>
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

/// <summary>
/// Route context containing transaction and available PSPs
/// </summary>
public record RouteContext(
    RouteInput Transaction,
    IReadOnlyList<PspSnapshot> Candidates
);

/// <summary>
/// Route constraints for the decision
/// </summary>
public record RouteConstraints(
    bool Must_Use_3ds, 
    int Retry_Window_Ms, 
    int Max_Retries
);

/// <summary>
/// Final routing decision
/// </summary>
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

/// <summary>
/// Detailed performance metrics for a PSP
/// </summary>
public record PspPerformanceMetrics(
    string PspName,
    double AuthRate30d,
    double AuthRate7d,
    int TotalTransactions30d,
    int SuccessfulTransactions30d,
    decimal AverageFeeBps,
    decimal AverageFixedFee,
    string HealthStatus,
    DateTime LastUpdated
);

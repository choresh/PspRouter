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


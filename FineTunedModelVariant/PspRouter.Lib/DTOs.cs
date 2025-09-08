namespace PspRouter.Lib;

public enum PaymentMethod { Card, PayPal, KlarnaPayLater }
public enum CardScheme { Visa, Mastercard, Amex, Unknown }

public record RouteInput(
    string MerchantId,
    string BuyerCountry,
    string MerchantCountry,
    string Currency,
    decimal Amount,
    PaymentMethod Method,
    CardScheme Scheme = CardScheme.Unknown,
    bool SCARequired = false,
    int RiskScore = 0,
    string? Bin = null
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
    IReadOnlyList<PspSnapshot> Candidates,
    IReadOnlyDictionary<string,string> MerchantPrefs,
    IReadOnlyDictionary<string,double> SegmentStats
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

public record TrainingData(
    long PaymentTransactionId,
    string OrderId,
    decimal Amount,
    long PaymentGatewayId,
    long PaymentMethodId,
    long CurrencyId,
    long? CountryId,
    string? PaymentCardBin,
    long? ThreeDSTypeId,
    bool IsTokenized,
    long PaymentTransactionStatusId,
    string? GatewayStatusReason,
    string? GatewayResponseCode,
    bool IsReroutedFlag,
    long? PaymentRoutingRuleId,
    DateTime DateCreated,
    DateTime? DateStatusLastUpdated,
    string? PspReference
);


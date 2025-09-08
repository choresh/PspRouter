namespace PspRouter.Trainer;

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

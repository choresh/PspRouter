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
)
{
    /// <summary>
    /// Determines if the transaction was successful based on status ID
    /// </summary>
    public bool IsSuccessful => PaymentTransactionStatusId == 5 || // Authorized
                               PaymentTransactionStatusId == 7 || // Captured  
                               PaymentTransactionStatusId == 9;   // Settled

    /// <summary>
    /// Gets a human-readable status name
    /// </summary>
    public string StatusName => PaymentTransactionStatusId switch
    {
        5 => "Authorized",
        7 => "Captured", 
        9 => "Settled",
        11 => "Refused",
        15 => "ChargeBack",
        17 => "Error",
        22 => "AuthorizationFailed",
        _ => $"Status_{PaymentTransactionStatusId}"
    };

    /// <summary>
    /// Calculates a simple risk score based on transaction characteristics
    /// </summary>
    public float CalculateRiskScore()
    {
        var riskScore = 0f;
        
        // Higher risk for larger amounts
        if (Amount > 1000) riskScore += 20f;
        else if (Amount > 500) riskScore += 10f;
        
        // Higher risk for certain payment methods (assuming card = 1)
        if (PaymentMethodId == 1) riskScore += 5f;
        
        // Higher risk for tokenized transactions
        if (IsTokenized) riskScore += 15f;
        
        // Higher risk for rerouted transactions
        if (IsReroutedFlag) riskScore += 25f;
        
        return Math.Min(riskScore, 100f);
    }
}

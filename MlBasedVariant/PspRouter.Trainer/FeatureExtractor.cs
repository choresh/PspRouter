using Microsoft.Extensions.Logging;

namespace PspRouter.Trainer;

/// <summary>
/// Extracts ML features from training data
/// </summary>
public class FeatureExtractor
{
    private readonly ILogger<FeatureExtractor> _logger;
    private readonly Dictionary<long, PspMetadata> _pspMetadata;

    public FeatureExtractor(ILogger<FeatureExtractor> logger)
    {
        _logger = logger;
        _pspMetadata = new Dictionary<long, PspMetadata>();
        InitializePspMetadata();
    }

    /// <summary>
    /// Extract features from training data
    /// </summary>
    public IEnumerable<RoutingTrainingExample> ExtractFeatures(IEnumerable<TrainingData> trainingData)
    {
        _logger.LogInformation("Extracting features from {Count} training records", trainingData.Count());
        
        var examples = new List<RoutingTrainingExample>();
        
        foreach (var data in trainingData)
        {
            try
            {
                var example = ExtractFeaturesFromTransaction(data);
                if (example != null)
                {
                    examples.Add(example);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to extract features from transaction {TransactionId}", data.PaymentTransactionId);
            }
        }
        
        _logger.LogInformation("Successfully extracted {Count} feature examples", examples.Count);
        return examples;
    }

    private RoutingTrainingExample? ExtractFeaturesFromTransaction(TrainingData data)
    {
        // Get PSP metadata
        if (!_pspMetadata.TryGetValue(data.PaymentGatewayId, out var pspMeta))
        {
            _logger.LogWarning("No metadata found for PSP {PspId}", data.PaymentGatewayId);
            return null;
        }

        // Determine success based on status
        var isSuccessful = data.PaymentTransactionStatusId == 5 || // Authorized
                          data.PaymentTransactionStatusId == 7 || // Captured  
                          data.PaymentTransactionStatusId == 9;   // Settled

        // Calculate derived features
        var amount = (float)data.Amount;
        var feeRatio = CalculateFeeRatio(amount, pspMeta.FeeBps, pspMeta.FixedFee);
        var riskAdjustedAuthRate = pspMeta.AuthRate * (1 - (data.PaymentTransactionStatusId == 11 ? 50f : 0f) / 100f); // Simple risk proxy
        var complianceScore = (data.ThreeDSTypeId.HasValue && pspMeta.Supports3DS) ? 1f : 0f;
        var amountLog = amount > 0 ? (float)Math.Log(amount) : 0f;

        // Extract time features
        var hourOfDay = data.DateCreated.Hour;
        var dayOfWeek = (int)data.DateCreated.DayOfWeek;

        var features = new RoutingFeatures
        {
            // Transaction features
            Amount = amount,
            PaymentMethodId = data.PaymentMethodId,
            CurrencyId = data.CurrencyId,
            CountryId = data.CountryId ?? 0,
            RiskScore = CalculateRiskScore(data),
            IsTokenized = data.IsTokenized ? 1f : 0f,
            HasThreeDS = data.ThreeDSTypeId.HasValue ? 1f : 0f,
            IsRerouted = data.IsReroutedFlag ? 1f : 0f,
            
            // PSP features
            PspAuthRate = pspMeta.AuthRate,
            PspFeeBps = pspMeta.FeeBps,
            PspFixedFee = (float)pspMeta.FixedFee,
            PspSupports3DS = pspMeta.Supports3DS ? 1f : 0f,
            PspHealth = GetHealthScore(pspMeta.Health),
            PspId = data.PaymentGatewayId,
            
            // Derived features
            FeeRatio = feeRatio,
            RiskAdjustedAuthRate = riskAdjustedAuthRate,
            ComplianceScore = complianceScore,
            AmountLog = amountLog,
            HourOfDay = hourOfDay,
            DayOfWeek = dayOfWeek
        };

        return new RoutingTrainingExample
        {
            Features = features,
            IsSuccessful = isSuccessful,
            SuccessScore = isSuccessful ? 1f : 0f,
            CostScore = CalculateCostScore(amount, pspMeta.FeeBps, pspMeta.FixedFee),
            AuthRateScore = isSuccessful ? pspMeta.AuthRate : 0f,
            TargetPspId = data.PaymentGatewayId,
            TransactionId = data.PaymentTransactionId,
            DateCreated = data.DateCreated
        };
    }

    private float CalculateFeeRatio(float amount, int feeBps, decimal fixedFee)
    {
        if (amount <= 0) return 0f;
        var totalFee = (feeBps * amount / 10000f) + (float)fixedFee;
        return totalFee / amount;
    }

    private float CalculateRiskScore(TrainingData data)
    {
        // Simple risk scoring based on transaction characteristics
        var riskScore = 0f;
        
        // Higher risk for larger amounts
        if (data.Amount > 1000) riskScore += 20f;
        else if (data.Amount > 500) riskScore += 10f;
        
        // Higher risk for certain payment methods (assuming card = 1)
        if (data.PaymentMethodId == 1) riskScore += 5f;
        
        // Higher risk for tokenized transactions (potential fraud)
        if (data.IsTokenized) riskScore += 15f;
        
        // Higher risk for rerouted transactions
        if (data.IsReroutedFlag) riskScore += 25f;
        
        return Math.Min(riskScore, 100f);
    }

    private float CalculateCostScore(float amount, int feeBps, decimal fixedFee)
    {
        if (amount <= 0) return 1f; // High cost for zero amount
        var totalFee = (feeBps * amount / 10000f) + (float)fixedFee;
        var feePercentage = totalFee / amount;
        
        // Normalize to 0-1 scale (lower is better)
        // Assuming typical fee range is 0-5%
        return Math.Min(feePercentage / 0.05f, 1f);
    }

    private float GetHealthScore(string health)
    {
        return health?.ToLower() switch
        {
            "green" => 2f,
            "yellow" => 1f,
            "red" => 0f,
            _ => 1f // Default to yellow
        };
    }

    private void InitializePspMetadata()
    {
        // Initialize with sample PSP metadata
        // In production, this would come from a database or configuration
        _pspMetadata[1] = new PspMetadata("Stripe", 0.95f, 290, 0.30m, true, "green");
        _pspMetadata[2] = new PspMetadata("PayPal", 0.92f, 340, 0.35m, true, "green");
        _pspMetadata[3] = new PspMetadata("Adyen", 0.94f, 320, 0.25m, true, "green");
        _pspMetadata[4] = new PspMetadata("Square", 0.89f, 280, 0.30m, false, "yellow");
        _pspMetadata[5] = new PspMetadata("Braintree", 0.91f, 295, 0.00m, true, "green");
        
        _logger.LogInformation("Initialized PSP metadata for {Count} PSPs", _pspMetadata.Count);
    }
}

/// <summary>
/// PSP metadata for feature extraction
/// </summary>
public record PspMetadata(
    string Name,
    float AuthRate,
    int FeeBps,
    decimal FixedFee,
    bool Supports3DS,
    string Health
);

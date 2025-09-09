using Microsoft.Extensions.Logging;
using Microsoft.ML;

namespace PspRouter.Lib;

/// <summary>
/// ML-based prediction service for PSP routing
/// </summary>
public class PredictionService : IPredictionService
{
    private readonly ILogger<PredictionService> _logger;
    private readonly MLContext _mlContext;
    private readonly string _modelPath;
    private ITransformer? _model;

    public bool IsModelLoaded => _model != null;

    public PredictionService(ILogger<PredictionService> logger, string modelPath)
    {
        _logger = logger;
        _mlContext = new MLContext(seed: 42);
        _modelPath = modelPath;
    }

    /// <summary>
    /// Load the trained ML model
    /// </summary>
    public async Task LoadModelAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            if (!File.Exists(_modelPath))
            {
                _logger.LogError("ML model file not found at path: {ModelPath}", _modelPath);
                return;
            }

            _model = _mlContext.Model.Load(_modelPath, out var modelSchema);
            _logger.LogInformation("ML model loaded successfully from {ModelPath}", _modelPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load ML model from {ModelPath}", _modelPath);
            _model = null;
        }
    }

    /// <summary>
    /// Predict the best PSP for the given routing context
    /// </summary>
    public async Task<MLRoutingPrediction?> PredictBestPspAsync(RouteContext context, CancellationToken cancellationToken = default)
    {
        if (_model == null)
        {
            _logger.LogWarning("ML model not loaded, cannot make predictions");
            return null;
        }

        try
        {
            var predictions = new List<PSPPrediction>();

            // For each valid PSP candidate, predict success probability
            foreach (var candidate in context.Candidates)
            {
                var features = ExtractFeatures(context.Tx, candidate);
                var prediction = PredictForPsp(features);
                
                predictions.Add(new PSPPrediction
                {
                    PspName = candidate.Name,
                    SuccessProbability = prediction.Probability,
                    Score = prediction.Score,
                    IsSuccessful = prediction.IsSuccessful
                });
            }

            // Find the PSP with highest success probability
            var bestPsp = predictions.OrderByDescending(p => p.SuccessProbability).FirstOrDefault();
            
            if (bestPsp == null)
            {
                _logger.LogWarning("No valid PSP predictions generated");
                return null;
            }

            return new MLRoutingPrediction
            {
                RecommendedPsp = bestPsp.PspName,
                SuccessProbability = bestPsp.SuccessProbability,
                AllPredictions = predictions,
                ModelVersion = "1.0",
                PredictionTimestamp = DateTime.UtcNow
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error making ML prediction for transaction {MerchantId}", context.Tx.MerchantId);
            return null;
        }
    }

    private RoutingFeatures ExtractFeatures(RouteInput tx, PspSnapshot psp)
    {
        // Calculate derived features
        var amount = (float)tx.Amount;
        var feeRatio = amount > 0 ? ((float)psp.FeeBps / 10000f * amount + (float)psp.FixedFee) / amount : 0f;
        var riskAdjustedAuthRate = (float)(psp.AuthRate30d * (1 - tx.RiskScore / 100f));
        var complianceScore = (psp.Supports3DS && tx.SCARequired) ? 1f : 0f;
        var amountLog = (float)Math.Log10(Math.Max(1, (double)tx.Amount));
        var hourOfDay = DateTime.UtcNow.Hour;
        var dayOfWeek = (float)DateTime.UtcNow.DayOfWeek;

        return new RoutingFeatures
        {
            // Transaction features
            Amount = amount,
            PaymentMethodId = tx.PaymentMethodId,
            CurrencyId = tx.CurrencyId,
            CountryId = GetCountryId(tx.BuyerCountry), // Convert country to ID
            RiskScore = tx.RiskScore,
            IsTokenized = tx.IsTokenized ? 1f : 0f,
            HasThreeDS = tx.SCARequired ? 1f : 0f,
            IsRerouted = 0f, // Not available in current context
            
            // PSP features
            PspAuthRate = (float)psp.AuthRate30d,
            PspFeeBps = psp.FeeBps,
            PspFixedFee = (float)psp.FixedFee,
            PspSupports3DS = psp.Supports3DS ? 1f : 0f,
            PspHealth = GetHealthScore(psp.Health),
            PspId = GetPspId(psp.Name), // Convert PSP name to ID
            
            // Derived features
            FeeRatio = feeRatio,
            RiskAdjustedAuthRate = riskAdjustedAuthRate,
            ComplianceScore = complianceScore,
            AmountLog = amountLog,
            HourOfDay = hourOfDay,
            DayOfWeek = dayOfWeek,
            
            // Labels (not used for prediction, but required by the model)
            IsSuccessful = false,
            SuccessScore = 0f,
            CostScore = 0f,
            AuthRateScore = 0f,
            TargetPspId = 0,
            TransactionId = 0,
            DateCreated = DateTime.UtcNow
        };
    }

    private RoutingPrediction PredictForPsp(RoutingFeatures features)
    {
        var predictionEngine = _mlContext.Model.CreatePredictionEngine<RoutingFeatures, RoutingPrediction>(_model!);
        return predictionEngine.Predict(features);
    }

    private float GetHealthScore(string health)
    {
        return health.ToLowerInvariant() switch
        {
            "green" => 2f,
            "yellow" => 1f,
            "red" => 0f,
            _ => 0f
        };
    }

    private long GetCountryId(string countryCode)
    {
        // Simple mapping - in production, this would come from a lookup service
        return countryCode.ToUpperInvariant() switch
        {
            "US" => 1,
            "GB" => 2,
            "DE" => 3,
            "FR" => 4,
            "IT" => 5,
            "ES" => 6,
            "NL" => 7,
            "BE" => 8,
            "AT" => 9,
            "CH" => 10,
            _ => 0 // Unknown country
        };
    }

    private long GetPspId(string pspName)
    {
        // Simple mapping - in production, this would come from a lookup service
        return pspName.ToUpperInvariant() switch
        {
            "PSP_A" => 101,
            "PSP_B" => 102,
            "PSP_C" => 103,
            "PSP_D" => 104,
            "PSP_E" => 105,
            _ => 0 // Unknown PSP
        };
    }
}

/// <summary>
/// ML model prediction result for a single PSP
/// </summary>
public class PSPPrediction
{
    public string PspName { get; set; } = string.Empty;
    public float SuccessProbability { get; set; }
    public float Score { get; set; }
    public bool IsSuccessful { get; set; }
}

/// <summary>
/// Complete ML routing prediction result
/// </summary>
public class MLRoutingPrediction
{
    public string RecommendedPsp { get; set; } = string.Empty;
    public float SuccessProbability { get; set; }
    public List<PSPPrediction> AllPredictions { get; set; } = new();
    public string ModelVersion { get; set; } = string.Empty;
    public DateTime PredictionTimestamp { get; set; }
}

/// <summary>
/// ML model input features (shared with trainer)
/// </summary>
public class RoutingFeatures
{
    // Transaction features
    public float Amount { get; set; }
    public float PaymentMethodId { get; set; }
    public float CurrencyId { get; set; }
    public float CountryId { get; set; }
    public float RiskScore { get; set; }
    public float IsTokenized { get; set; }
    public float HasThreeDS { get; set; }
    public float IsRerouted { get; set; }
    
    // PSP features
    public float PspAuthRate { get; set; }
    public float PspFeeBps { get; set; }
    public float PspFixedFee { get; set; }
    public float PspSupports3DS { get; set; }
    public float PspHealth { get; set; }
    public float PspId { get; set; }
    
    // Derived features
    public float FeeRatio { get; set; }
    public float RiskAdjustedAuthRate { get; set; }
    public float ComplianceScore { get; set; }
    public float AmountLog { get; set; }
    public float HourOfDay { get; set; }
    public float DayOfWeek { get; set; }
    
    // Labels (not used for prediction, but required by the model)
    public bool IsSuccessful { get; set; }
    public float SuccessScore { get; set; }
    public float CostScore { get; set; }
    public float AuthRateScore { get; set; }
    public long TargetPspId { get; set; }
    public long TransactionId { get; set; }
    public DateTime DateCreated { get; set; }
}

/// <summary>
/// ML model output prediction
/// </summary>
public class RoutingPrediction
{
    public bool IsSuccessful { get; set; }
    public float Score { get; set; }
    public float Probability { get; set; }
}

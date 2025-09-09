using Microsoft.ML.Data;

namespace PspRouter.Trainer;

/// <summary>
/// Input features for ML model training
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
    
    // PSP features (these would be enriched from PSP metadata)
    public float PspAuthRate { get; set; }
    public float PspFeeBps { get; set; }
    public float PspFixedFee { get; set; }
    public float PspSupports3DS { get; set; }
    public float PspHealth { get; set; } // 0=red, 1=yellow, 2=green
    public float PspId { get; set; }
    
    // Derived features
    public float FeeRatio { get; set; } // (fee_bps * amount + fixed_fee) / amount
    public float RiskAdjustedAuthRate { get; set; } // auth_rate * (1 - risk_score/100)
    public float ComplianceScore { get; set; } // supports_3ds && has_3ds ? 1 : 0
    public float AmountLog { get; set; } // log(amount) for better distribution
    public float HourOfDay { get; set; } // extracted from DateCreated
    public float DayOfWeek { get; set; } // extracted from DateCreated
    
    // Labels for training
    public bool IsSuccessful { get; set; } // Binary: success/failure
    public float SuccessScore { get; set; } // Continuous: 0-1 success probability
    public float CostScore { get; set; } // Continuous: normalized cost (lower is better)
    public float AuthRateScore { get; set; } // Continuous: actual auth rate achieved
    
    // Target PSP (for multi-class classification)
    public long TargetPspId { get; set; }
    
    // Transaction metadata
    public long TransactionId { get; set; }
    public DateTime DateCreated { get; set; }
}

/// <summary>
/// Output prediction for ML model
/// </summary>
public class RoutingPrediction
{
    [ColumnName("Score")]
    public float Score { get; set; }
    
    [ColumnName("Probability")]
    public float Probability { get; set; }
    
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }
}


/// <summary>
/// Model training configuration
/// </summary>
public class ModelTrainingConfig
{
    public int MaxIterations { get; set; } = 1000;
    public double LearningRate { get; set; } = 0.1;
    public int NumLeaves { get; set; } = 31;
    public int MinDataInLeaf { get; set; } = 20;
    public double FeatureFraction { get; set; } = 0.8;
    public double BaggingFraction { get; set; } = 0.8;
    public int BaggingFreq { get; set; } = 5;
    public double LambdaL1 { get; set; } = 0.0;
    public double LambdaL2 { get; set; } = 0.0;
    public int Verbosity { get; set; } = 1;
    public int Seed { get; set; } = 42;
    
    // Training objective
    public string Objective { get; set; } = "binary"; // binary, multiclass, regression
    public string Metric { get; set; } = "binary_logloss"; // binary_logloss, multiclass, rmse
    
    // Validation
    public double ValidationFraction { get; set; } = 0.2;
    public int EarlyStoppingRounds { get; set; } = 50;
}

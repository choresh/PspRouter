using Microsoft.ML.Data;

namespace PspRouter.Trainer;

/// <summary>
/// Input features for ML model training
/// </summary>
public class RoutingFeatures
{
    // Transaction features
    [LoadColumn(0)]
    public float Amount { get; set; }
    
    [LoadColumn(1)]
    public float PaymentMethodId { get; set; }
    
    [LoadColumn(2)]
    public float CurrencyId { get; set; }
    
    [LoadColumn(3)]
    public float CountryId { get; set; }
    
    [LoadColumn(4)]
    public float RiskScore { get; set; }
    
    [LoadColumn(5)]
    public float IsTokenized { get; set; }
    
    [LoadColumn(6)]
    public float HasThreeDS { get; set; }
    
    [LoadColumn(7)]
    public float IsRerouted { get; set; }
    
    // PSP features (these would be enriched from PSP metadata)
    [LoadColumn(8)]
    public float PspAuthRate { get; set; }
    
    [LoadColumn(9)]
    public float PspFeeBps { get; set; }
    
    [LoadColumn(10)]
    public float PspFixedFee { get; set; }
    
    [LoadColumn(11)]
    public float PspSupports3DS { get; set; }
    
    [LoadColumn(12)]
    public float PspHealth { get; set; } // 0=red, 1=yellow, 2=green
    
    [LoadColumn(13)]
    public float PspId { get; set; }
    
    // Derived features
    [LoadColumn(14)]
    public float FeeRatio { get; set; } // (fee_bps * amount + fixed_fee) / amount
    
    [LoadColumn(15)]
    public float RiskAdjustedAuthRate { get; set; } // auth_rate * (1 - risk_score/100)
    
    [LoadColumn(16)]
    public float ComplianceScore { get; set; } // supports_3ds && has_3ds ? 1 : 0
    
    [LoadColumn(17)]
    public float AmountLog { get; set; } // log(amount) for better distribution
    
    [LoadColumn(18)]
    public float HourOfDay { get; set; } // extracted from DateCreated
    
    [LoadColumn(19)]
    public float DayOfWeek { get; set; } // extracted from DateCreated
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
/// Training example with features and labels
/// </summary>
public class RoutingTrainingExample
{
    public RoutingFeatures Features { get; set; } = new();
    
    // Labels for different training objectives
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

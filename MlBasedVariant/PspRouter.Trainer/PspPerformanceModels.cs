using Microsoft.ML.Data;

namespace PspRouter.Trainer;

/// <summary>
/// Features for PSP performance prediction models
/// </summary>
public class PspPerformanceFeatures
{
    // Transaction context features
    public float Amount { get; set; }
    public float PaymentMethodId { get; set; }
    public float CurrencyId { get; set; }
    public float CountryId { get; set; }
    public float RiskScore { get; set; }
    public float IsTokenized { get; set; }
    public float HasThreeDS { get; set; }
    public float IsRerouted { get; set; }
    
    // PSP identification
    public float PspId { get; set; }
    public string PspName { get; set; } = string.Empty;
    
    // Time-based features
    public float HourOfDay { get; set; }
    public float DayOfWeek { get; set; }
    public float DayOfMonth { get; set; }
    public float MonthOfYear { get; set; }
    
    // Historical performance features
    public float RecentSuccessRate { get; set; } // Last 7 days
    public float RecentProcessingTime { get; set; } // Last 7 days average
    public float RecentVolume { get; set; } // Last 7 days transaction count
    
    // Derived features
    public float AmountLog { get; set; }
    public float RiskAdjustedAmount { get; set; } // amount * (1 - risk_score/100)
    public float TimeOfDayCategory { get; set; } // 0=night, 1=morning, 2=afternoon, 3=evening
    
    // Labels for different prediction tasks
    public bool IsSuccessful { get; set; } // For success rate prediction
    public float ProcessingTimeMs { get; set; } // For processing time prediction
    public float HealthScore { get; set; } // For health prediction (0=red, 1=yellow, 2=green)
    
    // Transaction metadata
    public long TransactionId { get; set; }
    public DateTime DateCreated { get; set; }
}

/// <summary>
/// Prediction output for PSP success rate
/// </summary>
public class PspSuccessRatePrediction
{
    [ColumnName("Score")]
    public float Score { get; set; }
    
    [ColumnName("Probability")]
    public float Probability { get; set; }
    
    [ColumnName("PredictedLabel")]
    public bool PredictedLabel { get; set; }
}

/// <summary>
/// Prediction output for PSP processing time
/// </summary>
public class PspProcessingTimePrediction
{
    [ColumnName("Score")]
    public float Score { get; set; }
}

/// <summary>
/// Prediction output for PSP health status
/// </summary>
public class PspHealthPrediction
{
    [ColumnName("Score")]
    public float Score { get; set; }
    
    [ColumnName("PredictedLabel")]
    public float PredictedLabel { get; set; } // 0=red, 1=yellow, 2=green
}

/// <summary>
/// Configuration for PSP performance model training
/// </summary>
public class PspPerformanceModelConfig
{
    // Success rate prediction model
    public int SuccessRateMaxIterations { get; set; }
    public double SuccessRateLearningRate { get; set; }
    public int SuccessRateNumLeaves { get; set; }
    
    // Processing time prediction model
    public int ProcessingTimeMaxIterations { get; set; }
    public double ProcessingTimeLearningRate { get; set; }
    public int ProcessingTimeNumLeaves { get; set; }
    
    // Health prediction model
    public int HealthMaxIterations { get; set; }
    public double HealthLearningRate { get; set; }
    public int HealthNumLeaves { get; set; }
    
    // Common settings
    public int MinDataInLeaf { get; set; }
    public double ValidationFraction { get; set; }
    public int EarlyStoppingRounds { get; set; }
    public int Seed { get; set; }
    
    // Model output paths
    public string SuccessRateModelPath { get; set; } = string.Empty;
    public string ProcessingTimeModelPath { get; set; } = string.Empty;
    public string HealthModelPath { get; set; } = string.Empty;
}

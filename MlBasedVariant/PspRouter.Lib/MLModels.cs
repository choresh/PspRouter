using Microsoft.ML.Data;

namespace PspRouter.Lib;

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


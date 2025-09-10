namespace PspRouter.Lib.Production;

/// <summary>
/// Production-grade ML model manager with online learning capabilities
/// </summary>
public interface IMLModelManager
{
    /// <summary>
    /// Predict success rate for a PSP given transaction context
    /// </summary>
    Task<double> PredictSuccessRateAsync(string pspName, RouteInput transaction, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Predict processing time for a PSP given transaction context
    /// </summary>
    Task<int> PredictProcessingTimeAsync(string pspName, RouteInput transaction, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Predict PSP health status
    /// </summary>
    Task<string> PredictHealthStatusAsync(string pspName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update model with real-time feedback (online learning)
    /// </summary>
    Task UpdateWithFeedbackAsync(TransactionFeedback feedback, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if model needs retraining
    /// </summary>
    Task<bool> ShouldRetrainAsync(string pspName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Perform incremental model retraining
    /// </summary>
    Task RetrainIncrementalAsync(string pspName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Get model performance metrics
    /// </summary>
    Task<ModelPerformanceMetrics> GetModelMetricsAsync(string pspName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Deploy new model version
    /// </summary>
    Task DeployModelAsync(string pspName, string modelVersion, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Rollback to previous model version
    /// </summary>
    Task RollbackModelAsync(string pspName, string modelVersion, CancellationToken cancellationToken = default);
}

/// <summary>
/// Model performance metrics for monitoring
/// </summary>
public class ModelPerformanceMetrics
{
    public string ModelVersion { get; set; } = string.Empty;
    public double Accuracy { get; set; }
    public double Precision { get; set; }
    public double Recall { get; set; }
    public double F1Score { get; set; }
    public double MAE { get; set; } // Mean Absolute Error
    public double RMSE { get; set; } // Root Mean Square Error
    public DateTime LastUpdated { get; set; }
    public int PredictionCount { get; set; }
    public int FeedbackCount { get; set; }
    public double DataDrift { get; set; } // Measure of how much data has drifted
}

/// <summary>
/// Model version information
/// </summary>
public class ModelVersion
{
    public string Version { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string TrainingDataHash { get; set; } = string.Empty;
    public ModelPerformanceMetrics Performance { get; set; } = new();
    public bool IsActive { get; set; }
    public string DeploymentStatus { get; set; } = string.Empty;
}

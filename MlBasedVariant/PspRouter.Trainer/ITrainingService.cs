namespace PspRouter.Trainer;

// Training service interface for ML-based training
public interface ITrainingService
{
    Task<string> TrainModelAsync(CancellationToken cancellationToken = default);
    Task<ModelMetrics> EvaluateModelAsync(string modelPath, CancellationToken cancellationToken = default);
    Task<bool> SaveModelAsync(string modelPath, CancellationToken cancellationToken = default);
    Task<bool> LoadModelAsync(string modelPath, CancellationToken cancellationToken = default);
}

// Model evaluation metrics
public record ModelMetrics(
    float Accuracy,
    float Precision,
    float Recall,
    float F1Score,
    float AUC,
    float LogLoss,
    Dictionary<string, float> FeatureImportance
);

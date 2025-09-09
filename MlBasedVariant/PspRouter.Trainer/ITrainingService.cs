namespace PspRouter.Trainer;

// Training service interface for ML-based training
public interface ITrainingService
{
    Task<string> TrainModel(CancellationToken cancellationToken = default);
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

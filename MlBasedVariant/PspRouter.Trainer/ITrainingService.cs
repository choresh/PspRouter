namespace PspRouter.Trainer;

// Training service interface for ML-based training
public interface ITrainingService
{
    Task TrainPspSelectionModel(CancellationToken cancellationToken = default);
    Task TrainPspPerformanceModels(CancellationToken cancellationToken = default);
}

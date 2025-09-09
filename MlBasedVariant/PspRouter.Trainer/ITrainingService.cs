namespace PspRouter.Trainer;

// Training service interface for ML-based training
public interface ITrainingService
{
    Task TrainModel(CancellationToken cancellationToken = default);
}

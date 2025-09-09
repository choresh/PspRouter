namespace PspRouter.Trainer;
public interface ITrainingDataProvider
{
    Task<IEnumerable<TrainingData>> GetTrainingData(CancellationToken cancellationToken = default);
}

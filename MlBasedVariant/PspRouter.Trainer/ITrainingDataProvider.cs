namespace PspRouter.Trainer;
public interface ITrainingDataProvider
{
    Task<IEnumerable<TrainingData>> GetTrainingDataAsync(CancellationToken cancellationToken = default);
}

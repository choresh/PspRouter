namespace PspRouter.Trainer;

// Training service interface
public interface ITrainingService
{
    Task<string> CreateFineTunedModel(CancellationToken cancellationToken = default);
}

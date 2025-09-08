namespace PspRouter.Trainer;

// Training service interface
public interface ITrainingService
{
    Task<string> CreateFineTunedModelAsync(CancellationToken cancellationToken = default);
    Task<string> UploadTrainingDataAsync(CancellationToken cancellationToken = default);
    Task<string> CreateFineTuningJobAsync(string fileId, CancellationToken cancellationToken = default);
    Task<string> GetFineTuningJobStatusAsync(string jobId, CancellationToken cancellationToken = default);
}

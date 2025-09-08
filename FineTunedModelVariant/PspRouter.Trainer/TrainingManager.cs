using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace PspRouter.Trainer;

// Training manager for orchestrating the fine-tuning process
public class TrainingManager : BackgroundService
{
    private readonly ITrainingService _trainingService;
    private readonly ILogger<TrainingManager> _logger;
    private readonly IConfiguration _configuration;

    public TrainingManager(ITrainingService trainingService, ILogger<TrainingManager> logger, IConfiguration configuration)
    {
        _trainingService = trainingService;
        _logger = logger;
        _configuration = configuration;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Training manager started");
        
        try
        {
            _logger.LogInformation("Starting fine-tuning process...");
            
            // Step 1: Upload training data from database
            var fileId = await _trainingService.UploadTrainingDataAsync(stoppingToken);
            _logger.LogInformation("Training data uploaded with ID: {FileId}", fileId);
            
            // Step 2: Create fine-tuning job
            var jobId = await _trainingService.CreateFineTuningJobAsync(fileId, stoppingToken);
            _logger.LogInformation("Fine-tuning job created with ID: {JobId}", jobId);
            
            // Step 3: Monitor job status
            string status;
            do
            {
                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken); // Check every minute
                status = await _trainingService.GetFineTuningJobStatusAsync(jobId, stoppingToken);
                _logger.LogInformation("Fine-tuning job {JobId} status: {Status}", jobId, status);
            } while (status != "completed" && status != "failed" && !stoppingToken.IsCancellationRequested);
            
            if (status == "completed")
            {
                _logger.LogInformation("Fine-tuning completed successfully!");
                // TODO: Update configuration with new model ID
            }
            else if (status == "failed")
            {
                _logger.LogError("Fine-tuning job failed!");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during training process");
            throw;
        }
    }
}

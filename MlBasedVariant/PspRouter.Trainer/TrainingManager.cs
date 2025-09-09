using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PspRouter.Trainer;

// Training manager for running the ML model training process
public class TrainingManager : BackgroundService
{
    private readonly ITrainingService _trainingService;
    private readonly ILogger<TrainingManager> _logger;
    private readonly TrainerSettings _settings;

    public TrainingManager(
        ITrainingService trainingService, 
        ILogger<TrainingManager> logger,
        TrainerSettings settings)
    {
        _trainingService = trainingService;
        _logger = logger;
        _settings = settings;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ML Training manager started");
        
        try
        {
            _logger.LogInformation("Starting LightGBM model training process...");
            
            // Train the model (includes evaluation and feature importance)
            await _trainingService.TrainModel(stoppingToken);
            
            _logger.LogInformation("🎉 Complete ML training pipeline finished successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ML training process");
            throw;
        }
    }
}

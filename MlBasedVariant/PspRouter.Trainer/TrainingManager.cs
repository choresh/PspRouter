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
            _logger.LogInformation("Entire ML training process started...");
            
            // Train the main routing model (includes evaluation and feature importance)
            await _trainingService.TrainPspSelectionModel(stoppingToken);
            
            // Train PSP performance prediction models
            await _trainingService.TrainPspPerformanceModels(stoppingToken);
            
            _logger.LogInformation("🎉 Entire ML training process finished successfully!");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Entire ML training process failed");
            throw;
        }
    }
}

using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace PspRouter.Trainer;

// Training manager for running the fine-tuning process
public class TrainingManager : BackgroundService
{
    private readonly ITrainingService _trainingService;
    private readonly ILogger<TrainingManager> _logger;

    public TrainingManager(ITrainingService trainingService, ILogger<TrainingManager> logger)
    {
        _trainingService = trainingService;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Training manager started");
        
        try
        {
            _logger.LogInformation("Starting complete fine-tuning process...");
            
            // Run the complete fine-tuning workflow
            var modelId = await _trainingService.CreateFineTunedModel(stoppingToken);
            
            _logger.LogInformation("🎉 Fine-tuning completed successfully! Model ID: {ModelId}", modelId);
            
            // TODO: Save the model ID to configuration or database for use in the router
            _logger.LogInformation("Fine-tuned model is ready for use in PSP routing decisions");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during training process");
            throw;
        }
    }
}

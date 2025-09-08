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
            _logger.LogInformation("Starting complete fine-tuning process...");
            
            // Use the complete fine-tuning workflow
            var modelId = await _trainingService.CreateFineTunedModelAsync(stoppingToken);
            
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

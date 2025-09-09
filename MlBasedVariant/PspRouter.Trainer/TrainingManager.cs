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
            
            // Train the model
            var modelPath = await _trainingService.TrainModel(stoppingToken);
            
            _logger.LogInformation("🎉 Model training completed successfully! Model saved to: {ModelPath}", modelPath);
            
            // Evaluate the model
            _logger.LogInformation("Evaluating trained model...");
            var metrics = await _trainingService.EvaluateModel(modelPath, stoppingToken);
            
            _logger.LogInformation("Model Evaluation Results:");
            _logger.LogInformation("  Accuracy: {Accuracy:F4}", metrics.Accuracy);
            _logger.LogInformation("  Precision: {Precision:F4}", metrics.Precision);
            _logger.LogInformation("  Recall: {Recall:F4}", metrics.Recall);
            _logger.LogInformation("  F1 Score: {F1Score:F4}", metrics.F1Score);
            _logger.LogInformation("  AUC: {AUC:F4}", metrics.AUC);
            _logger.LogInformation("  Log Loss: {LogLoss:F4}", metrics.LogLoss);
            
            _logger.LogInformation("Top Feature Importance:");
            foreach (var feature in metrics.FeatureImportance.OrderByDescending(x => x.Value).Take(10))
            {
                _logger.LogInformation("  {Feature}: {Importance:F4}", feature.Key, feature.Value);
            }
            
            _logger.LogInformation("✅ LightGBM model is ready for use in PSP routing decisions");
            _logger.LogInformation("Model file: {ModelPath}", modelPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during ML training process");
            throw;
        }
    }
}

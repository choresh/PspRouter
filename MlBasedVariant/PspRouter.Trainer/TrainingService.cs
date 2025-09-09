using Microsoft.Extensions.Logging;
using Microsoft.ML;
using System.Text.Json;

namespace PspRouter.Trainer;

/// <summary>
/// LightGBM-based training service for PSP routing
/// </summary>
public class TrainingService : ITrainingService
{
    private readonly ILogger<TrainingService> _logger;
    private readonly ITrainingDataProvider _trainingDataProvider;
    private readonly FeatureExtractor _featureExtractor;
    private readonly ModelTrainingConfig _config;
    private readonly MLContext _mlContext;
    private ITransformer? _trainedModel;

    public TrainingService(
        ILogger<TrainingService> logger,
        ITrainingDataProvider trainingDataProvider,
        FeatureExtractor featureExtractor,
        ModelTrainingConfig config)
    {
        _logger = logger;
        _trainingDataProvider = trainingDataProvider;
        _featureExtractor = featureExtractor;
        _config = config;
        _mlContext = new MLContext(seed: _config.Seed);
    }

    public async Task<string> TrainModel(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting LightGBM model training...");
        
        try
        {
            // 1. Load training data
            _logger.LogInformation("Step 1: Loading training data...");
            var trainingData = await _trainingDataProvider.GetTrainingData(cancellationToken);
            
            // 2. Extract features
            _logger.LogInformation("Step 2: Extracting features...");
            var trainingExamples = _featureExtractor.ExtractFeatures(trainingData).ToList();
            
            if (trainingExamples.Count == 0)
            {
                throw new InvalidOperationException("No training examples extracted from data");
            }
            
            _logger.LogInformation("Extracted {Count} training examples", trainingExamples.Count);
            
            // 3. Prepare ML.NET data
            _logger.LogInformation("Step 3: Preparing ML.NET data...");
            var dataView = _mlContext.Data.LoadFromEnumerable(trainingExamples);
            
            // 4. Split data for training and validation
            _logger.LogInformation("Step 4: Splitting data for training and validation...");
            var trainTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: _config.ValidationFraction);
            
            // 5. Define training pipeline
            _logger.LogInformation("Step 5: Defining training pipeline...");
            var pipeline = CreateTrainingPipeline();
            
            // 6. Train the model
            _logger.LogInformation("Step 6: Training LightGBM model...");
            _trainedModel = pipeline.Fit(trainTestSplit.TrainSet);
            
            // 7. Evaluate the model
            _logger.LogInformation("Step 7: Evaluating model...");
            var predictions = _trainedModel.Transform(trainTestSplit.TestSet);
            var metrics = _mlContext.BinaryClassification.Evaluate(predictions);
            
            _logger.LogInformation("Model training completed successfully!");
            _logger.LogInformation("Accuracy: {Accuracy:F4}", metrics.Accuracy);
            _logger.LogInformation("AUC: {AUC:F4}", metrics.AreaUnderRocCurve);
            _logger.LogInformation("F1 Score: {F1Score:F4}", metrics.F1Score);
            
            // 8. Save the model
            var modelPath = "models/psp_routing_model.zip";
            await SaveModel(modelPath, cancellationToken);
            
            return modelPath;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error training LightGBM model");
            throw;
        }
    }

    private async Task<ModelMetrics> EvaluateModel(string modelPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Evaluating model from {ModelPath}", modelPath);
        
        try
        {
            // Load the model
            var model = _mlContext.Model.Load(modelPath, out var modelInputSchema);
            
            // Load test data
            var trainingData = await _trainingDataProvider.GetTrainingData(cancellationToken);
            var testExamples = _featureExtractor.ExtractFeatures(trainingData).ToList();
            var dataView = _mlContext.Data.LoadFromEnumerable(testExamples);
            
            // Split for evaluation (use last 20% as test)
            var testCount = (int)(testExamples.Count * 0.2);
            var testData = testExamples.TakeLast(testCount);
            var testDataView = _mlContext.Data.LoadFromEnumerable(testData);
            
            // Make predictions
            var predictions = model.Transform(testDataView);
            
            // Calculate metrics
            var metrics = _mlContext.BinaryClassification.Evaluate(predictions);
            
            // Get feature importance
            var featureImportance = GetFeatureImportance(model);
            
            return new ModelMetrics(
                Accuracy: (float)metrics.Accuracy,
                Precision: (float)metrics.PositivePrecision,
                Recall: (float)metrics.PositiveRecall,
                F1Score: (float)metrics.F1Score,
                AUC: (float)metrics.AreaUnderRocCurve,
                LogLoss: (float)metrics.LogLoss,
                FeatureImportance: featureImportance
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error evaluating model from {ModelPath}", modelPath);
            throw;
        }
    }

    private Task<bool> SaveModel(string modelPath, CancellationToken cancellationToken = default)
    {
        if (_trainedModel == null)
        {
            _logger.LogError("No trained model to save");
            return Task.FromResult(false);
        }
        
        try
        {
            _logger.LogInformation("Saving model to {ModelPath}", modelPath);
            
            // Ensure directory exists
            var directory = Path.GetDirectoryName(modelPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            
            // Save the model
            _mlContext.Model.Save(_trainedModel, null, modelPath);
            
            // Save model metadata
            var metadataPath = Path.ChangeExtension(modelPath, ".metadata.json");
            var metadata = new
            {
                ModelType = "LightGBM",
                TrainingDate = DateTime.UtcNow,
                Config = _config,
                Version = "1.0"
            };
            
            File.WriteAllText(metadataPath, JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
            
            _logger.LogInformation("Model saved successfully to {ModelPath}", modelPath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving model to {ModelPath}", modelPath);
            return Task.FromResult(false);
        }
    }

    private Task<bool> LoadModel(string modelPath, CancellationToken cancellationToken = default)
    {
        try
        {
            _logger.LogInformation("Loading model from {ModelPath}", modelPath);
            
            if (!File.Exists(modelPath))
            {
                _logger.LogError("Model file not found: {ModelPath}", modelPath);
                return Task.FromResult(false);
            }
            
            _trainedModel = _mlContext.Model.Load(modelPath, out var modelInputSchema);
            
            _logger.LogInformation("Model loaded successfully from {ModelPath}", modelPath);
            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error loading model from {ModelPath}", modelPath);
            return Task.FromResult(false);
        }
    }

    private IEstimator<ITransformer> CreateTrainingPipeline()
    {
        // Define feature columns
        var featureColumns = new[]
        {
            nameof(RoutingFeatures.Amount),
            nameof(RoutingFeatures.PaymentMethodId),
            nameof(RoutingFeatures.CurrencyId),
            nameof(RoutingFeatures.CountryId),
            nameof(RoutingFeatures.RiskScore),
            nameof(RoutingFeatures.IsTokenized),
            nameof(RoutingFeatures.HasThreeDS),
            nameof(RoutingFeatures.IsRerouted),
            nameof(RoutingFeatures.PspAuthRate),
            nameof(RoutingFeatures.PspFeeBps),
            nameof(RoutingFeatures.PspFixedFee),
            nameof(RoutingFeatures.PspSupports3DS),
            nameof(RoutingFeatures.PspHealth),
            nameof(RoutingFeatures.PspId),
            nameof(RoutingFeatures.FeeRatio),
            nameof(RoutingFeatures.RiskAdjustedAuthRate),
            nameof(RoutingFeatures.ComplianceScore),
            nameof(RoutingFeatures.AmountLog),
            nameof(RoutingFeatures.HourOfDay),
            nameof(RoutingFeatures.DayOfWeek)
        };

        return _mlContext.Transforms.Concatenate("Features", featureColumns)
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.BinaryClassification.Trainers.LightGbm(
                labelColumnName: nameof(RoutingTrainingExample.IsSuccessful),
                featureColumnName: "Features",
                numberOfLeaves: _config.NumLeaves,
                minimumExampleCountPerLeaf: _config.MinDataInLeaf,
                learningRate: _config.LearningRate,
                numberOfIterations: _config.MaxIterations));
    }

    private Dictionary<string, float> GetFeatureImportance(ITransformer model)
    {
        // This is a simplified feature importance extraction
        // In a real implementation, you'd extract this from the LightGBM model
        var featureNames = new[]
        {
            "Amount", "PaymentMethodId", "CurrencyId", "CountryId", "RiskScore",
            "IsTokenized", "HasThreeDS", "IsRerouted", "PspAuthRate", "PspFeeBps",
            "PspFixedFee", "PspSupports3DS", "PspHealth", "PspId", "FeeRatio",
            "RiskAdjustedAuthRate", "ComplianceScore", "AmountLog", "HourOfDay", "DayOfWeek"
        };
        
        // For now, return equal importance (in production, extract from model)
        return featureNames.ToDictionary(name => name, _ => 1.0f / featureNames.Length);
    }
}

using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.ML;
using System.Runtime;

namespace PspRouter.Lib;

/// <summary>
/// Service for retraining ML models with new feedback data
/// </summary>
public interface IMLModelRetrainingService
{
    /// <summary>
    /// Retrain all ML models with accumulated feedback data
    /// </summary>
    Task RetrainAllModelsAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Retrain a specific model for a PSP
    /// </summary>
    Task RetrainPspModelAsync(string pspName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if models need retraining based on data volume and time
    /// </summary>
    bool ShouldRetrainModels();
    
    /// <summary>
    /// Get retraining statistics
    /// </summary>
    Task<RetrainingStats> GetRetrainingStatsAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Implementation of ML model retraining service
/// </summary>
public class MLModelRetrainingService : IMLModelRetrainingService
{
    private readonly ILogger<MLModelRetrainingService> _logger;
    private readonly IPspPerformancePredictor _performancePredictor;
    private readonly string _modelBasePath;
    private readonly MLContext _mlContext;
    private DateTime _lastRetraining = DateTime.MinValue;
    private int _totalRetrainingCount = 0;

    public MLModelRetrainingService(
        ILogger<MLModelRetrainingService> logger,
        IPspPerformancePredictor performancePredictor)
    {
        _logger = logger;
        _performancePredictor = performancePredictor;
        _mlContext = new MLContext(seed: 42);
        
        // Get model path from solution root
        var solutionRoot = GetSolutionRoot();
        _modelBasePath = Path.Combine(solutionRoot, "models");
        
        _logger.LogInformation("ML Model Retraining Service initialized. Model path: {ModelPath}", _modelBasePath);
    }

    public async Task RetrainAllModelsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting full ML model retraining...");
        
        try
        {
            // 1. Collect all feedback data from all PSPs
            var allFeedbackData = await CollectAllFeedbackDataAsync(cancellationToken);
            
            if (!allFeedbackData.Any())
            {
                _logger.LogWarning("No feedback data available for retraining");
                return;
            }
            
            _logger.LogInformation("Collected {Count} feedback records for retraining", allFeedbackData.Count);
            
            // 2. Retrain each PSP's models
            var pspNames = allFeedbackData.Select(f => f.PspName).Distinct().ToList();
            var successCount = 0;
            
            foreach (var pspName in pspNames)
            {
                try
                {
                    await RetrainPspModelAsync(pspName, cancellationToken);
                    successCount++;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrain models for PSP {PspName}", pspName);
                }
            }
            
            _lastRetraining = DateTime.UtcNow;
            _totalRetrainingCount++;
            
            _logger.LogInformation("ML model retraining completed. Successfully retrained {SuccessCount}/{TotalCount} PSPs", 
                successCount, pspNames.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during full ML model retraining");
            throw;
        }
    }

    public async Task RetrainPspModelAsync(string pspName, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retraining ML models for PSP {PspName}...", pspName);
        
        try
        {
            // 1. Get training data for this PSP from PaymentTransactions database
            var trainingFeatures = await GetPspPerformanceDataFromDatabaseAsync(pspName, cancellationToken);
            
            if (!trainingFeatures.Any())
            {
                _logger.LogWarning("No PaymentTransactions data available for PSP {PspName} retraining", pspName);
                return;
            }
            
            _logger.LogInformation("Retrieved {Count} training records for PSP {PspName} retraining", 
                trainingFeatures.Count, pspName);
            
            // 2. Retrain each model type with database data
            await RetrainSuccessRateModelAsync(pspName, trainingFeatures, cancellationToken);
            await RetrainProcessingTimeModelAsync(pspName, trainingFeatures, cancellationToken);
            await RetrainHealthModelAsync(pspName, trainingFeatures, cancellationToken);
            
            _logger.LogInformation("Successfully retrained all models for PSP {PspName} using PaymentTransactions data", pspName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retraining models for PSP {PspName}", pspName);
            throw;
        }
    }

    public bool ShouldRetrainModels()
    {
        // Retrain if:
        // 1. Never retrained before
        // 2. Last retraining was more than 24 hours ago
        // 3. Performance predictor indicates retraining needed
        return _lastRetraining == DateTime.MinValue ||
               DateTime.UtcNow - _lastRetraining > TimeSpan.FromHours(24) ||
               _performancePredictor.ShouldRetrain();
    }

    public async Task<RetrainingStats> GetRetrainingStatsAsync(CancellationToken cancellationToken = default)
    {
        return await Task.FromResult(new RetrainingStats
        {
            LastRetraining = _lastRetraining,
            TotalRetrainingCount = _totalRetrainingCount,
            ShouldRetrain = ShouldRetrainModels(),
            ModelPath = _modelBasePath
        });
    }

    private async Task<List<TransactionFeedback>> CollectAllFeedbackDataAsync(CancellationToken cancellationToken)
    {
        // For now, we'll collect feedback data from the database
        // In a production system, this could be from in-memory storage, message queues, etc.
        var allFeedback = new List<TransactionFeedback>();
        
        try
        {
            var connectionString = GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // Query to get recent transaction feedback data
            var query = @"
                SELECT TOP 1000
                    pt.PaymentTransactionId as DecisionId,
                    CAST(pt.PaymentGatewayId AS NVARCHAR(50)) as MerchantId,  -- Use PaymentGatewayId as MerchantId for now
                    CAST(pt.PaymentGatewayId AS NVARCHAR(50)) as PspName,
                    CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END as Authorized,
                    pt.Amount as TransactionAmount,
                    CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN pt.Amount * 0.025 ELSE 0 END as FeeAmount,
                    CASE 
                        WHEN pt.DateStatusLastUpdated IS NULL OR pt.DateCreated IS NULL THEN 0
                        WHEN pt.DateStatusLastUpdated < pt.DateCreated THEN 0
                        WHEN DATEDIFF(DAY, pt.DateCreated, pt.DateStatusLastUpdated) > 30 THEN 0
                        WHEN DATEDIFF(DAY, pt.DateCreated, pt.DateStatusLastUpdated) < 0 THEN 0
                        ELSE CAST(DATEDIFF(MINUTE, pt.DateCreated, pt.DateStatusLastUpdated) AS BIGINT) * 60000
                    END as ProcessingTimeMs,
                    CAST(50 AS INT) as RiskScore, -- Default risk score
                    pt.DateCreated as ProcessedAt,
                    CASE WHEN pt.PaymentTransactionStatusId NOT IN (5, 7, 9) THEN 'DECLINED' ELSE NULL END as ErrorCode,
                    CASE WHEN pt.PaymentTransactionStatusId NOT IN (5, 7, 9) THEN 'Transaction declined' ELSE NULL END as ErrorMessage,
                    'ml' as RoutingMethod
                FROM PaymentTransactions pt
                WHERE pt.DateCreated >= DATEADD(DAY, -7, GETDATE())  -- Last 7 days
                    AND pt.PaymentGatewayId IS NOT NULL
                    AND pt.PaymentTransactionStatusId IN (5, 7, 9, 11, 15, 17, 22)
                ORDER BY pt.DateCreated DESC";
            
            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 30;
            
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var feedback = new TransactionFeedback(
                    DecisionId: reader.GetInt64(0).ToString(),  // Convert Int64 to String
                    MerchantId: reader.GetString(1),
                    PspName: reader.GetString(2),
                    Authorized: Convert.ToInt32(reader.GetValue(3)) == 1,  // Use flexible conversion
                    TransactionAmount: reader.GetDecimal(4),
                    FeeAmount: reader.GetDecimal(5),
                    ProcessingTimeMs: Convert.ToInt32(reader.GetValue(6)),  // Use flexible conversion
                    RiskScore: Convert.ToInt32(reader.GetValue(7)),  // Use flexible conversion
                    ProcessedAt: reader.GetDateTime(8),
                    ErrorCode: reader.IsDBNull(9) ? null : reader.GetString(9),
                    ErrorMessage: reader.IsDBNull(10) ? null : reader.GetString(10),
                    RoutingMethod: reader.GetString(11)
                );
                
                allFeedback.Add(feedback);
            }
            
            _logger.LogInformation("Collected {Count} feedback records for retraining", allFeedback.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting feedback data for retraining");
            // Return empty list if collection fails
        }
        
        return allFeedback;
    }

    private async Task<List<TransactionFeedback>> GetPspFeedbackDataAsync(string pspName, CancellationToken cancellationToken)
    {
        // Get feedback data for a specific PSP from the database
        var pspFeedback = new List<TransactionFeedback>();
        
        try
        {
            var connectionString = GetConnectionString();
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // Get PSP ID from name
            var pspId = GetPspIdFromName(pspName);
            if (pspId == null)
            {
                _logger.LogWarning("PSP {PspName} not found in database", pspName);
                return pspFeedback;
            }
            
            // Query to get recent transaction feedback data for specific PSP
            var query = @"
                SELECT TOP 500
                    pt.PaymentTransactionId as DecisionId,
                    CAST(pt.PaymentGatewayId AS NVARCHAR(50)) as MerchantId,  -- Use PaymentGatewayId as MerchantId for now
                    CAST(pt.PaymentGatewayId AS NVARCHAR(50)) as PspName,
                    CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END as Authorized,
                    pt.Amount as TransactionAmount,
                    CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN pt.Amount * 0.025 ELSE 0 END as FeeAmount,
                    CASE 
                        WHEN pt.DateStatusLastUpdated IS NULL OR pt.DateCreated IS NULL THEN 0
                        WHEN pt.DateStatusLastUpdated < pt.DateCreated THEN 0
                        WHEN DATEDIFF(DAY, pt.DateCreated, pt.DateStatusLastUpdated) > 30 THEN 0
                        WHEN DATEDIFF(DAY, pt.DateCreated, pt.DateStatusLastUpdated) < 0 THEN 0
                        ELSE CAST(DATEDIFF(MINUTE, pt.DateCreated, pt.DateStatusLastUpdated) AS BIGINT) * 60000
                    END as ProcessingTimeMs,
                    CAST(50 AS INT) as RiskScore, -- Default risk score
                    pt.DateCreated as ProcessedAt,
                    CASE WHEN pt.PaymentTransactionStatusId NOT IN (5, 7, 9) THEN 'DECLINED' ELSE NULL END as ErrorCode,
                    CASE WHEN pt.PaymentTransactionStatusId NOT IN (5, 7, 9) THEN 'Transaction declined' ELSE NULL END as ErrorMessage,
                    'ml' as RoutingMethod
                FROM PaymentTransactions pt
                WHERE pt.PaymentGatewayId = @PspId
                    AND pt.DateCreated >= DATEADD(DAY, -7, GETDATE())  -- Last 7 days
                    AND pt.PaymentTransactionStatusId IN (5, 7, 9, 11, 15, 17, 22)
                ORDER BY pt.DateCreated DESC";
            
            using var command = new SqlCommand(query, connection);
            command.Parameters.AddWithValue("@PspId", pspId);
            command.CommandTimeout = 30;
            
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var feedback = new TransactionFeedback(
                    DecisionId: reader.GetInt64(0).ToString(),  // Convert Int64 to String
                    MerchantId: reader.GetString(1),
                    PspName: reader.GetString(2),
                    Authorized: Convert.ToInt32(reader.GetValue(3)) == 1,  // Use flexible conversion
                    TransactionAmount: reader.GetDecimal(4),
                    FeeAmount: reader.GetDecimal(5),
                    ProcessingTimeMs: Convert.ToInt32(reader.GetValue(6)),  // Use flexible conversion
                    RiskScore: Convert.ToInt32(reader.GetValue(7)),  // Use flexible conversion
                    ProcessedAt: reader.GetDateTime(8),
                    ErrorCode: reader.IsDBNull(9) ? null : reader.GetString(9),
                    ErrorMessage: reader.IsDBNull(10) ? null : reader.GetString(10),
                    RoutingMethod: reader.GetString(11)
                );
                
                pspFeedback.Add(feedback);
            }
            
            _logger.LogInformation("Collected {Count} feedback records for PSP {PspName}", pspFeedback.Count, pspName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error collecting feedback data for PSP {PspName}", pspName);
            // Return empty list if collection fails
        }
        
        return pspFeedback;
    }

    private async Task<List<PspPerformanceFeatures>> GetPspPerformanceDataFromDatabaseAsync(string pspName, CancellationToken cancellationToken)
    {
        // Option 2: Get data directly from PaymentTransactions table (comprehensive, historical)
        // This is more comprehensive but slower and requires database access
        
        var features = new List<PspPerformanceFeatures>();
        
        try
        {
            // Get connection string from settings (would need to be injected)
            var connectionString = GetConnectionString();
            
            using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync(cancellationToken);
            
            // Query recent PaymentTransactions for this PSP
            var query = @"
                SELECT 
                    pt.PaymentGatewayId,
                    CAST(pt.PaymentGatewayId AS NVARCHAR(50)) as PspName,
                    pt.PaymentTransactionId,
                    pt.OrderId,
                    pt.Amount,
                    pt.PaymentMethodId,
                    pt.CurrencyId,
                    pt.CountryId,
                    pt.PaymentCardBin,
                    pt.ThreeDSTypeId,
                    pt.IsTokenized,
                    pt.PaymentTransactionStatusId,
                    pt.GatewayStatusReason,
                    pt.GatewayResponseCode,
                    pt.IsReroutedFlag,
                    pt.PaymentRoutingRuleId,
                    pt.DateCreated,
                    pt.DateStatusLastUpdated,
                    pt.PspReference,
                    CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END AS IsSuccess,
                    CASE 
                        WHEN pt.DateStatusLastUpdated IS NULL OR pt.DateCreated IS NULL THEN 0
                        WHEN pt.DateStatusLastUpdated < pt.DateCreated THEN 0
                        WHEN DATEDIFF(DAY, pt.DateCreated, pt.DateStatusLastUpdated) > 30 THEN 0
                        WHEN DATEDIFF(DAY, pt.DateCreated, pt.DateStatusLastUpdated) < 0 THEN 0
                        ELSE CAST(DATEDIFF(MINUTE, pt.DateCreated, pt.DateStatusLastUpdated) AS BIGINT) * 60000
                    END AS ProcessingTimeMs,
                    CAST(0.85 AS FLOAT) AS RecentSuccessRate,
                    CAST(2000 AS FLOAT) AS RecentProcessingTime,
                    CAST(0 AS INT) AS RecentVolume
                FROM PaymentTransactions pt
                WHERE pt.PaymentGatewayId = @PspId
                    AND pt.DateCreated >= DATEADD(DAY, -7, GETDATE())  -- Last 7 days for retraining
                    AND pt.PaymentGatewayId IS NOT NULL
                    AND pt.PaymentTransactionStatusId IN (5, 7, 9, 11, 15, 17, 22)
                    AND pt.CountryId IS NOT NULL
                    AND pt.PaymentMethodId IS NOT NULL
                ORDER BY pt.DateCreated DESC";
            
            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = 60; // 1 minute timeout for retraining
            command.Parameters.AddWithValue("@PspId", GetPspIdFromName(pspName));
            
            using var reader = await command.ExecuteReaderAsync(cancellationToken);
            
            while (await reader.ReadAsync(cancellationToken))
            {
                var feature = new PspPerformanceFeatures
                {
                    Amount = (float)reader.GetDecimal(4),
                    PaymentMethodId = (int)reader.GetInt64(5),
                    CurrencyId = (int)reader.GetInt64(6),
                    CountryId = (int)reader.GetInt64(7),
                    RiskScore = 50, // Default
                    IsTokenized = reader.GetBoolean(10) ? 1f : 0f,
                    HasThreeDS = reader.GetInt64(9) > 0 ? 1f : 0f,
                    IsRerouted = reader.GetBoolean(14) ? 1f : 0f,
                    PspId = (int)reader.GetInt64(0),
                    HourOfDay = reader.GetDateTime(16).Hour,
                    DayOfWeek = (int)reader.GetDateTime(16).DayOfWeek,
                    DayOfMonth = reader.GetDateTime(16).Day,
                    MonthOfYear = reader.GetDateTime(16).Month,
                    RecentSuccessRate = Convert.ToSingle(reader.GetValue(21)),
                    RecentProcessingTime = Convert.ToSingle(reader.GetValue(22)),
                    RecentVolume = reader.GetInt32(23),
                    AmountLog = (float)Math.Log10(Math.Max((float)reader.GetDecimal(4), 1)),
                    RiskAdjustedAmount = (float)reader.GetDecimal(4) * 1.0f,
                    TimeOfDayCategory = GetTimeOfDayCategory(reader.GetDateTime(16).Hour),
                    ProcessingTimeMs = (int)reader.GetInt64(20),
                    HealthScore = CalculateHealthScore(
                        reader.GetInt32(19) == 1 ? 1.0f : 0.0f,
                        (int)reader.GetInt64(20)
                    )
                };
                
                features.Add(feature);
            }
            
            _logger.LogInformation("Retrieved {Count} PaymentTransactions records for PSP {PspName} retraining", 
                features.Count, pspName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving PaymentTransactions data for PSP {PspName} retraining", pspName);
            throw;
        }
        
        return features;
    }

    private string GetConnectionString()
    {
        // Get .env file variable
        var baseConnectionString = Environment.GetEnvironmentVariable("PSPROUTER_DB_CONNECTION")
            ?? throw new InvalidOperationException("PSPROUTER_DB_CONNECTION environment variable is required");
        
        /* TODO: Fix tis
        // Ensure TrustServerCertificate=true is included to handle SSL certificate issues
        if (_settings.TrustServerCertificate && !baseConnectionString.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
        {
            return baseConnectionString.TrimEnd(';') + ";TrustServerCertificate=true;";
        }*/

        return baseConnectionString.TrimEnd(';') + ";TrustServerCertificate=true;";


        return baseConnectionString;
    }

    private List<PspPerformanceFeatures> ConvertFeedbackToFeatures(List<TransactionFeedback> feedback)
    {
        // Convert feedback data to training features
        // This is a simplified version - in real implementation, this would be more comprehensive
        return feedback.Select(f => new PspPerformanceFeatures
        {
            // Map feedback to features
            Amount = (float)f.TransactionAmount,
            PaymentMethodId = GetPaymentMethodIdFromFeedback(f),
            CurrencyId = 1, // Default
            CountryId = int.Parse(f.MerchantId), // Convert merchant ID to int
            RiskScore = f.RiskScore,
            IsTokenized = 0f,
            HasThreeDS = 0f,
            IsRerouted = 0f,
            PspId = GetPspIdFromName(f.PspName),
            HourOfDay = f.ProcessedAt.Hour,
            DayOfWeek = (int)f.ProcessedAt.DayOfWeek,
            DayOfMonth = f.ProcessedAt.Day,
            MonthOfYear = f.ProcessedAt.Month,
            RecentSuccessRate = f.Authorized ? 1.0f : 0.0f,
            RecentProcessingTime = f.ProcessingTimeMs,
            RecentVolume = 1,
            AmountLog = (float)Math.Log10(Math.Max((float)f.TransactionAmount, 1)),
            RiskAdjustedAmount = (float)f.TransactionAmount * 1.0f,
            TimeOfDayCategory = GetTimeOfDayCategory(f.ProcessedAt.Hour),
            ProcessingTimeMs = f.ProcessingTimeMs,
            HealthScore = CalculateHealthScore(f.Authorized ? 1.0f : 0.0f, f.ProcessingTimeMs)
        }).ToList();
    }

    private Task RetrainSuccessRateModelAsync(string pspName, List<PspPerformanceFeatures> features, CancellationToken cancellationToken)
    {
        if (features.Count < 10)
        {
            _logger.LogWarning("Insufficient data for retraining success rate model for PSP {PspName}", pspName);
            return Task.CompletedTask;
        }

        // Create data view
        var dataView = _mlContext.Data.LoadFromEnumerable(features);
        var trainTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        // Create pipeline
        var pipeline = _mlContext.Transforms.Concatenate("Features", 
                nameof(PspPerformanceFeatures.Amount),
                nameof(PspPerformanceFeatures.PaymentMethodId),
                nameof(PspPerformanceFeatures.CurrencyId),
                nameof(PspPerformanceFeatures.CountryId),
                nameof(PspPerformanceFeatures.RiskScore),
                nameof(PspPerformanceFeatures.IsTokenized),
                nameof(PspPerformanceFeatures.HasThreeDS),
                nameof(PspPerformanceFeatures.IsRerouted),
                nameof(PspPerformanceFeatures.PspId),
                nameof(PspPerformanceFeatures.HourOfDay),
                nameof(PspPerformanceFeatures.DayOfWeek),
                nameof(PspPerformanceFeatures.DayOfMonth),
                nameof(PspPerformanceFeatures.MonthOfYear),
                nameof(PspPerformanceFeatures.RecentSuccessRate),
                nameof(PspPerformanceFeatures.RecentProcessingTime),
                nameof(PspPerformanceFeatures.RecentVolume),
                nameof(PspPerformanceFeatures.AmountLog),
                nameof(PspPerformanceFeatures.RiskAdjustedAmount),
                nameof(PspPerformanceFeatures.TimeOfDayCategory))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Regression.Trainers.LightGbm(
                labelColumnName: nameof(PspPerformanceFeatures.RecentSuccessRate),
                featureColumnName: "Features",
                numberOfLeaves: 31,
                minimumExampleCountPerLeaf: 20,
                learningRate: 0.1,
                numberOfIterations: 100));

        // Train model
        var trainedModel = pipeline.Fit(trainTestSplit.TrainSet);

        // Save model
        var modelPath = Path.Combine(_modelBasePath, $"{pspName}_success_rate_model.zip");
        _mlContext.Model.Save(trainedModel, dataView.Schema, modelPath);

        _logger.LogInformation("Success rate model retrained and saved for PSP {PspName}", pspName);
        return Task.CompletedTask;
    }

    private Task RetrainProcessingTimeModelAsync(string pspName, List<PspPerformanceFeatures> features, CancellationToken cancellationToken)
    {
        if (features.Count < 10)
        {
            _logger.LogWarning("Insufficient data for retraining processing time model for PSP {PspName}", pspName);
            return Task.CompletedTask;
        }

        // Similar implementation for processing time model
        var dataView = _mlContext.Data.LoadFromEnumerable(features);
        var trainTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        var pipeline = _mlContext.Transforms.Concatenate("Features", 
                nameof(PspPerformanceFeatures.Amount),
                nameof(PspPerformanceFeatures.PaymentMethodId),
                nameof(PspPerformanceFeatures.CurrencyId),
                nameof(PspPerformanceFeatures.CountryId),
                nameof(PspPerformanceFeatures.RiskScore),
                nameof(PspPerformanceFeatures.IsTokenized),
                nameof(PspPerformanceFeatures.HasThreeDS),
                nameof(PspPerformanceFeatures.IsRerouted),
                nameof(PspPerformanceFeatures.PspId),
                nameof(PspPerformanceFeatures.HourOfDay),
                nameof(PspPerformanceFeatures.DayOfWeek),
                nameof(PspPerformanceFeatures.DayOfMonth),
                nameof(PspPerformanceFeatures.MonthOfYear),
                nameof(PspPerformanceFeatures.RecentSuccessRate),
                nameof(PspPerformanceFeatures.RecentProcessingTime),
                nameof(PspPerformanceFeatures.RecentVolume),
                nameof(PspPerformanceFeatures.AmountLog),
                nameof(PspPerformanceFeatures.RiskAdjustedAmount),
                nameof(PspPerformanceFeatures.TimeOfDayCategory))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Regression.Trainers.LightGbm(
                labelColumnName: nameof(PspPerformanceFeatures.ProcessingTimeMs),
                featureColumnName: "Features",
                numberOfLeaves: 31,
                minimumExampleCountPerLeaf: 20,
                learningRate: 0.1,
                numberOfIterations: 100));

        var trainedModel = pipeline.Fit(trainTestSplit.TrainSet);

        var modelPath = Path.Combine(_modelBasePath, $"{pspName}_processing_time_model.zip");
        _mlContext.Model.Save(trainedModel, dataView.Schema, modelPath);

        _logger.LogInformation("Processing time model retrained and saved for PSP {PspName}", pspName);
        return Task.CompletedTask;
    }

    private Task RetrainHealthModelAsync(string pspName, List<PspPerformanceFeatures> features, CancellationToken cancellationToken)
    {
        if (features.Count < 10)
        {
            _logger.LogWarning("Insufficient data for retraining health model for PSP {PspName}", pspName);
            return Task.CompletedTask;
        }

        // Similar implementation for health model
        var dataView = _mlContext.Data.LoadFromEnumerable(features);
        var trainTestSplit = _mlContext.Data.TrainTestSplit(dataView, testFraction: 0.2);

        var pipeline = _mlContext.Transforms.Concatenate("Features", 
                nameof(PspPerformanceFeatures.Amount),
                nameof(PspPerformanceFeatures.PaymentMethodId),
                nameof(PspPerformanceFeatures.CurrencyId),
                nameof(PspPerformanceFeatures.CountryId),
                nameof(PspPerformanceFeatures.RiskScore),
                nameof(PspPerformanceFeatures.IsTokenized),
                nameof(PspPerformanceFeatures.HasThreeDS),
                nameof(PspPerformanceFeatures.IsRerouted),
                nameof(PspPerformanceFeatures.PspId),
                nameof(PspPerformanceFeatures.HourOfDay),
                nameof(PspPerformanceFeatures.DayOfWeek),
                nameof(PspPerformanceFeatures.DayOfMonth),
                nameof(PspPerformanceFeatures.MonthOfYear),
                nameof(PspPerformanceFeatures.RecentSuccessRate),
                nameof(PspPerformanceFeatures.RecentProcessingTime),
                nameof(PspPerformanceFeatures.RecentVolume),
                nameof(PspPerformanceFeatures.AmountLog),
                nameof(PspPerformanceFeatures.RiskAdjustedAmount),
                nameof(PspPerformanceFeatures.TimeOfDayCategory))
            .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
            .Append(_mlContext.Transforms.Conversion.MapValueToKey("HealthScoreKey", nameof(PspPerformanceFeatures.HealthScore)))
            .Append(_mlContext.MulticlassClassification.Trainers.LightGbm(
                labelColumnName: "HealthScoreKey",
                featureColumnName: "Features",
                numberOfLeaves: 31,
                minimumExampleCountPerLeaf: 20,
                learningRate: 0.1,
                numberOfIterations: 100));

        var trainedModel = pipeline.Fit(trainTestSplit.TrainSet);

        var modelPath = Path.Combine(_modelBasePath, $"{pspName}_health_model.zip");
        _mlContext.Model.Save(trainedModel, dataView.Schema, modelPath);

        _logger.LogInformation("Health model retrained and saved for PSP {PspName}", pspName);
        return Task.CompletedTask;
    }

    private string GetSolutionRoot()
    {
        var currentDirectory = Directory.GetCurrentDirectory();
        var directory = new DirectoryInfo(currentDirectory);
        
        while (directory != null)
        {
            var solutionFiles = directory.GetFiles("*.sln");
            if (solutionFiles.Length > 0)
            {
                return directory.FullName;
            }
            directory = directory.Parent;
        }
        
        return currentDirectory;
    }

    private int GetPaymentMethodIdFromFeedback(TransactionFeedback feedback)
    {
        // Map feedback to payment method ID
        return 1; // Default to card
    }

    private int GetPspIdFromName(string pspName)
    {
        // Map PSP name to ID
        return pspName.GetHashCode() % 1000; // Simple hash-based ID
    }

    private int GetTimeOfDayCategory(int hour)
    {
        return hour switch
        {
            >= 6 and < 12 => 0,  // Morning
            >= 12 and < 18 => 1, // Afternoon
            >= 18 and < 22 => 2, // Evening
            _ => 3               // Night
        };
    }

    private float CalculateHealthScore(float successRate, int processingTime)
    {
        // Calculate health score based on success rate and processing time
        var timeScore = processingTime switch
        {
            < 1000 => 2.0f,  // Green
            < 3000 => 1.0f,  // Yellow
            _ => 0.0f        // Red
        };
        
        var successScore = successRate switch
        {
            >= 0.95f => 2.0f,  // Green
            >= 0.85f => 1.0f,  // Yellow
            _ => 0.0f          // Red
        };
        
        return (timeScore + successScore) / 2.0f;
    }
}

/// <summary>
/// Statistics about model retraining
/// </summary>
public class RetrainingStats
{
    public DateTime LastRetraining { get; set; }
    public int TotalRetrainingCount { get; set; }
    public bool ShouldRetrain { get; set; }
    public string ModelPath { get; set; } = string.Empty;
}

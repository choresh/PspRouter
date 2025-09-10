using Microsoft.Extensions.Logging;
using Microsoft.Data.SqlClient;

namespace PspRouter.Lib;

/// <summary>
/// In-memory PSP candidate provider with performance tracking and ML enhancement
/// </summary>
public class PspCandidateProvider : IPspCandidateProvider
{
    private readonly ILogger<PspCandidateProvider> _logger;
    private readonly Dictionary<string, PspCandidate> _candidates;
    private readonly object _lock = new object();
    private readonly PspCandidateSettings _settings;
    private readonly IPspPerformancePredictor? _performancePredictor;
    private readonly IMLModelRetrainingService? _retrainingService;

    public PspCandidateProvider(ILogger<PspCandidateProvider> logger, PspCandidateSettings settings, IPspPerformancePredictor? performancePredictor = null, IMLModelRetrainingService? retrainingService = null)
    {
        _logger = logger;
        _settings = settings;
        _performancePredictor = performancePredictor;
        _retrainingService = retrainingService;
        _candidates = LoadCandidatesFromDatabase();
    }

    public async Task<IReadOnlyList<PspSnapshot>> GetCandidates(RouteInput transaction, CancellationToken cancellationToken = default)
    {
        // 1. Get base candidates from database (inside lock)
        List<PspCandidate> baseCandidates;
        lock (_lock)
        {
            var allowedHealth = new[] { "green", "yellow" };

            baseCandidates = _candidates.Values
                .Where(c => c.Supports)
                .Where(c => allowedHealth.Contains(c.Health, StringComparer.OrdinalIgnoreCase))
                .Where(c => !(transaction.SCARequired && transaction.PaymentMethodId == 1 && !c.Supports3DS)) // PaymentMethodId 1 = Card
                .ToList();
        }

        // 2. Enhance with ML predictions if available (outside lock to allow await)
        if (_performancePredictor != null)
        {
            var enhancedCandidates = new List<PspSnapshot>();

            foreach (var candidate in baseCandidates)
            {
                try
                {
                    // Get ML predictions for this PSP
                    var successRate = await _performancePredictor.PredictSuccessRate(candidate.Name, transaction, cancellationToken);
                    var processingTime = await _performancePredictor.PredictProcessingTime(candidate.Name, transaction, cancellationToken);
                    var mlHealth = await _performancePredictor.PredictHealthStatus(candidate.Name, cancellationToken);

                    // Use ML predictions if they're better than historical data
                    var finalAuthRate = Math.Max(successRate, candidate.CurrentAuthRate);

                    enhancedCandidates.Add(new PspSnapshot(
                        candidate.Name,
                        candidate.Supports,
                        mlHealth, // Use ML-based health
                        finalAuthRate, // Use ML-enhanced auth rate
                        candidate.FeeBps,
                        candidate.FixedFee,
                        candidate.Supports3DS,
                        candidate.Tokenization
                    ));

                    _logger.LogDebug("Enhanced PSP {PspName} with ML predictions: Success={SuccessRate:P2}, Health={Health}",
                        candidate.Name, successRate, mlHealth);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to get ML predictions for PSP {PspName}, using historical data", candidate.Name);

                    // Fallback to historical data
                    enhancedCandidates.Add(new PspSnapshot(
                        candidate.Name,
                        candidate.Supports,
                        candidate.Health,
                        candidate.CurrentAuthRate,
                        candidate.FeeBps,
                        candidate.FixedFee,
                        candidate.Supports3DS,
                        candidate.Tokenization
                    ));
                }
            }

            _logger.LogInformation("Retrieved {Count} ML-enhanced PSP candidates for transaction {MerchantId}",
                enhancedCandidates.Count, transaction.MerchantId);

            return enhancedCandidates;
        }
        else
        {
            // No ML predictor available, use historical data
            var candidates = baseCandidates.Select(c => new PspSnapshot(
                    c.Name,
                    c.Supports,
                    c.Health,
                c.CurrentAuthRate,
                    c.FeeBps,
                    c.FixedFee,
                    c.Supports3DS,
                    c.Tokenization
            )).ToList();

            _logger.LogInformation("Retrieved {Count} PSP candidates (historical data only) for transaction {MerchantId}",
                candidates.Count, transaction.MerchantId);

            return candidates;
        }
    }

    public async Task ProcessFeedback(TransactionFeedback feedback, CancellationToken cancellationToken = default)
    {
        // 1. Update in-memory candidate data (inside lock)
        lock (_lock)
        {
            if (_candidates.TryGetValue(feedback.PspName, out var candidate))
            {
                var updatedCandidate = candidate.WithPerformanceUpdate(
                    feedback.Authorized,
                    feedback.ProcessingTimeMs,
                    feedback.MerchantId, // Using merchant ID as country proxy for now
                    GetPaymentMethodIdFromFeedback(feedback)
                );

                _candidates[feedback.PspName] = updatedCandidate;

                _logger.LogInformation("Updated PSP {PspName} performance: {SuccessRate:P2} auth rate, {AvgTime}ms avg processing time",
                    feedback.PspName, updatedCandidate.CurrentAuthRate, updatedCandidate.AverageProcessingTime);
            }
            else
            {
                _logger.LogWarning("Received feedback for unknown PSP: {PspName}", feedback.PspName);
            }
        }

        // 2. Update ML models with feedback (outside lock to allow await)
        if (_performancePredictor != null)
        {
            try
            {
                await _performancePredictor.UpdateWithFeedback(feedback, cancellationToken);
                _logger.LogDebug("Updated ML models with feedback for PSP {PspName}", feedback.PspName);

                // 3. Check if models need retraining and do it asynchronously
                if (_retrainingService != null && _retrainingService.ShouldRetrainModels())
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _retrainingService.RetrainPspModelAsync(feedback.PspName, cancellationToken);
                            _logger.LogInformation("ML models retrained with real data after feedback from PSP {PspName}", feedback.PspName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to retrain ML models after feedback from PSP {PspName}", feedback.PspName);
                        }
                    }, cancellationToken);
                }
                else if (_performancePredictor.ShouldRetrain())
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            await _performancePredictor.RetrainIncremental(cancellationToken);
                            _logger.LogInformation("ML models updated incrementally after feedback from PSP {PspName}", feedback.PspName);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Failed to update ML models after feedback from PSP {PspName}", feedback.PspName);
                        }
                    }, cancellationToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to update ML models with feedback for PSP {PspName}", feedback.PspName);
                // Don't throw - in-memory update was successful, ML update is optional
            }
        }
    }

    public Task<IReadOnlyList<PspCandidate>> GetAllCandidates(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<PspCandidate>>(_candidates.Values.ToList());
        }
    }

    public Task<PspCandidate?> GetCandidate(string pspName, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _candidates.TryGetValue(pspName, out var candidate);
            return Task.FromResult(candidate);
        }
    }

    private Dictionary<string, PspCandidate> LoadCandidatesFromDatabase()
    {
        _logger.LogInformation("Loading PSP candidates from database");

        try
        {
            var connectionString = GetConnectionString();
            var candidates = new Dictionary<string, PspCandidate>();

            using var connection = new SqlConnection(connectionString);
            connection.Open();

            // Query to derive PSP information from PaymentTransactions table
            var query = @"
                WITH PspStats AS (
                    SELECT 
                        PaymentGatewayId,
                        CAST(PaymentGatewayId AS NVARCHAR(50)) as PspName,
                        COUNT(*) as TotalTransactions,
                        SUM(CASE WHEN PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END) as SuccessfulTransactions,
                        AVG(CASE WHEN PaymentTransactionStatusId IN (5, 7, 9) THEN 1.0 ELSE 0.0 END) as AuthRate30d,
                        AVG(CASE 
                            WHEN DateStatusLastUpdated IS NULL OR DateCreated IS NULL THEN 0
                            WHEN DateStatusLastUpdated < DateCreated THEN 0
                            WHEN DATEDIFF(DAY, DateCreated, DateStatusLastUpdated) > 30 THEN 0
                            WHEN DATEDIFF(DAY, DateCreated, DateStatusLastUpdated) < 0 THEN 0
                            ELSE CAST(DATEDIFF(MINUTE, DateCreated, DateStatusLastUpdated) AS BIGINT) * 60000
                        END) as AvgProcessingTime,
                        MAX(CASE WHEN IsTokenized = 1 THEN 1 ELSE 0 END) as SupportsTokenization,
                        MAX(CASE WHEN ThreeDSTypeId IS NOT NULL THEN 1 ELSE 0 END) as Supports3DS
                    FROM PaymentTransactions 
                    WHERE DateCreated >= DATEADD(MONTH, -3, GETDATE())
                        AND PaymentGatewayId IS NOT NULL
                    GROUP BY PaymentGatewayId
                    HAVING COUNT(*) >= 10  -- Only include PSPs with at least 10 transactions
                )
                SELECT 
                    PaymentGatewayId,
                    PspName,
                    TotalTransactions,
                    SuccessfulTransactions,
                    AuthRate30d,
                    AvgProcessingTime,
                    SupportsTokenization,
                    Supports3DS,
                    CASE 
                        WHEN AuthRate30d >= 0.9 AND AvgProcessingTime <= 2000 THEN 'green'
                        WHEN AuthRate30d >= 0.8 AND AvgProcessingTime <= 3000 THEN 'yellow'
                        ELSE 'red'
                    END as HealthStatus
                FROM PspStats
                ORDER BY AuthRate30d DESC";

            using var command = new SqlCommand(query, connection);
            command.CommandTimeout = _settings.QueryTimeoutSeconds;

            using var reader = command.ExecuteReader();

            while (reader.Read())
            {
                var pspId = reader.GetInt64(0).ToString();
                var name = reader.GetString(1);
                var totalTransactions = reader.GetInt32(2);
                var successfulTransactions = reader.GetInt32(3);
                var authRate = Convert.ToDouble(reader.GetValue(4));
                var avgProcessingTime = reader.IsDBNull(5) ? 2000.0 : Convert.ToDouble(reader.GetValue(5));
                var supportsTokenization = reader.GetInt32(6) == 1;
                var supports3DS = reader.GetInt32(7) == 1;
                var healthStatus = reader.GetString(8);

                // Calculate default fees based on PSP performance (simplified)
                var feeBps = authRate >= 0.9 ? 25 : authRate >= 0.8 ? 30 : 35;
                var fixedFee = authRate >= 0.9 ? 0.30m : authRate >= 0.8 ? 0.25m : 0.20m;

                var candidate = new PspCandidate(
                    Name: name,
                    Supports: true, // All PSPs in the query are considered active
                    Health: healthStatus,
                    AuthRate30d: authRate,
                    FeeBps: feeBps,
                    FixedFee: fixedFee,
                    Supports3DS: supports3DS,
                    Tokenization: supportsTokenization,
                    PerformanceByCountry: new Dictionary<string, double>(),
                    PerformanceByPaymentMethod: new Dictionary<string, double>()
                );

                candidates[name] = candidate;
            }

            _logger.LogInformation("Successfully loaded {Count} PSP candidates from PaymentTransactions data", candidates.Count);
            return candidates;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load PSP candidates from database");
            throw;
        }
    }

    private string GetConnectionString()
    {
        // Get .env file variable
        var baseConnectionString = Environment.GetEnvironmentVariable("PSPROUTER_DB_CONNECTION")
            ?? throw new InvalidOperationException("PSPROUTER_DB_CONNECTION environment variable is required");

        // Ensure TrustServerCertificate=true is included to handle SSL certificate issues
        if (_settings.TrustServerCertificate && !baseConnectionString.Contains("TrustServerCertificate", StringComparison.OrdinalIgnoreCase))
        {
            return baseConnectionString.TrimEnd(';') + ";TrustServerCertificate=true;";
        }

        return baseConnectionString;
    }

    private long GetPaymentMethodIdFromFeedback(TransactionFeedback feedback)
    {
        // TODO: Implement this

        // In a real implementation, this would be extracted from the feedback
        // For now, we'll use a default value or extract from transaction context
        return 1; // Default to card payment method
    }
}

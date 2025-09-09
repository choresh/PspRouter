using Microsoft.Extensions.Logging;

namespace PspRouter.Lib;

/// <summary>
/// ML-based PSP performance prediction service
/// </summary>
public interface IPspPerformancePredictor
{
    /// <summary>
    /// Predict success probability for a PSP given transaction context
    /// </summary>
    Task<double> PredictSuccessRate(string pspName, RouteInput transaction, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Predict processing time for a PSP given transaction context
    /// </summary>
    Task<int> PredictProcessingTime(string pspName, RouteInput transaction, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Predict PSP health status based on recent performance patterns
    /// </summary>
    Task<string> PredictHealthStatus(string pspName, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Update models with new feedback data
    /// </summary>
    Task UpdateWithFeedback(TransactionFeedback feedback, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Check if models need retraining
    /// </summary>
    bool ShouldRetrain();
    
    /// <summary>
    /// Perform incremental model retraining
    /// </summary>
    Task RetrainIncremental(CancellationToken cancellationToken = default);
}

/// <summary>
/// ML-based PSP performance prediction service implementation
/// </summary>
public class PspPerformancePredictor : IPspPerformancePredictor
{
    private readonly ILogger<PspPerformancePredictor> _logger;
    private readonly PspCandidateSettings _settings;
    private readonly Dictionary<string, PspPerformanceModel> _models;
    private readonly object _lock = new object();
    
    public PspPerformancePredictor(ILogger<PspPerformancePredictor> logger, PspCandidateSettings settings)
    {
        _logger = logger;
        _settings = settings;
        _models = new Dictionary<string, PspPerformanceModel>();
    }
    
    public Task<double> PredictSuccessRate(string pspName, RouteInput transaction, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_models.TryGetValue(pspName, out var model))
            {
                _logger.LogWarning("No performance model found for PSP {PspName}, using default rate", pspName);
                return Task.FromResult(0.85); // Default success rate
            }
            
            return Task.FromResult(model.PredictSuccessRate(transaction));
        }
    }
    
    public Task<int> PredictProcessingTime(string pspName, RouteInput transaction, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_models.TryGetValue(pspName, out var model))
            {
                _logger.LogWarning("No performance model found for PSP {PspName}, using default processing time", pspName);
                return Task.FromResult(2000); // Default 2 seconds
            }
            
            return Task.FromResult(model.PredictProcessingTime(transaction));
        }
    }
    
    public Task<string> PredictHealthStatus(string pspName, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_models.TryGetValue(pspName, out var model))
            {
                _logger.LogWarning("No performance model found for PSP {PspName}, using default health", pspName);
                return Task.FromResult("yellow"); // Default health
            }
            
            return Task.FromResult(model.PredictHealthStatus());
        }
    }
    
    public Task UpdateWithFeedback(TransactionFeedback feedback, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            if (!_models.TryGetValue(feedback.PspName, out var model))
            {
                // Create new model for this PSP
                model = new PspPerformanceModel(feedback.PspName);
                _models[feedback.PspName] = model;
            }
            
            model.UpdateWithFeedback(feedback);
            _logger.LogDebug("Updated performance model for PSP {PspName}", feedback.PspName);
        }
        
        return Task.CompletedTask;
    }
    
    public bool ShouldRetrain()
    {
        lock (_lock)
        {
            // Retrain if any model has accumulated enough new data
            return _models.Values.Any(m => m.ShouldRetrain());
        }
    }
    
    public Task RetrainIncremental(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var modelsToRetrain = _models.Values.Where(m => m.ShouldRetrain()).ToList();
            
            foreach (var model in modelsToRetrain)
            {
                try
                {
                    model.RetrainIncremental();
                    _logger.LogInformation("Retrained performance model for PSP {PspName}", model.PspName);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to retrain model for PSP {PspName}", model.PspName);
                }
            }
        }
        
        return Task.CompletedTask;
    }
}

/// <summary>
/// Individual PSP performance model
/// </summary>
public class PspPerformanceModel
{
    public string PspName { get; }
    private readonly List<TransactionFeedback> _recentFeedback;
    private readonly object _lock = new object();
    private double _cachedSuccessRate = 0.85;
    private int _cachedProcessingTime = 2000;
    private string _cachedHealthStatus = "green";
    private DateTime _lastRetrain = DateTime.UtcNow;
    
    public PspPerformanceModel(string pspName)
    {
        PspName = pspName;
        _recentFeedback = new List<TransactionFeedback>();
    }
    
    public double PredictSuccessRate(RouteInput transaction)
    {
        lock (_lock)
        {
            // Simple heuristic-based prediction for now
            // In a real implementation, this would use a trained ML model
            
            var baseRate = _cachedSuccessRate;
            
            // Adjust based on transaction characteristics
            if (transaction.RiskScore > 50) baseRate *= 0.9;
            if (transaction.Amount > 1000) baseRate *= 0.95;
            if (transaction.SCARequired) baseRate *= 0.98;
            
            return Math.Max(0.1, Math.Min(1.0, baseRate));
        }
    }
    
    public int PredictProcessingTime(RouteInput transaction)
    {
        lock (_lock)
        {
            // Simple heuristic-based prediction for now
            var baseTime = _cachedProcessingTime;
            
            // Adjust based on transaction characteristics
            if (transaction.RiskScore > 50) baseTime += 500;
            if (transaction.Amount > 1000) baseTime += 300;
            if (transaction.SCARequired) baseTime += 1000;
            
            return Math.Max(500, baseTime);
        }
    }
    
    public string PredictHealthStatus()
    {
        lock (_lock)
        {
            return _cachedHealthStatus;
        }
    }
    
    public void UpdateWithFeedback(TransactionFeedback feedback)
    {
        lock (_lock)
        {
            _recentFeedback.Add(feedback);
            
            // Keep only recent feedback (last 1000 transactions)
            if (_recentFeedback.Count > 1000)
            {
                _recentFeedback.RemoveAt(0);
            }
            
            // Update cached values
            UpdateCachedValues();
        }
    }
    
    public bool ShouldRetrain()
    {
        lock (_lock)
        {
            // Retrain if we have enough new data or if it's been too long
            return _recentFeedback.Count >= 100 || 
                   DateTime.UtcNow - _lastRetrain > TimeSpan.FromHours(1);
        }
    }
    
    public void RetrainIncremental()
    {
        lock (_lock)
        {
            // Simple incremental update for now
            // In a real implementation, this would retrain the ML model
            
            UpdateCachedValues();
            _lastRetrain = DateTime.UtcNow;
        }
    }
    
    private void UpdateCachedValues()
    {
        if (_recentFeedback.Count == 0) return;
        
        // Calculate success rate
        var successful = _recentFeedback.Count(f => f.Authorized);
        _cachedSuccessRate = (double)successful / _recentFeedback.Count;
        
        // Calculate average processing time
        _cachedProcessingTime = (int)_recentFeedback.Average(f => f.ProcessingTimeMs);
        
        // Determine health status
        if (_cachedSuccessRate >= 0.9 && _cachedProcessingTime <= 2000)
            _cachedHealthStatus = "green";
        else if (_cachedSuccessRate >= 0.8 && _cachedProcessingTime <= 3000)
            _cachedHealthStatus = "yellow";
        else
            _cachedHealthStatus = "red";
    }
}

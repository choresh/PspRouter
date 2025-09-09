using Microsoft.Extensions.Logging;

namespace PspRouter.Lib;

/// <summary>
/// Service for managing PSP candidates with performance tracking and learning
/// </summary>
public interface IPspCandidateProvider
{
    Task<IReadOnlyList<PspSnapshot>> GetCandidatesAsync(RouteInput transaction, CancellationToken cancellationToken = default);
    Task ProcessFeedbackAsync(TransactionFeedback feedback, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PspCandidate>> GetAllCandidatesAsync(CancellationToken cancellationToken = default);
    Task<PspCandidate?> GetCandidateAsync(string pspName, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory PSP candidate provider with performance tracking
/// </summary>
public class PspCandidateProvider : IPspCandidateProvider
{
    private readonly ILogger<PspCandidateProvider> _logger;
    private readonly Dictionary<string, PspCandidate> _candidates;
    private readonly object _lock = new object();

    public PspCandidateProvider(ILogger<PspCandidateProvider> logger)
    {
        _logger = logger;
        _candidates = InitializeDefaultCandidates();
    }

    public Task<IReadOnlyList<PspSnapshot>> GetCandidatesAsync(RouteInput transaction, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            var allowedHealth = new[] { "green", "yellow" };
            
            var candidates = _candidates.Values
                .Where(c => c.Supports)
                .Where(c => allowedHealth.Contains(c.Health, StringComparer.OrdinalIgnoreCase))
                .Where(c => !(transaction.SCARequired && transaction.PaymentMethodId == 1 && !c.Supports3DS)) // PaymentMethodId 1 = Card
                .Select(c => new PspSnapshot(
                    c.Name,
                    c.Supports,
                    c.Health,
                    c.CurrentAuthRate, // Use current performance-based auth rate
                    c.FeeBps,
                    c.FixedFee,
                    c.Supports3DS,
                    c.Tokenization
                ))
                .ToList();

            _logger.LogInformation("Retrieved {Count} valid PSP candidates for transaction {MerchantId}", 
                candidates.Count, transaction.MerchantId);

            return Task.FromResult<IReadOnlyList<PspSnapshot>>(candidates);
        }
    }

    public Task ProcessFeedbackAsync(TransactionFeedback feedback, CancellationToken cancellationToken = default)
    {
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

        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<PspCandidate>> GetAllCandidatesAsync(CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            return Task.FromResult<IReadOnlyList<PspCandidate>>(_candidates.Values.ToList());
        }
    }

    public Task<PspCandidate?> GetCandidateAsync(string pspName, CancellationToken cancellationToken = default)
    {
        lock (_lock)
        {
            _candidates.TryGetValue(pspName, out var candidate);
            return Task.FromResult(candidate);
        }
    }

    private Dictionary<string, PspCandidate> InitializeDefaultCandidates()
    {
        var candidates = new Dictionary<string, PspCandidate>
        {
            ["PSP_A"] = new PspCandidate(
                Name: "PSP_A",
                Supports: true,
                Health: "green",
                AuthRate30d: 0.92,
                FeeBps: 100,
                FixedFee: 0.10m,
                Supports3DS: true,
                Tokenization: true,
                PerformanceByCountry: new Dictionary<string, double>(),
                PerformanceByPaymentMethod: new Dictionary<string, double>()
            ),
            ["PSP_B"] = new PspCandidate(
                Name: "PSP_B",
                Supports: true,
                Health: "yellow",
                AuthRate30d: 0.88,
                FeeBps: 120,
                FixedFee: 0.15m,
                Supports3DS: false,
                Tokenization: true,
                PerformanceByCountry: new Dictionary<string, double>(),
                PerformanceByPaymentMethod: new Dictionary<string, double>()
            ),
            ["PSP_C"] = new PspCandidate(
                Name: "PSP_C",
                Supports: true,
                Health: "green",
                AuthRate30d: 0.95,
                FeeBps: 80,
                FixedFee: 0.05m,
                Supports3DS: true,
                Tokenization: false,
                PerformanceByCountry: new Dictionary<string, double>(),
                PerformanceByPaymentMethod: new Dictionary<string, double>()
            ),
            ["PSP_D"] = new PspCandidate(
                Name: "PSP_D",
                Supports: true,
                Health: "red",
                AuthRate30d: 0.70,
                FeeBps: 200,
                FixedFee: 0.20m,
                Supports3DS: false,
                Tokenization: false,
                PerformanceByCountry: new Dictionary<string, double>(),
                PerformanceByPaymentMethod: new Dictionary<string, double>()
            ),
            ["PSP_E"] = new PspCandidate(
                Name: "PSP_E",
                Supports: true,
                Health: "green",
                AuthRate30d: 0.90,
                FeeBps: 90,
                FixedFee: 0.08m,
                Supports3DS: true,
                Tokenization: true,
                PerformanceByCountry: new Dictionary<string, double>(),
                PerformanceByPaymentMethod: new Dictionary<string, double>()
            )
        };

        _logger.LogInformation("Initialized {Count} default PSP candidates", candidates.Count);
        return candidates;
    }

    private long GetPaymentMethodIdFromFeedback(TransactionFeedback feedback)
    {
        // In a real implementation, this would be extracted from the feedback
        // For now, we'll use a default value or extract from transaction context
        return 1; // Default to card payment method
    }
}

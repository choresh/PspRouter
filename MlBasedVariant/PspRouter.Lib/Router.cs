using Microsoft.Extensions.Logging;

namespace PspRouter.Lib;

/// <summary>
/// ML-based PSP router that uses trained models for routing decisions
/// </summary>
public sealed class Router
{
    private readonly IPredictionService _predictionService;
    private readonly IPspCandidateProvider _candidateProvider;
    private readonly ILogger<Router>? _logger;
    private readonly RoutingSettings _settings;

    public Router(
        IPredictionService predictionService,
        IPspCandidateProvider candidateProvider,
        ILogger<Router>? logger = null, 
        RoutingSettings? settings = null)
    {
        _predictionService = predictionService;
        _candidateProvider = candidateProvider;
        _logger = logger;
        _settings = settings ?? RoutingSettings.Default;
    }

    public async Task<RouteDecision> Decide(RouteInput transaction, CancellationToken ct)
    {
        _logger?.LogInformation("Starting ML-based PSP routing decision for merchant {MerchantId}, amount {Amount} CurrencyId {CurrencyId}", 
            transaction.MerchantId, transaction.Amount, transaction.CurrencyId);

        // Get candidates from the provider
        var candidates = await _candidateProvider.GetCandidatesAsync(transaction, ct);
        
        if (candidates.Count == 0)
        {
            _logger?.LogWarning("No valid PSPs found for transaction {MerchantId}", transaction.MerchantId);
            return Fail("veto:no_valid_psp", "No valid PSP");
        }

        var ctx = new RouteContext(transaction, candidates);

        // Try ML-based routing
        try
        {
            var mlDecision = await DecideWithML(ctx, candidates.ToList(), ct);
            if (mlDecision != null)
            {
                _logger?.LogInformation("ML routing successful: {Candidate}", mlDecision.Candidate);
                return mlDecision;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ML routing failed, falling back to deterministic scoring");
        }

        // Fallback to deterministic scoring
        _logger?.LogInformation("Using deterministic fallback routing");
        return ScoreDeterministically(transaction, candidates.ToList());
    }

    private static RouteDecision Fail(string guardrail, string why) =>
        new("1.0", Guid.NewGuid().ToString(), "NONE", Array.Empty<string>(), why, guardrail,
            new RouteConstraints(false, 0, 0), Array.Empty<string>());

    private async Task<RouteDecision?> DecideWithML(RouteContext ctx, List<PspSnapshot> validCandidates, CancellationToken ct)
    {
        if (!_predictionService.IsModelLoaded)
        {
            _logger?.LogWarning("ML model not loaded, skipping ML-based routing");
            return null;
        }

        // Create a new context with only valid candidates
        var mlContext = new RouteContext(ctx.Tx, validCandidates);
        
        var prediction = await _predictionService.PredictBestPspAsync(mlContext, ct);
        if (prediction == null)
        {
            _logger?.LogWarning("ML prediction failed");
            return null;
        }

        // Find the recommended PSP in our valid candidates
        var recommendedPsp = validCandidates.FirstOrDefault(c => c.Name == prediction.RecommendedPsp);
        if (recommendedPsp == null)
        {
            _logger?.LogWarning("ML recommended PSP {RecommendedPsp} not found in valid candidates", prediction.RecommendedPsp);
            return null;
        }

        // Create decision with ML prediction details
        var features = prediction.AllPredictions
            .Select(p => $"ml:{p.PspName}={p.SuccessProbability:F3}")
            .ToArray();

        return new RouteDecision(
            "1.0",
            Guid.NewGuid().ToString(),
            Candidate: recommendedPsp.Name,
            Alternates: validCandidates.Where(c => c.Name != recommendedPsp.Name).Select(c => c.Name).ToArray(),
            Reasoning: $"ML prediction - Success probability: {prediction.SuccessProbability:P2}, Auth: {recommendedPsp.AuthRate30d:P2}, Fee: {recommendedPsp.FeeBps}bps + {recommendedPsp.FixedFee:C}",
            Guardrail: "none",
            Constraints: new RouteConstraints(
                Must_Use_3ds: ctx.Tx.SCARequired && ctx.Tx.PaymentMethodId == 1,
                Retry_Window_Ms: 8000,
                Max_Retries: 1),
            Features_Used: features.Concat(new[] { 
                $"ml_model=v1.0", 
                $"ml_confidence={prediction.SuccessProbability:F3}",
                $"ml_timestamp={prediction.PredictionTimestamp:yyyy-MM-ddTHH:mm:ssZ}"
            }).ToArray()
        );
    }


    private RouteDecision ScoreDeterministically(RouteInput tx, List<PspSnapshot> candidates)
    {
        // Fallback to deterministic scoring with configurable weights
        var w = _settings.Weights;
        var bias = w.BusinessBias;
        var biasWeight = w.BusinessBiasWeight;
        double Score(PspSnapshot c)
        {
            var baseScore =
                w.AuthWeight * c.AuthRate30d
                - w.FeeBpsWeight * (c.FeeBps / 10000.0)
                - w.FixedFeeWeight * (double)(c.FixedFee / Math.Max(tx.Amount, 1m))
                + biasWeight * (bias.TryGetValue(c.Name, out var b) ? b : 0.0);

            // Bonus for 3DS support when SCA is required
            if (tx.SCARequired && tx.PaymentMethodId == 1 && c.Supports3DS)
            {
                baseScore += w.Supports3DSBonusWhenSCARequired;
            }

            // Penalty for yellow health (red is already filtered out)
            if (string.Equals(c.Health, "yellow", StringComparison.OrdinalIgnoreCase))
            {
                baseScore -= w.HealthYellowPenalty;
            }

            // Risk penalty per risk point (0-100)
            baseScore -= w.RiskScorePenaltyPerPoint * tx.RiskScore / 100.0;

            return baseScore;
        }

        var best = candidates.OrderByDescending(Score).First();

        return CreateDecision(best, candidates, tx, "Deterministic scoring", "deterministic");
    }

    private static RouteDecision CreateDecision(PspSnapshot chosen, List<PspSnapshot> allCandidates, RouteInput tx, string method, string segmentKey)
    {
        return new RouteDecision(
            "1.0",
            Guid.NewGuid().ToString(),
            Candidate: chosen.Name,
            Alternates: allCandidates.Where(c => c.Name != chosen.Name).Select(c => c.Name).ToArray(),
            Reasoning: $"{method} - Auth: {chosen.AuthRate30d:P2}, Fee: {chosen.FeeBps}bps + {chosen.FixedFee:C}",
            Guardrail: "none",
            Constraints: new RouteConstraints(
                Must_Use_3ds: tx.SCARequired && tx.PaymentMethodId == 1, // PaymentMethodId 1 = Card
                Retry_Window_Ms: 8000,
                Max_Retries: 1),
            Features_Used: new[] { 
                $"auth={chosen.AuthRate30d:F2}", 
                $"fee_bps={chosen.FeeBps}", 
                $"method={method}",
                $"segment={segmentKey}"
            }
        );
    }
}

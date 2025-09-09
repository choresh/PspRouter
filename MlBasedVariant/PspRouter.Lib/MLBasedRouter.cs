using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PspRouter.Lib;

/// <summary>
/// ML-based PSP router that uses trained models for routing decisions with LLM fallback
/// </summary>
public sealed class MLBasedRouter
{
    private readonly IMLPredictionService _mlPredictionService;
    private readonly IChatClient _chat;
    private readonly ILogger<MLBasedRouter>? _logger;
    private readonly RoutingSettings _settings;

    public MLBasedRouter(
        IMLPredictionService mlPredictionService,
        IChatClient chat, 
        ILogger<MLBasedRouter>? logger = null, 
        RoutingSettings? settings = null)
    {
        _mlPredictionService = mlPredictionService;
        _chat = chat;
        _logger = logger;
        _settings = settings ?? RoutingSettings.Default;
    }

    public async Task<RouteDecision> Decide(RouteContext ctx, CancellationToken ct)
    {
        _logger?.LogInformation("Starting ML-based PSP routing decision for merchant {MerchantId}, amount {Amount} CurrencyId {CurrencyId}", 
            ctx.Tx.MerchantId, ctx.Tx.Amount, ctx.Tx.CurrencyId);

        var allowedHealth = _settings.AllowedHealthStatuses;
        var valid = ctx.Candidates
            .Where(c => c.Supports)
            .Where(c => allowedHealth.Contains(c.Health, StringComparer.OrdinalIgnoreCase))
            .Where(c => !(ctx.Tx.SCARequired && ctx.Tx.PaymentMethodId == 1 && !c.Supports3DS)) // PaymentMethodId 1 = Card
            .ToList();

        if (valid.Count == 0)
        {
            _logger?.LogWarning("No valid PSPs found for transaction {MerchantId}", ctx.Tx.MerchantId);
            return Fail("veto:no_valid_psp", "No valid PSP");
        }

        // Try ML-based routing first
        try
        {
            var mlDecision = await DecideWithML(ctx, valid, ct);
            if (mlDecision != null)
            {
                _logger?.LogInformation("ML routing successful: {Candidate}", mlDecision.Candidate);
                return mlDecision;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ML routing failed, falling back to LLM");
        }

        // Fallback to LLM-based routing
        try
        {
            var llmDecision = await DecideWithLLM(ctx, valid, ct);
            if (llmDecision != null)
            {
                _logger?.LogInformation("LLM fallback routing successful: {Candidate}", llmDecision.Candidate);
                return llmDecision;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "LLM routing failed, falling back to deterministic scoring");
        }

        // Final fallback to deterministic scoring
        _logger?.LogInformation("Using deterministic fallback routing");
        return ScoreDeterministically(ctx.Tx, valid);
    }

    private static RouteDecision Fail(string guardrail, string why) =>
        new("1.0", Guid.NewGuid().ToString(), "NONE", Array.Empty<string>(), why, guardrail,
            new RouteConstraints(false, 0, 0), Array.Empty<string>());

    private async Task<RouteDecision?> DecideWithML(RouteContext ctx, List<PspSnapshot> validCandidates, CancellationToken ct)
    {
        if (!_mlPredictionService.IsModelLoaded)
        {
            _logger?.LogWarning("ML model not loaded, skipping ML-based routing");
            return null;
        }

        // Create a new context with only valid candidates
        var mlContext = new RouteContext(ctx.Tx, validCandidates);
        
        var prediction = await _mlPredictionService.PredictBestPspAsync(mlContext, ct);
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

    private async Task<RouteDecision?> DecideWithLLM(RouteContext ctx, List<PspSnapshot> validCandidates, CancellationToken ct)
    {
        // Build context for LLM
        var contextJson = JsonSerializer.Serialize(new {
            Transaction = ctx.Tx,
            Candidates = validCandidates,
            Weights = _settings.Weights
        }, new JsonSerializerOptions { WriteIndented = true });

        var systemPrompt = BuildSystemPrompt();
        var userInstruction = $"Route this payment transaction to the optimal PSP. Consider auth rates, fees, and compliance requirements.";

        var response = await _chat.CompleteJson(systemPrompt, userInstruction, contextJson, 0.1, ct);
        
        try
        {
            var decision = JsonSerializer.Deserialize<RouteDecision>(response);
            if (decision != null && validCandidates.Any(c => c.Name == decision.Candidate))
            {
                return decision;
            }
        }
        catch (JsonException ex)
        {
            _logger?.LogError(ex, "Failed to parse LLM response as RouteDecision: {Response}", response);
        }

        return null;
    }

    private static string BuildSystemPrompt()
    {
        return """
        You are an expert payment service provider (PSP) routing system. Your job is to select the optimal PSP for each transaction to maximize authorization success, minimize fees, and ensure compliance.

        CRITICAL RULES:
        1. NEVER route to a PSP that doesn't support the payment method
        2. ALWAYS enforce SCA/3DS requirements when specified
        3. Consider authorization rates, fees, and compliance requirements
        4. Use learned patterns from training to inform decisions
        5. Provide clear reasoning for your choice

        DECISION FACTORS (in order of importance):
        1. Compliance (SCA/3DS requirements)
        2. Authorization success rates (weighted by Weights.AuthWeight)
        3. Fee optimization (variable fees weighted by Weights.FeeBpsWeight, fixed by Weights.FixedFeeWeight)
        4. Business bias (Weights.BusinessBiasWeight × Weights.BusinessBias[psp] if provided)
        5. Health (penalize yellow by Weights.HealthYellowPenalty; red is already excluded)
        6. Risk (apply Weights.RiskScorePenaltyPerPoint × risk_score/100)
        7. 3DS (apply Weights.Supports3DSBonusWhenSCARequired bonus when SCA is required and PSP supports 3DS)

        RESPONSE FORMAT:
        Return a JSON object with this exact structure:
        {
            "Schema_Version": "1.0",
            "Decision_Id": "unique-id",
            "Candidate": "PSP_NAME",
            "Alternates": ["PSP1", "PSP2"],
            "Reasoning": "Clear explanation of why this PSP was chosen",
            "Guardrail": "none|compliance|health|capability",
            "Constraints": {
                "Must_Use_3ds": true/false,
                "Retry_Window_Ms": 8000,
                "Max_Retries": 1
            },
            "Features_Used": ["auth=0.89", "fee_bps=200", "compliance=3ds"]
        }
        """;
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

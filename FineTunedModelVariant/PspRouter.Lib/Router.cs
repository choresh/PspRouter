using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace PspRouter.Lib;

public sealed class PspRouter
{
    private readonly IChatClient _chat;
    private readonly IEnumerable<IAgentTool> _tools;
    private readonly IHealthProvider _health;
    private readonly IFeeQuoteProvider _fees;
    
    private readonly ILogger<PspRouter>? _logger;

    public PspRouter(IChatClient chat, IHealthProvider health, IFeeQuoteProvider fees,
                     IEnumerable<IAgentTool>? tools = null, ILogger<PspRouter>? logger = null)
    {
        _chat = chat; _health = health; _fees = fees; _tools = tools ?? Array.Empty<IAgentTool>();
        _logger = logger;
    }

    public async Task<RouteDecision> DecideAsync(RouteContext ctx, CancellationToken ct)
    {
        _logger?.LogInformation("Starting PSP routing decision for merchant {MerchantId}, amount {Amount} {Currency}", 
            ctx.Tx.MerchantId, ctx.Tx.Amount, ctx.Tx.Currency);

        var valid = ctx.Candidates
            .Where(c => c.Supports)
            .Where(c => c.Health is "green" or "yellow")
            .Where(c => !(ctx.Tx.SCARequired && ctx.Tx.Method == PaymentMethod.Card && !c.Supports3DS))
            .ToList();

        if (valid.Count == 0)
        {
            _logger?.LogWarning("No valid PSPs found for transaction {MerchantId}", ctx.Tx.MerchantId);
            return Fail("veto:no_valid_psp", "No valid PSP");
        }

        // Try LLM-based routing first
        try
        {
            var llmDecision = await DecideWithLLMAsync(ctx, valid, ct);
            if (llmDecision != null)
            {
                _logger?.LogInformation("LLM routing successful: {Candidate}", llmDecision.Candidate);
                return llmDecision;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "LLM routing failed, falling back to deterministic scoring");
        }

        // Fallback to deterministic scoring
        _logger?.LogInformation("Using deterministic fallback routing");
        return ScoreDeterministically(ctx.Tx, valid);
    }

    private static RouteDecision Fail(string guardrail, string why) =>
        new("1.0", Guid.NewGuid().ToString(), "NONE", Array.Empty<string>(), why, guardrail,
            new RouteConstraints(false, 0, 0), Array.Empty<string>());

    private async Task<RouteDecision?> DecideWithLLMAsync(RouteContext ctx, List<PspSnapshot> validCandidates, CancellationToken ct)
    {
        // Retrieve relevant lessons from memory
        var relevantLessons = await GetRelevantLessonsAsync(ctx, ct);
        
        // Build context for LLM
        var contextJson = JsonSerializer.Serialize(new {
            Transaction = ctx.Tx,
            Candidates = validCandidates,
            MerchantPreferences = ctx.MerchantPrefs,
            SegmentStats = ctx.SegmentStats,
            RelevantLessons = relevantLessons
        }, new JsonSerializerOptions { WriteIndented = true });

        var systemPrompt = BuildSystemPrompt();
        var userInstruction = $"Route this payment transaction to the optimal PSP. Consider auth rates, fees, compliance requirements, and historical lessons.";

        var response = await _chat.CompleteJsonAsync(systemPrompt, userInstruction, contextJson, _tools, 0.1, ct);
        
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

    private async Task<List<string>> GetRelevantLessonsAsync(RouteContext ctx, CancellationToken ct)
    {
        try
        {
            // Create a query based on transaction context
            var query = $"PSP routing for {ctx.Tx.Currency} {ctx.Tx.Method} {ctx.Tx.Scheme} merchant {ctx.Tx.MerchantCountry}";
            
            // Note: In a full implementation, you would:
            // 1. Use OpenAIEmbeddings to embed the query
            // 2. Search the vector memory with the embedding
            // 3. Return the top-k relevant lessons
            
            // For now, return empty list as embedding integration requires additional setup
            _logger?.LogDebug("Memory search requires embedding integration - returning empty lessons");
            
            // Add a small delay to make this truly async
            await Task.Delay(1, ct);
            return new List<string>();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to retrieve relevant lessons from memory");
            return new List<string>();
        }
    }

    private static string BuildSystemPrompt()
    {
        return """
        You are an expert payment service provider (PSP) routing system. Your job is to select the optimal PSP for each transaction to maximize authorization success, minimize fees, and ensure compliance.

        CRITICAL RULES:
        1. NEVER route to a PSP that doesn't support the payment method
        2. ALWAYS enforce SCA/3DS requirements when specified
        3. Consider authorization rates, fees, and merchant preferences
        4. Use historical lessons to inform decisions
        5. Provide clear reasoning for your choice

        DECISION FACTORS (in order of importance):
        1. Compliance (SCA/3DS requirements)
        2. Authorization success rates
        3. Fee optimization
        4. Merchant preferences
        5. Historical performance

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
            "Features_Used": ["auth=0.89", "fee_bps=200", "preference=low_fees"]
        }
        """;
    }

    private RouteDecision ScoreDeterministically(RouteInput tx, List<PspSnapshot> candidates)
    {
        // Fallback to deterministic scoring
        var best = candidates.OrderByDescending(c =>
            c.AuthRate30d - (c.FeeBps/10000.0) - (double)(c.FixedFee/Math.Max(tx.Amount,1m))
        ).First();

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
                Must_Use_3ds: tx.SCARequired && tx.Method == PaymentMethod.Card,
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

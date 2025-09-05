using System.Text.Json;
using System.Linq;
using System.Threading;

namespace PspRouter;

public sealed class PspRouter
{
    private readonly IChatClient _chat;
    private readonly IEnumerable<IAgentTool> _tools;
    private readonly IHealthProvider _health;
    private readonly IFeeQuoteProvider _fees;

    public PspRouter(IChatClient chat, IHealthProvider health, IFeeQuoteProvider fees,
                     IEnumerable<IAgentTool>? tools = null)
    {
        _chat = chat; _health = health; _fees = fees; _tools = tools ?? Array.Empty<IAgentTool>();
    }

    public async Task<RouteDecision> DecideAsync(RouteContext ctx, CancellationToken ct)
    {
        var valid = ctx.Candidates
            .Where(c => c.Supports)
            .Where(c => c.Health is "green" or "yellow")
            .Where(c => !(ctx.Tx.SCARequired && ctx.Tx.Method == PaymentMethod.Card && !c.Supports3DS))
            .ToList();

        if (valid.Count == 0)
            return Fail("veto:no_valid_psp", "No valid PSP");

        // Example: Deterministic fallback scoring
        return ScoreDeterministically(ctx.Tx, valid);
    }

    private static RouteDecision Fail(string guardrail, string why) =>
        new("1.0", Guid.NewGuid().ToString(), "NONE", Array.Empty<string>(), why, guardrail,
            new RouteConstraints(false, 0, 0), Array.Empty<string>());

    private static RouteDecision ScoreDeterministically(RouteInput tx, List<PspSnapshot> candidates)
    {
        var best = candidates.OrderByDescending(c =>
            c.AuthRate30d - (c.FeeBps/10000.0) - (double)(c.FixedFee/Math.Max(tx.Amount,1m))
        ).First();

        return new RouteDecision(
            "1.0",
            Guid.NewGuid().ToString(),
            Candidate: best.Name,
            Alternates: candidates.Where(c => c.Name != best.Name).Select(c => c.Name).ToArray(),
            Reasoning: "Deterministic fallback",
            Guardrail: "none",
            Constraints: new RouteConstraints(
                Must_Use_3ds: tx.SCARequired && tx.Method == PaymentMethod.Card,
                Retry_Window_Ms: 8000,
                Max_Retries: 1),
            Features_Used: new[] { $"auth={best.AuthRate30d}", $"fee_bps={best.FeeBps}" }
        );
    }
}

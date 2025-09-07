using System.Text.Json;

namespace PspRouter.Lib;

public sealed class GetHealthTool : IAgentTool
{
    private readonly IHealthProvider _health;
    public GetHealthTool(IHealthProvider health) => _health = health;

    public string Name => "get_psp_health";

    public string JsonSchema => @"{""type"":""object"",""properties"":{""psp"":{""type"":""string""}},""required"":[""psp""]}";

    public async Task<string> InvokeAsync(string jsonArgs, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(jsonArgs);
        var psp = doc.RootElement.GetProperty("psp").GetString() ?? "";
        var (h, lat) = await _health.GetAsync(psp, ct);
        var payload = new { psp = psp, health = h, latency_ms = lat };
        return JsonSerializer.Serialize(payload);
    }
}

public sealed class GetFeeQuoteTool : IAgentTool
{
    private readonly IFeeQuoteProvider _fees;
    private readonly Func<RouteInput> _txAccessor;
    public GetFeeQuoteTool(IFeeQuoteProvider fees, Func<RouteInput> txAccessor)
    {
        _fees = fees; _txAccessor = txAccessor;
    }

    public string Name => "get_fee_quote";

    public string JsonSchema => @"{""type"":""object"",""properties"":{""psp"":{""type"":""string""}},""required"":[""psp""]}";

    public async Task<string> InvokeAsync(string jsonArgs, CancellationToken ct)
    {
        using var doc = JsonDocument.Parse(jsonArgs);
        var psp = doc.RootElement.GetProperty("psp").GetString() ?? "";
        var (bps, fixedFee) = await _fees.GetAsync(psp, _txAccessor(), ct);
        var payload = new { psp = psp, fee_bps = bps, fixed_fee = fixedFee };
        return JsonSerializer.Serialize(payload);
    }
}

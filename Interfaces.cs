namespace PspRouter;

public interface IHealthProvider
{
    Task<(string health, int latencyMs)> GetAsync(string psp, CancellationToken ct);
}

public interface IFeeQuoteProvider
{
    Task<(int feeBps, decimal fixedFee)> GetAsync(string psp, RouteInput tx, CancellationToken ct);
}

public interface IChatClient
{
    Task<string> CompleteJsonAsync(string systemPrompt, string userInstruction, string contextJson,
        IEnumerable<IAgentTool> tools, double temperature, CancellationToken ct);
}

public interface IAgentTool
{
    string Name { get; }
    string JsonSchema { get; }
    Task<string> InvokeAsync(string jsonArgs, CancellationToken ct);
}

namespace PspRouter.Lib;

public interface IHealthProvider
{
    Task<(string health, int latencyMs)> Get(string psp, CancellationToken ct);
}

public interface IFeeQuoteProvider
{
    Task<(int feeBps, decimal fixedFee)> Get(string psp, RouteInput tx, CancellationToken ct);
}

public interface IChatClient
{
    Task<string> CompleteJson(string systemPrompt, string userInstruction, string contextJson,
        IEnumerable<IAgentTool> tools, double temperature, CancellationToken ct);
}

public interface IAgentTool
{
    string Name { get; }
    string JsonSchema { get; }
    Task<string> Invoke(string jsonArgs, CancellationToken ct);
}

public interface ICapabilityProvider
{
    bool Supports(string pspName, RouteInput tx);
}
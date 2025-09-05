namespace PspRouter;

public class DummyHealthProvider : IHealthProvider
{
    public Task<(string health, int latencyMs)> GetAsync(string psp, CancellationToken ct)
    {
        return Task.FromResult(("green", 100));
    }
}

public class DummyFeeProvider : IFeeQuoteProvider
{
    public Task<(int feeBps, decimal fixedFee)> GetAsync(string psp, RouteInput tx, CancellationToken ct)
    {
        return Task.FromResult((200, 0.30m));
    }
}

public class DummyChatClient : IChatClient
{
    public Task<string> CompleteJsonAsync(string systemPrompt, string userInstruction, string contextJson,
        IEnumerable<IAgentTool> tools, double temperature, CancellationToken ct)
    {
        // Always returns Adyen as dummy choice
        return Task.FromResult("{\"Schema_Version\":\"1.0\",\"Decision_Id\":\"dummy\",\"Candidate\":\"Adyen\",\"Alternates\":[],\"Reasoning\":\"Dummy decision\",\"Guardrail\":\"none\",\"Constraints\":{\"Must_Use_3ds\":false,\"Retry_Window_Ms\":8000,\"Max_Retries\":1},\"Features_Used\":[]}");
    }
}

using PspRouter.Lib;

namespace PspRouter.API;

public class DummyHealthProvider : IHealthProvider
{
    public Task<(string health, int latencyMs)> Get(string psp, CancellationToken ct)
    {
        return Task.FromResult(("green", 100));
    }
}

public class DummyFeeProvider : IFeeQuoteProvider
{
    public Task<(int feeBps, decimal fixedFee)> Get(string psp, RouteInput tx, CancellationToken ct)
    {
        return Task.FromResult((200, 0.30m));
    }
}

public class DummyChatClient : IChatClient
{
    public Task<string> CompleteJson(string systemPrompt, string userInstruction, string contextJson,
        IEnumerable<IAgentTool> tools, double temperature, CancellationToken ct)
    {
        // Always returns Adyen as dummy choice
        return Task.FromResult("{\"Schema_Version\":\"1.0\",\"Decision_Id\":\"dummy\",\"Candidate\":\"Adyen\",\"Alternates\":[],\"Reasoning\":\"Dummy decision\",\"Guardrail\":\"none\",\"Constraints\":{\"Must_Use_3ds\":false,\"Retry_Window_Ms\":8000,\"Max_Retries\":1},\"Features_Used\":[]}");
    }
}

public sealed class DummyCapabilityProvider : ICapabilityProvider
{
    public bool Supports(string psp, RouteInput tx) => tx.PaymentMethodId switch
    {
        1 => psp is "Adyen" or "Stripe",        // PaymentMethodId 1 = Card
        2 => psp is "PayPal",                   // PaymentMethodId 2 = PayPal
        3 => psp is "Klarna",                   // PaymentMethodId 3 = KlarnaPayLater
        _ => false
    };
}

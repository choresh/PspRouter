using PspRouter.Lib;

namespace PspRouter.API;

public class DummyChatClient : IChatClient
{
    public Task<string> CompleteJson(string systemPrompt, string userInstruction, string contextJson,
        double temperature, CancellationToken ct)
    {
        // Always returns Adyen as dummy choice
        return Task.FromResult("{\"Schema_Version\":\"1.0\",\"Decision_Id\":\"dummy\",\"Candidate\":\"Adyen\",\"Alternates\":[],\"Reasoning\":\"Dummy decision\",\"Guardrail\":\"none\",\"Constraints\":{\"Must_Use_3ds\":false,\"Retry_Window_Ms\":8000,\"Max_Retries\":1},\"Features_Used\":[]}");
    }
}
namespace PspRouter.Lib;

public interface IChatClient
{
    Task<string> CompleteJson(string systemPrompt, string userInstruction, string contextJson,
        double temperature, CancellationToken ct);
}
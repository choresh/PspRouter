using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PspRouter.Lib;

public sealed class OpenAIChatClient : IChatClient
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OpenAIChatClient(string apiKey, string model = "gpt-4.1")
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        _http.BaseAddress = new Uri("https://api.openai.com/v1/");
        _model = model;
    }

    public async Task<string> CompleteJson(string systemPrompt, string userInstruction, string contextJson,
        double temperature, CancellationToken ct)
    {
        var messages = new List<object> {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userInstruction },
            new { role = "system", content = contextJson }
        };

        // Tools removed - fine-tuned model learns everything from historical data

        while (true)
        {
            var payload = new
            {
                model = _model,
                temperature,
                messages,
                response_format = new { type = "json_object" }
            };

            using var req = new HttpRequestMessage(HttpMethod.Post, "chat/completions")
            {
                Content = JsonContent.Create(payload, options: new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                })
            };

            using var resp = await _http.SendAsync(req, ct);
            resp.EnsureSuccessStatusCode();
            using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);

            var choice = doc.RootElement.GetProperty("choices")[0];
            var message = choice.GetProperty("message");

            // Tool calling logic removed - fine-tuned model doesn't need tools

            var content = message.GetProperty("content").GetString() ?? "{}";
            return content;
        }
    }
}

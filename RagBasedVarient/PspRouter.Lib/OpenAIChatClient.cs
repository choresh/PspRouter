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

    public async Task<string> CompleteJsonAsync(string systemPrompt, string userInstruction, string contextJson,
        IEnumerable<IAgentTool> tools, double temperature, CancellationToken ct)
    {
        var messages = new List<object> {
            new { role = "system", content = systemPrompt },
            new { role = "user", content = userInstruction },
            new { role = "system", content = contextJson }
        };

        var toolDefs = tools.Select(t => new {
            type = "function",
            function = new {
                name = t.Name,
                description = "tool",
                parameters = JsonDocument.Parse(t.JsonSchema).RootElement
            }
        }).ToList();

        while (true)
        {
            var payload = new
            {
                model = _model,
                temperature,
                messages,
                response_format = new { type = "json_object" },
                tools = toolDefs.Count == 0 ? null : toolDefs,
                tool_choice = toolDefs.Count == 0 ? null : "auto"
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

            if (message.TryGetProperty("tool_calls", out var toolCalls) && toolCalls.ValueKind == JsonValueKind.Array && toolCalls.GetArrayLength() > 0)
            {
                foreach (var call in toolCalls.EnumerateArray())
                {
                    var id = call.GetProperty("id").GetString() ?? "";
                    var name = call.GetProperty("function").GetProperty("name").GetString() ?? "";
                    var argsJson = call.GetProperty("function").GetProperty("arguments").GetString() ?? "{}";

                    var tool = tools.FirstOrDefault(t => t.Name == name);
                    string toolResult = "{\"ok\":false,\"error\":\"unknown tool\"}";
                    if (tool is not null)
                    {
                        toolResult = await tool.InvokeAsync(argsJson, ct);
                    }

                    messages.Add(new { role = "tool", content = toolResult, tool_call_id = id });
                }
                // continue the loop to let the model observe tool results
                continue;
            }

            var content = message.GetProperty("content").GetString() ?? "{}";
            return content;
        }
    }
}

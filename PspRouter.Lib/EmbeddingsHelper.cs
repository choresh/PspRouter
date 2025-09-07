using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PspRouter.Lib;

public sealed class OpenAIEmbeddings
{
    private readonly HttpClient _http;
    private readonly string _model;

    public OpenAIEmbeddings(string apiKey, string model = "text-embedding-3-large")
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
        _http.BaseAddress = new Uri("https://api.openai.com/v1/");
        _model = model;
    }

    public async Task<float[]> EmbedAsync(string text, CancellationToken ct)
    {
        var payload = new { model = _model, input = text };
        using var req = new HttpRequestMessage(HttpMethod.Post, "embeddings")
        {
            Content = JsonContent.Create(payload)
        };

        using var resp = await _http.SendAsync(req, ct);
        resp.EnsureSuccessStatusCode();

        using var doc = await JsonDocument.ParseAsync(await resp.Content.ReadAsStreamAsync(ct), cancellationToken: ct);
        var embArr = doc.RootElement.GetProperty("data")[0].GetProperty("embedding");
        var list = new List<float>(embArr.GetArrayLength());
        foreach (var v in embArr.EnumerateArray()) list.Add((float)v.GetDouble());
        return list.ToArray();
    }
}

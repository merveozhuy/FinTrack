using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using FinTrack.Application.Common.Interfaces;
using FinTrack.Domain.Common;

namespace FinTrack.Infrastructure.Ai;

/// <summary>OpenAI embeddings, used when configured with an API key. Off by default.</summary>
public class OpenAiEmbeddingProvider : IEmbeddingProvider
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AiOptions _options;

    public OpenAiEmbeddingProvider(IHttpClientFactory httpClientFactory, AiOptions options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public int Dimensions => VectorConstants.EmbeddingDimensions;

    public async Task<float[]> EmbedAsync(string text, CancellationToken cancellationToken)
    {
        using var client = CreateClient(_httpClientFactory, _options);
        var response = await client.PostAsJsonAsync(
            "embeddings", new { model = _options.EmbeddingModel, input = text }, cancellationToken);
        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<EmbeddingResponse>(cancellationToken);
        return payload!.Data[0].Embedding;
    }

    internal static HttpClient CreateClient(IHttpClientFactory factory, AiOptions options)
    {
        var client = factory.CreateClient();
        client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/') + "/");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", options.ApiKey);
        return client;
    }

    private sealed record EmbeddingResponse(List<EmbeddingData> Data);

    private sealed record EmbeddingData(float[] Embedding);
}

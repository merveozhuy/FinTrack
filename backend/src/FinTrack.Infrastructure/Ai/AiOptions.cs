namespace FinTrack.Infrastructure.Ai;

public class AiOptions
{
    public string Provider { get; set; } = "Fake";
    public string ApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = "gpt-4o-mini";
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";
}

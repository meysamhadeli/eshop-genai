namespace BuildingBlocks.AI.SemanticSearch;

public class SemanticSearchOptions
{
    public bool Enabled { get; set; }
    public string Provider { get; set; } = "Ollama"; // Ollama, OpenAI, AzureOpenAI
    public string VectorDbConnectionString { get; set; }
    public string DefaultCollectionPrefix { get; set; } = "semantic_";
    public string EmbeddingModel { get; set; }
    public string ChatModel { get; set; }
    public string DeploymentName { get; set; }
    public string ApiVersion { get; set; }
    public string ApiKey { get; set; } = string.Empty;
    public string EmbeddingBaseUrl { get; set; } = "http://localhost:11434";
    public string ChatBaseUrl { get; set; } = "http://localhost:11434";
    public int MaxResults { get; set; } = 10;
    public double SimilarityThreshold { get; set; } = 0.7;
    public int VectorSize { get; set; } = 768;
    public int? EmbeddingDimensions { get; set; }
}
namespace BuildingBlocks.AI;

public class AIOptions
{
    public string Provider { get; set; } = "Ollama";
    public string VectorDbConnectionString { get; set; } = "http://localhost:6334";
    public string DefaultCollectionPrefix { get; set; } = "semantic_";

    // Embedding settings
    public string EmbeddingModel { get; set; } = "nomic-embed-text";
    public string EmbeddingBaseUrl { get; set; } = "http://localhost:11434";
    public int? EmbeddingDimensions { get; set; }

    // Chat/completion settings
    public string ChatModel { get; set; } = "qwen3:0.6b";
    public string ChatBaseUrl { get; set; } = "http://localhost:11434";

    // Azure/OpenAI specific
    public string ApiKey { get; set; } = string.Empty;
    public string DeploymentName { get; set; } = string.Empty;
    public string ApiVersion { get; set; } = "2024-02-01";

    // Semantic search settings
    public bool SemanticSearchEnabled { get; set; } = true;
    public bool SearchExplanationEnabled { get; set; }
    public int MaxResults { get; set; } = 10;
    public double SimilarityThreshold { get; set; } = 0.7;
    public int VectorSize { get; set; } = 768;

    // Recommendation settings
    public bool RecommendationEnabled { get; set; } = true;
    public bool RecommendationExplanationEnabled { get; set; }

}
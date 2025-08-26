using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace BuildingBlocks.AI.SemanticSearch.Providers;

public class OllamaStrategy : IAIProviders
{
    private readonly SemanticSearchOptions _options;

    public OllamaStrategy(SemanticSearchOptions options)
    {
        _options = options;
    }

    public ITextEmbeddingGenerationService CreateEmbeddingProvider()
    {
        var kernel = Kernel.CreateBuilder()
            .AddOllamaTextEmbeddingGeneration(
                modelId: _options.EmbeddingModel,
                endpoint: new Uri(_options.EmbeddingBaseUrl))
            .Build();

        return kernel.GetRequiredService<ITextEmbeddingGenerationService>();
    }

    public IChatCompletionService CreateChatProvider()
    {
        var kernel = Kernel.CreateBuilder()
            .AddOllamaChatCompletion(
                modelId: _options.ChatModel,
                endpoint: new Uri(_options.ChatBaseUrl))
            .Build();

        return kernel.GetRequiredService<IChatCompletionService>();
    }
}
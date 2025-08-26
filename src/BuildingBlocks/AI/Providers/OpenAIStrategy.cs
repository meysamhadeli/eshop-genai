using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace BuildingBlocks.AI.SemanticSearch.Providers;

public class OpenAIStrategy: IAIProviders
{
    private readonly SemanticSearchOptions _options;

    public OpenAIStrategy(SemanticSearchOptions options)
    {
        _options = options;
    }

    public ITextEmbeddingGenerationService CreateEmbeddingProvider()
    {
        var kernel = Kernel.CreateBuilder()
            .AddOpenAITextEmbeddingGeneration(
                modelId: _options.EmbeddingModel,
                apiKey: _options.ApiKey,
                dimensions: _options.EmbeddingDimensions)
            .Build();

        return kernel.GetRequiredService<ITextEmbeddingGenerationService>();
    }
    
    public IChatCompletionService CreateChatProvider()
    {
        var kernel = Kernel.CreateBuilder()
            .AddOpenAIChatCompletion(
                endpoint: new Uri(_options.ChatBaseUrl),
                modelId: _options.ChatModel,
                apiKey: _options.ApiKey)
            .Build();

        return kernel.GetRequiredService<IChatCompletionService>();
    }
}
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace BuildingBlocks.AI.SemanticSearch.Providers;

public class OpenAIStrategy: IEmbeddingProvider
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
}
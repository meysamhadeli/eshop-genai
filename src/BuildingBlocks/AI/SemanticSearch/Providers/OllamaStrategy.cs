using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace BuildingBlocks.AI.SemanticSearch.Providers;

public class OllamaStrategy: IEmbeddingProvider
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
                endpoint: new Uri(_options.BaseUrl))
            .Build();

        return kernel.GetRequiredService<ITextEmbeddingGenerationService>();
    }
}
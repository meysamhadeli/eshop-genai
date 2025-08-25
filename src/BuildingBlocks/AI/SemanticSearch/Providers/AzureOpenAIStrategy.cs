using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Embeddings;

namespace BuildingBlocks.AI.SemanticSearch.Providers;

public class AzureOpenAIStrategy: IEmbeddingProvider
{
    private readonly SemanticSearchOptions _options;

    public AzureOpenAIStrategy(SemanticSearchOptions options)
    {
        _options = options;
    }

    public ITextEmbeddingGenerationService CreateEmbeddingProvider()
    {
        var kernel = Kernel.CreateBuilder()
            .AddAzureOpenAITextEmbeddingGeneration(
                deploymentName: _options.DeploymentName,
                endpoint: _options.BaseUrl,
                apiKey: _options.ApiKey,
                apiVersion: _options.ApiVersion,
                dimensions: _options.EmbeddingDimensions)
            .Build();

        return kernel.GetRequiredService<ITextEmbeddingGenerationService>();
    }
}
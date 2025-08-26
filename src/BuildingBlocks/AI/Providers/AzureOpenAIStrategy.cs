using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace BuildingBlocks.AI.SemanticSearch.Providers;

public class AzureOpenAIStrategy: IAIProviders
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
                endpoint: _options.ChatBaseUrl,
                apiKey: _options.ApiKey,
                modelId: _options.EmbeddingModel,
                apiVersion: _options.ApiVersion,
                dimensions: _options.EmbeddingDimensions)
            .Build();

        return kernel.GetRequiredService<ITextEmbeddingGenerationService>();
    }
    
    public IChatCompletionService CreateChatProvider()
    {
        var kernel = Kernel.CreateBuilder()
            .AddAzureOpenAIChatCompletion(
                deploymentName: _options.DeploymentName,
                endpoint: _options.ChatBaseUrl,
                modelId: _options.ChatModel,
                apiKey: _options.ApiKey,
                apiVersion: _options.ApiVersion)
            .Build();

        return kernel.GetRequiredService<IChatCompletionService>();
    }
}
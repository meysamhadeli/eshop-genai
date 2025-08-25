using System.Globalization;
using BuildingBlocks.AI.SemanticSearch.Providers;
using Microsoft.SemanticKernel.Embeddings;

namespace BuildingBlocks.AI.SemanticSearch;

public static class EmbeddingProviderFactory
{
    public static ITextEmbeddingGenerationService Register(SemanticSearchOptions options)
    {
        return options.Provider.ToLower(CultureInfo.CurrentCulture) switch
               {
                   "ollama" => new OllamaStrategy(options).CreateEmbeddingProvider(),
                   "openai" => new OpenAIStrategy(options).CreateEmbeddingProvider(),
                   "azureopenai" => new AzureOpenAIStrategy(options).CreateEmbeddingProvider(),
                   _ => throw new InvalidOperationException($"Unsupported embedding provider: {options.Provider}"),
               };
    }
}
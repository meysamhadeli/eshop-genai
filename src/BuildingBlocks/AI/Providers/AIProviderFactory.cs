using System.Globalization;
using BuildingBlocks.AI.SemanticSearch.Providers;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace BuildingBlocks.AI.SemanticSearch;

public static class AIProviderFactory
{
    public static ITextEmbeddingGenerationService RegisterEmbeddingProviders(SemanticSearchOptions options)
    {
        return options.Provider.ToLower(CultureInfo.CurrentCulture) switch
               {
                   "ollama" => new OllamaStrategy(options).CreateEmbeddingProvider(),
                   "openai" => new OpenAIStrategy(options).CreateEmbeddingProvider(),
                   "azureopenai" => new AzureOpenAIStrategy(options).CreateEmbeddingProvider(),
                   _ => throw new InvalidOperationException($"Unsupported embedding provider: {options.Provider}"),
               };
    }
    
    public static IChatCompletionService RegisterChatProviders(SemanticSearchOptions options)
    {
        return options.Provider.ToLower(CultureInfo.CurrentCulture) switch
               {
                   "ollama" => new OllamaStrategy(options).CreateChatProvider(),
                   "openai" => new OpenAIStrategy(options).CreateChatProvider(),
                   "azureopenai" => new AzureOpenAIStrategy(options).CreateChatProvider(),
                   _ => throw new InvalidOperationException($"Unsupported embedding provider: {options.Provider}"),
               };
    }
}
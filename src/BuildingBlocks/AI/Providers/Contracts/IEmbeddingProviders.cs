using Microsoft.SemanticKernel.Embeddings;

namespace BuildingBlocks.AI.SemanticSearch.Providers;

public interface IEmbeddingProviders
{
    ITextEmbeddingGenerationService CreateEmbeddingProvider();
}
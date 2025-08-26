using Microsoft.SemanticKernel.ChatCompletion;

namespace BuildingBlocks.AI.SemanticSearch.Providers;

public interface IChatProviders
{
    IChatCompletionService CreateChatProvider();
}
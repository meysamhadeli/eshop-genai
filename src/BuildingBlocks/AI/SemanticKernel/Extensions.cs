using System.Globalization;
using BuildingBlocks.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Embeddings;

namespace BuildingBlocks.AI;

public static class Extensions
{
    public static IServiceCollection AddSemanticKernel(this IServiceCollection services)
    {
        services.AddValidateOptions<AIOptions>();

        var options = services.GetOptions<AIOptions>(nameof(AIOptions));

        services.AddSingleton<ITextEmbeddingGenerationService>(sp => CreateEmbeddingService(options));

        services.AddSingleton<IChatCompletionService>(sp => CreateChatService(options));

        return services;
    }

    private static ITextEmbeddingGenerationService CreateEmbeddingService(AIOptions options)
    {
        var kernelBuilder = Kernel.CreateBuilder();

        return options.Provider.ToLower(CultureInfo.CurrentCulture) switch
        {
            "ollama" => kernelBuilder
                .AddOllamaTextEmbeddingGeneration(
                    modelId: options.EmbeddingModel,
                    endpoint: new Uri(options.EmbeddingBaseUrl))
                .Build()
                .GetRequiredService<ITextEmbeddingGenerationService>(),

            "openai" => kernelBuilder
                .AddOpenAITextEmbeddingGeneration(
                    modelId: options.EmbeddingModel,
                    apiKey: options.ApiKey,
                    dimensions: options.EmbeddingDimensions)
                .Build()
                .GetRequiredService<ITextEmbeddingGenerationService>(),

            "azureopenai" => kernelBuilder
                .AddAzureOpenAITextEmbeddingGeneration(
                    deploymentName: options.DeploymentName,
                    endpoint: options.ChatBaseUrl,
                    apiKey: options.ApiKey,
                    modelId: options.EmbeddingModel,
                    apiVersion: options.ApiVersion,
                    dimensions: options.EmbeddingDimensions)
                .Build()
                .GetRequiredService<ITextEmbeddingGenerationService>(),

            _ => throw new InvalidOperationException($"Unsupported provider: {options.Provider}")
        };
    }

    private static IChatCompletionService CreateChatService(AIOptions options)
    {
        var kernelBuilder = Kernel.CreateBuilder();

        return options.Provider.ToLower(CultureInfo.CurrentCulture) switch
        {
            "ollama" => kernelBuilder
                .AddOllamaChatCompletion(
                    modelId: options.ChatModel,
                    endpoint: new Uri(options.ChatBaseUrl))
                .Build()
                .GetRequiredService<IChatCompletionService>(),

            "openai" => kernelBuilder
                .AddOpenAIChatCompletion(
                    modelId: options.ChatModel,
                    apiKey: options.ApiKey)
                .Build()
                .GetRequiredService<IChatCompletionService>(),

            "azureopenai" => kernelBuilder
                .AddAzureOpenAIChatCompletion(
                    deploymentName: options.DeploymentName,
                    endpoint: options.ChatBaseUrl,
                    modelId: options.ChatModel,
                    apiKey: options.ApiKey,
                    apiVersion: options.ApiVersion)
                .Build()
                .GetRequiredService<IChatCompletionService>(),

            _ => throw new InvalidOperationException($"Unsupported provider: {options.Provider}")
        };
    }
}
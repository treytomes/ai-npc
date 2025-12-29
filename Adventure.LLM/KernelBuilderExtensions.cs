using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Adventure.LLM;

public static class KernelBuilderExtensions
{
	public static IKernelBuilder AddChatClientChatCompletion(
		this IKernelBuilder builder,
		IChatClient chatClient,
		string modelId,
		string? serviceId = null
	)
	{
		builder.Services.AddKeyedSingleton<IChatCompletionService>(
			serviceId,
			new ChatClientCompletionService(chatClient, modelId)
		);

		return builder;
	}

	public static IKernelBuilder AddChatClientChatCompletion(
		this IKernelBuilder builder,
		Func<IServiceProvider, IChatClient> chatClientFactory,
		string modelId,
		string? serviceId = null
	)
	{
		builder.Services.AddKeyedSingleton<IChatCompletionService>(
			serviceId,
			(serviceProvider, _) =>
			{
				var chatClient = chatClientFactory(serviceProvider);
				return new ChatClientCompletionService(chatClient, modelId);
			}
		);

		return builder;
	}
}
using System.Runtime.InteropServices;
using Adventure.LLM.Ollama;
using Adventure.LLM.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Adventure.LLM;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddLLM(this IServiceCollection services)
	{
		services.AddHostedService<OllamaLifetimeHook>();

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
		{
			services.AddSingleton<OllamaInstaller, LinuxOllamaInstaller>();
		}
		else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			services.AddSingleton<OllamaInstaller, WindowsOllamaInstaller>();
		}
		else
		{
			throw new ApplicationException($"Unsupported OS: {RuntimeInformation.OSDescription}");
		}

		services.AddSingleton<OllamaProcess>();
		services.AddSingleton<OllamaProcessManager>();
		services.AddSingleton<ILlmManager, OllamaLlmManager>();

		// Holds the initialized client
		services.AddSingleton<ChatClientHolder>();

		// Async initializer
		services.AddHostedService<ChatClientInitializer>();

		// Kernel
		services.AddSingleton<Kernel>(sp =>
		{
			var props = sp.GetRequiredService<OllamaProps>();
			var holder = sp.GetRequiredService<ChatClientHolder>();

			if (holder.Client is null)
			{
				throw new InvalidOperationException("Chat client has not been initialized yet.");
			}

			var builder = Kernel.CreateBuilder();

			builder.Services.AddSingleton<IChatCompletionService>(
				new ChatClientCompletionService(holder.Client, props.ModelId));

			builder.Services.AddLogging();

			return builder.Build();
		});

		return services;
	}
}

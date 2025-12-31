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

		services.AddSingleton<IChatClient>(sp =>
		{
			var llmManager = sp.GetRequiredService<ILlmManager>();
			// llmManager.InitializeAsync().GetAwaiter().GetResult();
			return llmManager.CreateChatClient().GetAwaiter().GetResult();
		});

		services.AddSingleton<Kernel>(sp =>
		{
			var llmManager = sp.GetRequiredService<ILlmManager>();
			// llmManager.InitializeAsync().GetAwaiter().GetResult();

			var props = sp.GetRequiredService<OllamaProps>();

			var builder = Kernel.CreateBuilder();
			builder.Services.AddTransient<IChatCompletionService>(_ =>
				new ChatClientCompletionService(llmManager.CreateChatClient().GetAwaiter().GetResult(), props.ModelId));

			// // Add renderer chat client
			// builder.AddChatClientChatCompletion(
			// 	_ => llmManager.CreateChatClient().GetAwaiter().GetResult(),
			// 	"renderer"
			// );

			// // Add validator chat client
			// builder.AddChatClientChatCompletion(
			// 	_ => llmManager.CreateChatClient().GetAwaiter().GetResult(),
			// 	"validator"
			// );

			// Add any other services you need.
			builder.Services.AddLogging();

			return builder.Build();
		});

		return services;
	}
}

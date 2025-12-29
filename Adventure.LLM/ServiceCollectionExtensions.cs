using Adventure.LLM.Ollama;
using Adventure.LLM.Services;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;

namespace Adventure.LLM;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddLLM(this IServiceCollection services)
	{
		services.AddHostedService<OllamaLifetimeHook>();

		services.AddSingleton<OllamaInstaller>();
		services.AddSingleton<OllamaProcess>();
		services.AddSingleton<OllamaProcessManager>();
		services.AddSingleton<ILLMManager, OllamaLLMManager>();

		services.AddSingleton<IChatClient>(sp =>
		{
			var repo = sp.GetRequiredService<ILLMManager>();
			return repo.CreateChatClient().GetAwaiter().GetResult();
		});

		return services;
	}
}

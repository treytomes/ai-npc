using Adventure.LLM.OllamaRuntime;
using Adventure.LLM.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Adventure.LLM;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddLLM(this IServiceCollection services)
	{
		services.AddHostedService<OllamaLifetimeHook>();

		services.AddSingleton<OllamaInstaller>();
		services.AddSingleton<OllamaProcess>();
		services.AddSingleton<OllamaManager>();
		services.AddSingleton<OllamaRepo>();
		return services;
	}
}

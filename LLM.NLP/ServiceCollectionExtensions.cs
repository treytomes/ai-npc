using LLM.NLP.Services;
using Microsoft.Extensions.DependencyInjection;

namespace LLM.NLP;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddNlpRuntime(
		this IServiceCollection services,
		Action<NlpRuntimeOptions>? configure = null)
	{
		var options = new NlpRuntimeOptions();
		configure?.Invoke(options);

		services.AddSingleton(options);
		services.AddSingleton<INlpRuntime, NlpRuntime>();
		services.AddSingleton<INlpParser, CatalystNlpParser>();
		services.AddSingleton<IIntentSeedExtractor, CatalystIntentSeedExtractor>();
		services.AddSingleton<INounPhraseExtractor, PosBasedNounPhraseExtractor>();

		return services;
	}
}

using Adventure.NLP.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Adventure.NLP;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddNlpRuntime(this IServiceCollection services)
	{
		services.AddSingleton<INlpRuntime, CatalystNlpRuntime>();
		services.AddSingleton<INlpParser, CatalystNlpParser>();
		services.AddSingleton<IIntentSeedExtractor, CatalystIntentSeedExtractor>();
		services.AddSingleton<INounPhraseExtractor, PosBasedNounPhraseExtractor>();
		return services;
	}
}

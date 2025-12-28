using Adventure.NLP.REPL.SystemIntent;
using Microsoft.Extensions.DependencyInjection;

namespace Adventure.NLP.REPL;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddREPL(this IServiceCollection services)
	{
		services.AddTransient<ISystemIntentEvaluator, SystemIntentEvaluator>();
		return services;
	}
}

using Adventure.LLM.Ollama;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Adventure.LLM.REPL;

internal class Bootstrap : Adventure.Bootstrap
{
	protected override void ConfigureServices<TAppSettings, TAppEngine, TMainState>(HostBuilderContext hostContext, IServiceCollection services)
	{
		base.ConfigureServices<TAppSettings, TAppEngine, TMainState>(hostContext, services);
		services.AddSingleton(provider =>
		{
			var settings = provider.GetRequiredService<IOptions<AppSettings>>();
			return new OllamaProps(settings.Value.OllamaUrl);
		});
		services.AddLLM();
	}
}
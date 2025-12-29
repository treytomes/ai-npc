using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Adventure.NLP.REPL;

internal class Bootstrap : Adventure.Bootstrap
{
	protected override void ConfigureServices<TAppSettings, TAppEngine, TMainState>(HostBuilderContext hostContext, IServiceCollection services)
	{
		base.ConfigureServices<TAppSettings, TAppEngine, TMainState>(hostContext, services);

		services.AddNlpRuntime();
		services.AddREPL();
	}
}
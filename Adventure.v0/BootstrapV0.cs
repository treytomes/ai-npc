using Adventure.Gpu.Services;
using Adventure.LLM;
using Adventure.NLP;
using Adventure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Reflection;

namespace Adventure;

internal class BootstrapV0 : Bootstrap
{
	protected override void ConfigureServices<TAppSettings, TAppEngine, TMainState>(HostBuilderContext hostContext, IServiceCollection services)
	{
		base.ConfigureServices<TAppSettings, TAppEngine, TMainState>(hostContext, services);

		services.AddSingleton(provider =>
		{
			var settings = provider.GetRequiredService<IOptions<AppSettings>>();
			return new OllamaRepoProps(settings.Value.OllamaUrl);
		});

		services.AddSingleton<IProcessService, ProcessService>();
		services.AddSingleton<IGpuVendorFactory, GpuVendorFactory>();
		services.AddSingleton<IGpuDetectorService, GpuDetectorService>();
		services.AddLLM();
		services.AddNlpRuntime();
	}
}
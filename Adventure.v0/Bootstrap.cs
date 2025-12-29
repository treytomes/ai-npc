using Adventure.Gpu.Services;
using Adventure.NLP;
using Adventure.OllamaRuntime;
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

		services.AddHostedService<OllamaLifetimeHook>();

		services.AddSingleton<IProcessService, ProcessService>();
		services.AddSingleton<IGpuVendorFactory, GpuVendorFactory>();
		services.AddSingleton<IGpuDetectorService, GpuDetectorService>();
		services.AddSingleton<OllamaInstaller>();
		services.AddSingleton<OllamaProcess>();
		services.AddSingleton<OllamaManager>();
		services.AddSingleton<OllamaRepo>();

		services.AddNlpRuntime();
	}
}
using Adventure.LLM.Ollama;
using Adventure.LLM.REPL.Persistence;
using Adventure.LLM.REPL.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
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
			return new OllamaProps(settings.Value.OllamaUrl, settings.Value.ModelId);
		});

		services.AddSingleton<IRoomRepository>(provider =>
		{
			var settings = provider.GetRequiredService<IOptions<AppSettings>>();
			var logger = provider.GetRequiredService<ILogger<YamlRoomRepository>>();
			var roomsPath = Path.Combine(settings.Value.AssetsPath, settings.Value.RoomAssetsPath);
			return new YamlRoomRepository(logger, roomsPath);
		});

		services.AddSingleton<IRoomNavigationService>(provider =>
		{
			var settings = provider.GetRequiredService<IOptions<AppSettings>>();
			var logger = provider.GetRequiredService<ILogger<RoomNavigationService>>();
			var repository = provider.GetRequiredService<IRoomRepository>();
			return new RoomNavigationService(logger, repository, settings.Value.InitialRoomName);
		});

		services.AddLLM();
	}
}
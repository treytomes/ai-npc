using AINPC.Gpu.Services;
using AINPC.OllamaRuntime;
using AINPC.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.CommandLine;
using System.Reflection;

namespace AINPC;

static class Bootstrap
{
	public static async Task<int> Start<TAppSettings, TAppEngine, TMainState>(string[] args)
		where TAppSettings : AppSettings
		where TAppEngine : AppEngine
		where TMainState : AppState
	{
		// Define command-line options.
		var configFileOption = new Option<string>(
			name: "--config",
			description: "Path to the configuration file",
			getDefaultValue: () => "appsettings.json");

		var debugOption = new Option<bool>(
			name: "--debug",
			description: "Enable debug mode");

		var fullscreenOption = new Option<bool>(
			name: "--fullscreen",
			description: "Start in fullscreen mode");

		var widthOption = new Option<int?>(
			name: "--width",
			description: "Window width in pixels");

		var heightOption = new Option<int?>(
			name: "--height",
			description: "Window height in pixels");

		// Create root command.
		var rootCommand = new RootCommand("AI NPC Example");
		rootCommand.AddOption(configFileOption);
		rootCommand.AddOption(debugOption);
		rootCommand.AddOption(fullscreenOption);
		rootCommand.AddOption(widthOption);
		rootCommand.AddOption(heightOption);

		// Set handler for processing the command.
		rootCommand.SetHandler(static async (configFile, debug, fullscreen, width, height) =>
			{
				await RunGameAsync<TAppSettings, TAppEngine, TMainState>(new()
				{
					ConfigFile = configFile,
					Debug = debug,
					Fullscreen = fullscreen,
					Width = width,
					Height = height,
				});
			},
			configFileOption, debugOption, fullscreenOption, widthOption, heightOption);

		// Parse the command line.
		return await rootCommand.InvokeAsync(args);
	}

	static async Task RunGameAsync<TAppSettings, TAppEngine, TMainState>(CommandLineProps props)
		where TAppSettings : AppSettings
		where TAppEngine : AppEngine
		where TMainState : AppState
	{
		try
		{
			// Build host with DI container.
			using var host = CreateHostBuilder<TAppSettings, TAppEngine, TMainState>(props).Build();

			// Start the app.
			var engine = host.Services.GetRequiredService<IAppEngine>();
			await engine.RunAsync<TMainState>();
		}
		catch (Exception ex)
		{
			Console.Error.WriteLine($"Error starting the app: {ex.Message}");
			Environment.Exit(1);
		}
	}

	static IHostBuilder CreateHostBuilder<TAppSettings, TAppEngine, TMainState>(CommandLineProps props)
		where TAppSettings : AppSettings
		where TAppEngine : AppEngine
		where TMainState : AppState
	{
		return Host.CreateDefaultBuilder()
			.ConfigureAppConfiguration((hostContext, config) => ConfigureAppConfiguration(config, props))
			.ConfigureLogging(ConfigureLogging)
			.ConfigureServices(ConfigureServices<TAppSettings, TAppEngine, TMainState>);
	}

	private static void ConfigureAppConfiguration(IConfigurationBuilder config, CommandLineProps props)
	{
		config.Sources.Clear();
		// config.SetBasePath(Directory.GetCurrentDirectory());
		config.SetBasePath(AppContext.BaseDirectory);
		config.AddJsonFile(props.ConfigFile, optional: false, reloadOnChange: false);

		// Add command line overrides.
		var commandLineConfig = new Dictionary<string, string?>();
		if (props.Debug)
		{
			commandLineConfig["Debug"] = "true";
		}
		if (props.Fullscreen)
		{
			commandLineConfig["Window:Fullscreen"] = "true";
		}
		if (props.Width.HasValue)
		{
			commandLineConfig["Window:Width"] = props.Width.ToString();
		}
		if (props.Height.HasValue)
		{
			commandLineConfig["Window:Height"] = props.Height.ToString();
		}

		config.AddInMemoryCollection(commandLineConfig);
	}

	private static void ConfigureLogging(HostBuilderContext hostContext, ILoggingBuilder logging)
	{
		logging.ClearProviders();

		// Disable console logging.
		// logging.AddConsole();

		// Set minimum log level based on debug setting.
		var debugEnabled = hostContext.Configuration.GetValue<bool>("Debug");
		var minLevel = debugEnabled ? LogLevel.Debug : LogLevel.Information;
		logging.SetMinimumLevel(minLevel);

		// Create log directory.
		var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
		Directory.CreateDirectory(logDir);

		// Timestamped log file (daily rotation).
		var logFile = Path.Combine(logDir, $"app-{DateTime.Now:yyyy-MM-dd}.log");

		// Add file logger.
		logging.AddProvider(new Logging.FileLoggerProvider(logFile, minLevel));
	}

	private static void ConfigureServices<TAppSettings, TAppEngine, TMainState>(HostBuilderContext hostContext, IServiceCollection services)
		where TAppEngine : AppEngine
		where TMainState : AppState
	{
		// Register configuration.
		services.Configure<AppSettings>(hostContext.Configuration);

		services.AddHostedService<OllamaLifetimeHook>();

		services.AddSingleton<IProcessService, ProcessService>();
		services.AddSingleton<IGpuVendorFactory, GpuVendorFactory>();
		services.AddSingleton<IGpuDetectorService, GpuDetectorService>();
		services.AddSingleton<HttpClient>();
		services.AddSingleton<OllamaInstaller>();
		services.AddSingleton<OllamaProcess>();
		services.AddSingleton<OllamaManager>();
		services.AddSingleton<OllamaRepo>();
		services.AddSingleton<TAppEngine>();
		services.AddTransient<IAppEngine>(sp => sp.GetRequiredService<OllamaAppEngine>());
		services.AddTransient<IStateManager>(sp => sp.GetRequiredService<TAppEngine>());

		// Register game states.
		var stateTypes = Assembly.GetExecutingAssembly().GetTypes()
			.Where(x => !x.IsAbstract)
			.Where(x => x.IsAssignableTo(typeof(AppState)));

		// Console.WriteLine("BEGIN REGISTERING STATES");
		foreach (var stateType in stateTypes)
		{
			// Console.WriteLine($"Registering {stateType.Name}...");
			services.AddTransient(stateType);
		}
		// Console.WriteLine("DONE");
	}
}
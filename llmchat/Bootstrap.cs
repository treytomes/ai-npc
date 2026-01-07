using Adventure.LLM;
using Adventure.LLM.Ollama;
using llmchat.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Net.Http;

namespace llmchat;

public sealed class Bootstrap
{
	public static IHost Host { get; private set; } = null!;

	public static int Start(string[] args)
	{
		var configFileOption = new Option<string>(
			name: "--config",
			getDefaultValue: () => "appsettings.json",
			description: "Path to configuration file");

		var debugOption = new Option<bool>(
			name: "--debug",
			description: "Enable debug logging");

		var rootCommand = new RootCommand("llmchat");
		rootCommand.AddOption(configFileOption);
		rootCommand.AddOption(debugOption);

		rootCommand.SetHandler(async (configFile, debug) =>
		{
			Host = CreateHost(configFile, debug);
			await Host.StartAsync();
		}, configFileOption, debugOption);

		return rootCommand.Invoke(args);
	}

	private static IHost CreateHost(string configFile, bool debug)
	{
		return Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
			.ConfigureAppConfiguration(config =>
			{
				config.Sources.Clear();
				config.SetBasePath(AppContext.BaseDirectory);
				config.AddJsonFile(configFile, optional: false);

				if (debug)
				{
					config.AddInMemoryCollection(new Dictionary<string, string?>
					{
						["Debug"] = "true"
					});
				}
			})
			.ConfigureLogging((context, logging) =>
			{
				logging.ClearProviders();

				var debugEnabled = context.Configuration.GetValue<bool>("Debug");
				var minLevel = debugEnabled ? LogLevel.Debug : LogLevel.Information;

				logging.SetMinimumLevel(minLevel);

				var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
				Directory.CreateDirectory(logDir);

				var logFile = Path.Combine(
					logDir,
					$"llmchat-{DateTime.Now:yyyy-MM-dd}.log");

				logging.AddProvider(
					new Adventure.Logging.FileLoggerProvider(logFile, minLevel));
			})
			.ConfigureServices((context, services) =>
			{
				services.Configure<AppSettings>(context.Configuration);

				services.AddSingleton(provider =>
				{
					var settings = provider.GetRequiredService<IOptions<AppSettings>>();
					return new OllamaProps(settings.Value.OllamaUrl, settings.Value.ModelId);
				});

				services.AddLLM();

				services.AddSingleton<HttpClient>();
				services.AddSingleton<IClipboardService, ClipboardService>();
				services.AddSingleton<IChatHistoryRepository, ChatHistoryRepository>();

				// ViewModels
				services.AddSingleton<ViewModels.MainWindowViewModel>();

				// Core services go here later
				// services.AddSingleton<LlmEngine>();
			})
			.Build();
	}
}

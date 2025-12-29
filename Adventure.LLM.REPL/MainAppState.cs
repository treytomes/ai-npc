using Adventure.LLM.REPL.Configuration;
using Adventure.LLM.REPL.Plugins;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Spectre.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Adventure.LLM.REPL;

#region Configuration

#endregion

internal sealed class MainAppState : AppState
{
	#region Constants

	private const string ASSETS_ROOT_PATH = "assets";
	private static readonly string PROMPT_ROOT_PATH = Path.Combine(ASSETS_ROOT_PATH, "prompts");

	#endregion

	#region Fields

	private readonly ILogger<MainAppState> _logger;
	private readonly Kernel _kernel;
	private ChatHistory _persistentHistory = null!;
	private AppConfiguration _config = new();

	#endregion

	#region Constructors

	public MainAppState(
		IStateManager states,
		ILogger<MainAppState> logger,
		Kernel kernel)
		: base(states)
	{
		_logger = logger;
		_kernel = kernel;
	}

	#endregion

	#region Methods

	public override async Task OnEnterAsync()
	{
		RenderHeader();
		await Task.CompletedTask;
	}

	public override async Task OnLoadAsync()
	{
		// Load configuration if exists
		await LoadConfigurationAsync();

		// Initialize persistent history with system prompt
		_persistentHistory = new ChatHistory(
			"You are an environment description renderer for a text adventure game.");

		// Create and register plugins

		// 1. Room Renderer Plugin
		var rendererLogger = _kernel.LoggerFactory.CreateLogger<RoomRendererPlugin>();
		var rendererPlugin = new RoomRendererPlugin(PROMPT_ROOT_PATH, _kernel, _persistentHistory, rendererLogger);
		_kernel.Plugins.AddFromObject(rendererPlugin, "RoomRenderer");

		// 2. Room Validator Plugin
		var validatorLogger = _kernel.LoggerFactory.CreateLogger<RoomValidatorPlugin>();
		var validatorPlugin = new RoomValidatorPlugin(PROMPT_ROOT_PATH, _kernel, validatorLogger);
		_kernel.Plugins.AddFromObject(validatorPlugin, "RoomValidator");

		// 3. Room Orchestration Plugin
		var orchestrationLogger = _kernel.LoggerFactory.CreateLogger<RoomOrchestrationPlugin>();
		var orchestrationConfig = new Dictionary<string, object>
		{
			["maxAttempts"] = _config.Validation.MaxAttempts,
			["sentenceCount"] = _config.Rendering.SentenceCount,
			["minSentences"] = _config.Validation.MinSentences,
			["maxSentences"] = _config.Validation.MaxSentences
		};
		var orchestrationPlugin = new RoomOrchestrationPlugin(_kernel, orchestrationLogger, orchestrationConfig);
		_kernel.Plugins.AddFromObject(orchestrationPlugin, "RoomOrchestration");

		_logger.LogInformation("Plugins registered: {PluginCount}", _kernel.Plugins.Count);
		_logger.LogInformation("Using prompt templates from YAML files");

		await Task.CompletedTask;
	}

	private async Task LoadConfigurationAsync()
	{
		try
		{
			if (File.Exists(Path.Combine(ASSETS_ROOT_PATH, "config.yaml")))
			{
				var yamlContent = await File.ReadAllTextAsync(Path.Combine(ASSETS_ROOT_PATH, "config.yaml"));
				var deserializer = new DeserializerBuilder()
					.WithNamingConvention(CamelCaseNamingConvention.Instance)
					.Build();

				_config = deserializer.Deserialize<AppConfiguration>(yamlContent) ?? new AppConfiguration();
				_logger.LogInformation("Configuration loaded from config.yaml");
			}
			else
			{
				_logger.LogInformation("Using default configuration");
			}
		}
		catch (Exception ex)
		{
			_logger.LogWarning(ex, "Failed to load configuration, using defaults");
			_config = new AppConfiguration();
		}
	}

	public override async Task OnLeaveAsync()
	{
		await Task.CompletedTask;
	}

	public override async Task OnUnloadAsync()
	{
		// Remove all plugins
		_kernel.Plugins.Remove("RoomRenderer");
		_kernel.Plugins.Remove("RoomValidator");
		_kernel.Plugins.Remove("RoomOrchestration");

		_logger.LogInformation("All plugins unloaded");
		await Task.CompletedTask;
	}

	public override async Task OnUpdateAsync()
	{
		var input = ReadInput();
		if (string.IsNullOrWhiteSpace(input))
			return;

		if (input.StartsWith(':'))
		{
			await EvaluateSystemCommandAsync(input);
			return;
		}

		await RenderRoomAsync(input);
	}

	private async Task RenderRoomAsync(string input)
	{
		try
		{
			// Use the orchestration plugin to handle rendering with validation
			var result = await _kernel.InvokeAsync<string>(
				"RoomOrchestration",
				"RenderValidatedRoom",
				new KernelArguments
				{
					["roomJson"] = MainLabJson,
					["userInput"] = input
				});

			// Result is already displayed by the streaming renderer
			// Additional processing could go here if needed

			// Optional: Show validation status
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				AnsiConsole.MarkupLine("[grey]✓ Description validated[/]");
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error rendering room");
			AnsiConsole.MarkupLine("[red]Error: Failed to render room description[/]");
		}
	}

	#endregion

	#region UI Helpers

	private static void RenderHeader()
	{
		AnsiConsole.Clear();
		AnsiConsole.Write(new FigletText("Adventure.LLM").Color(Color.Cyan));
		AnsiConsole.MarkupLine("[grey]Type commands or descriptions. :help for options.[/]");
		AnsiConsole.WriteLine();
	}

	private static string ReadInput() =>
		AnsiConsole.Prompt(new TextPrompt<string>("[bold green]>[/] ").AllowEmpty());

	#endregion

	#region System Commands

	private async Task EvaluateSystemCommandAsync(string input)
	{
		var parts = input.Split(' ', 2);
		var command = parts[0];
		var args = parts.Length > 1 ? parts[1] : string.Empty;

		switch (command)
		{
			case ":exit":
				await LeaveAsync();
				break;
			case ":clear":
				RenderHeader();
				break;
			case ":help":
				ShowHelp();
				break;
			case ":history":
				ShowHistory();
				break;
			case ":plugins":
				ShowPlugins();
				break;
			case ":debug":
				ToggleDebugMode();
				break;
			case ":config":
				ShowConfiguration();
				break;
			case ":reload":
				await ReloadTemplatesAsync();
				break;
			case ":test":
				await TestTemplatesAsync(args);
				break;
		}
	}

	private void ShowHelp()
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("[yellow]Command[/]")
			.AddColumn("[yellow]Description[/]")
			.AddRow(":exit", "Exit the application")
			.AddRow(":clear", "Clear the screen")
			.AddRow(":history", "Show conversation history")
			.AddRow(":plugins", "Show loaded plugins and functions")
			.AddRow(":debug", "Toggle debug mode")
			.AddRow(":config", "Show current configuration")
			.AddRow(":reload", "Reload prompt templates")
			.AddRow(":test [template]", "Test a specific template")
			.AddRow(":help", "Show this help");

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
	}

	private void ShowHistory()
	{
		if (_persistentHistory.Count == 0)
		{
			AnsiConsole.MarkupLine("[grey]No history yet.[/]");
			return;
		}

		var panel = new Panel("[grey]Conversation History[/]")
			.Border(BoxBorder.Rounded)
			.BorderColor(Color.Grey);

		AnsiConsole.Write(panel);

		foreach (var message in _persistentHistory.Skip(1)) // Skip system prompt
		{
			var role = message.Role == AuthorRole.User ? "[blue]User[/]" : "[green]Assistant[/]";
			AnsiConsole.MarkupLine($"{role}: {Markup.Escape(message.Content ?? "")}");
		}

		AnsiConsole.WriteLine();
	}

	private void ShowPlugins()
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title("[yellow]Loaded Plugins[/]")
			.AddColumn("[cyan]Plugin[/]")
			.AddColumn("[cyan]Functions[/]")
			.AddColumn("[cyan]Template[/]");

		foreach (var plugin in _kernel.Plugins)
		{
			var functions = string.Join("\n", plugin.Select(f => f.Name));
			var templateFile = plugin.Name switch
			{
				"RoomRenderer" => Path.Combine(PROMPT_ROOT_PATH, "room_renderer.yaml"),
				"RoomValidator" => Path.Combine(PROMPT_ROOT_PATH, "room_validator.yaml"),
				_ => "N/A"
			};
			table.AddRow(plugin.Name, functions, templateFile);
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
	}

	private void ShowConfiguration()
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title("[yellow]Current Configuration[/]")
			.AddColumn("[cyan]Setting[/]")
			.AddColumn("[cyan]Value[/]");

		table.AddRow("Sentence Count", _config.Rendering.SentenceCount);
		table.AddRow("Temperature", _config.Rendering.Temperature.ToString());
		table.AddRow("Max Tokens", _config.Rendering.MaxTokens.ToString());
		table.AddRow("Max Validation Attempts", _config.Validation.MaxAttempts.ToString());
		table.AddRow("Min Sentences", _config.Validation.MinSentences);
		table.AddRow("Max Sentences", _config.Validation.MaxSentences);

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
	}

	private async Task ReloadTemplatesAsync()
	{
		try
		{
			AnsiConsole.Status()
				.Start("Reloading templates...", ctx =>
				{
					ctx.Spinner(Spinner.Known.Star);
					ctx.SpinnerStyle(Style.Parse("green"));
				});

			// Unload and reload plugins
			await OnUnloadAsync();
			await OnLoadAsync();

			AnsiConsole.MarkupLine("[green]✓ Templates reloaded successfully[/]");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to reload templates");
			AnsiConsole.MarkupLine("[red]✗ Failed to reload templates[/]");
		}
	}

	private async Task TestTemplatesAsync(string templateName)
	{
		try
		{
			if (string.IsNullOrWhiteSpace(templateName))
			{
				AnsiConsole.MarkupLine("[yellow]Usage: :test [renderer|validator][/]");
				return;
			}

			switch (templateName.ToLower())
			{
				case "renderer":
					await TestRendererTemplate();
					break;
				case "validator":
					await TestValidatorTemplate();
					break;
				default:
					AnsiConsole.MarkupLine($"[red]Unknown template: {templateName}[/]");
					break;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Template test failed");
			AnsiConsole.MarkupLine("[red]Template test failed[/]");
		}
	}

	private async Task TestRendererTemplate()
	{
		AnsiConsole.MarkupLine("[cyan]Testing Room Renderer Template[/]");

		var testInput = "look around";
		var result = await _kernel.InvokeAsync<string>(
			"RoomRenderer",
			"RenderRoom",
			new KernelArguments
			{
				["roomJson"] = MainLabJson,
				["userInput"] = testInput,
				["sentenceCount"] = "3-5"
			});

		AnsiConsole.MarkupLine("[green]Test completed![/]");
	}

	private async Task TestValidatorTemplate()
	{
		AnsiConsole.MarkupLine("[cyan]Testing Room Validator Template[/]");

		var testDescription = "You stand in the main laboratory. The steel furniture shows signs of recent use, with scattered instruments and dried residue visible. A low electrical hum vibrates through the floor.";

		var result = await _kernel.InvokeAsync<bool>(
			"RoomValidator",
			"ValidateRoomDescription",
			new KernelArguments
			{
				["description"] = testDescription,
				["minSentences"] = "3",
				["maxSentences"] = "5"
			});

		AnsiConsole.MarkupLine($"[green]Validation result: {(result ? "OK" : "REGENERATE")}[/]");
	}

	private void ToggleDebugMode()
	{
		// This would typically toggle a logger configuration
		var currentLevel = _logger.IsEnabled(LogLevel.Debug);
		AnsiConsole.MarkupLine($"[yellow]Debug mode: {(!currentLevel ? "ON" : "OFF")}[/]");
	}

	#endregion

	#region World Data

	private const string MainLabJson = @"
{
  ""room"": {
    ""name"": ""Main Laboratory"",
    ""spatial_summary"": {
      ""shape"": ""rectangular"",
      ""size"": ""medium"",
      ""lighting"": ""flickering_overhead"",
      ""smell"": [""ozone"", ""cleaning_agent""]
    },
    ""static_features"": [
      {
        ""type"": ""furniture"",
        ""facts"": {
          ""material"": ""steel"",
          ""condition"": ""recently_used"",
          ""details"": [""scattered instruments"", ""dried residue""]
        }
      }
    ],
    ""ambient_details"": {
      ""always"": [""A low electrical hum vibrates through the floor.""]
    }
  }
}
";

	#endregion
}

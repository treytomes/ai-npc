using Adventure.LLM.REPL.Configuration;
using Adventure.LLM.REPL.Plugins;
using Adventure.LLM.REPL.ValueObjects;
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
	private static readonly string ROOMS_ROOT_PATH = Path.Combine(ASSETS_ROOT_PATH, "rooms");

	#endregion

	#region Fields

	private readonly ILogger<MainAppState> _logger;
	private readonly Kernel _kernel;
	private ChatHistory _persistentHistory = null!;
	private AppConfiguration _config = new();
	private Dictionary<string, WorldData> _worldData = new();
	private string _currentRoom = "main_lab";

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

		// Load world data
		await LoadWorldDataAsync();

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

	private async Task LoadWorldDataAsync()
	{
		try
		{
			var worldDataFiles = Directory.GetFiles(ROOMS_ROOT_PATH, "*.room.yaml");

			if (worldDataFiles.Length == 0)
			{
				throw new FileNotFoundException("Unable to find any room files.");
			}

			var deserializer = new DeserializerBuilder()
				.WithNamingConvention(UnderscoredNamingConvention.Instance)
				.Build();

			foreach (var file in worldDataFiles)
			{
				var yamlContent = await File.ReadAllTextAsync(file);
				var worldData = deserializer.Deserialize<WorldData>(yamlContent);
				var roomKey = Path.GetFileNameWithoutExtension(file).Replace(".room", "");
				_worldData[roomKey] = worldData;
				_logger.LogInformation("Loaded room data: {RoomKey} from {File}", roomKey, file);
			}

			AnsiConsole.MarkupLine($"[green]Loaded {_worldData.Count} room(s)[/]");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to load world data");
			AnsiConsole.MarkupLine("[red]Failed to load world data[/]");
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
			// Get current room data
			if (!_worldData.TryGetValue(_currentRoom, out var worldData))
			{
				AnsiConsole.MarkupLine("[red]Error: Current room data not found[/]");
				return;
			}

			// Convert world data to JSON for the renderer (or modify renderer to accept YAML)
			var roomYaml = ConvertWorldDataToYaml(worldData);

			// Use the orchestration plugin to handle rendering with validation
			var result = await _kernel.InvokeAsync<string>(
				"RoomOrchestration",
				"RenderValidatedRoom",
				new KernelArguments
				{
					["roomYaml"] = roomYaml,
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

	private string ConvertWorldDataToYaml(WorldData worldData)
	{
		var serializer = new SerializerBuilder()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();

		return serializer.Serialize(worldData);
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
			case ":rooms":
				ShowRooms();
				break;
			case ":goto":
				await ChangeRoomAsync(args);
				break;
			case ":room":
				ShowCurrentRoom();
				break;
			case ":export":
				await ExportRoomAsync(args);
				break;
		}
	}

	private void ShowRooms()
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title("[yellow]Available Rooms[/]")
			.AddColumn("[cyan]Key[/]")
			.AddColumn("[cyan]Name[/]")
			.AddColumn("[cyan]Features[/]");

		foreach (var (key, data) in _worldData)
		{
			var featureCount = data.Room.StaticFeatures.Count;
			var current = key == _currentRoom ? " [green](current)[/]" : "";
			table.AddRow(
				key + current,
				data.Room.Name,
				$"{featureCount} feature(s)"
			);
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
	}

	private async Task ChangeRoomAsync(string roomKey)
	{
		if (string.IsNullOrWhiteSpace(roomKey))
		{
			AnsiConsole.MarkupLine("[yellow]Usage: :goto <room_key>[/]");
			return;
		}

		if (_worldData.ContainsKey(roomKey))
		{
			_currentRoom = roomKey;
			AnsiConsole.MarkupLine($"[green]Moved to: {_worldData[roomKey].Room.Name}[/]");

			// Optionally auto-render the new room
			await RenderRoomAsync("look around");
		}
		else
		{
			AnsiConsole.MarkupLine($"[red]Unknown room: {roomKey}[/]");
		}
	}

	private void ShowCurrentRoom()
	{
		if (!_worldData.TryGetValue(_currentRoom, out var worldData))
		{
			AnsiConsole.MarkupLine("[red]Error: Current room data not found[/]");
			return;
		}

		var room = worldData.Room;

		var panel = new Panel($"""
            [yellow]Name:[/] {room.Name}
            [yellow]Shape:[/] {room.SpatialSummary.Shape}
            [yellow]Size:[/] {room.SpatialSummary.Size}
            [yellow]Lighting:[/] {room.SpatialSummary.Lighting}
            [yellow]Smells:[/] {string.Join(", ", room.SpatialSummary.Smell)}
            [yellow]Features:[/] {room.StaticFeatures.Count}
            """)
			.Header($"[cyan]Current Room: {_currentRoom}[/]")
			.Border(BoxBorder.Rounded)
			.BorderColor(Color.Cyan);

		AnsiConsole.Write(panel);
		AnsiConsole.WriteLine();
	}

	private async Task ExportRoomAsync(string format)
	{
		if (!_worldData.TryGetValue(_currentRoom, out var worldData))
		{
			AnsiConsole.MarkupLine("[red]Error: Current room data not found[/]");
			return;
		}

		format = format.ToLower();

		switch (format)
		{
			case "yaml":
				var serializer = new SerializerBuilder()
					.WithNamingConvention(UnderscoredNamingConvention.Instance)
					.Build();
				var yaml = serializer.Serialize(worldData);
				var yamlFile = $"{_currentRoom}_export.yaml";
				await File.WriteAllTextAsync(yamlFile, yaml);
				AnsiConsole.MarkupLine($"[green]Exported to {yamlFile}[/]");
				break;

			default:
				AnsiConsole.MarkupLine("[yellow]Usage: :export [json|yaml][/]");
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
			.AddRow(":test <template>", "Test a specific template")
			.AddRow(":rooms", "List all available rooms")
			.AddRow(":goto <room>", "Change to a different room")
			.AddRow(":room", "Show current room details")
			.AddRow(":export <yaml>", "Export current room data")
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

		// Get current room data
		if (!_worldData.TryGetValue(_currentRoom, out var worldData))
		{
			AnsiConsole.MarkupLine("[red]Error: Current room data not found[/]");
			return;
		}

		// Convert world data to JSON for the renderer (or modify renderer to accept YAML)
		var roomYaml = ConvertWorldDataToYaml(worldData);

		var testInput = "look around";
		var result = await _kernel.InvokeAsync<string>(
			"RoomRenderer",
			"RenderRoom",
			new KernelArguments
			{
				["roomYaml"] = roomYaml,
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
}

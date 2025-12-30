using Adventure.LLM.REPL.Configuration;
using Adventure.LLM.REPL.Persistence;
using Adventure.LLM.REPL.Plugins;
using Adventure.LLM.REPL.Renderables;
using Adventure.LLM.REPL.Services;
using Adventure.LLM.REPL.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Spectre.Console;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Adventure.LLM.REPL;

internal sealed class MainAppState : AppState
{
	#region Fields

	private readonly ILogger<MainAppState> _logger;
	private readonly AppSettings _settings;
	private readonly Kernel _kernel;
	private readonly IRoomRepository _roomRepository;
	private readonly IRoomNavigationService _navigationService;
	private ChatHistory _persistentHistory = null!;
	private AppConfiguration _config = new();

	#endregion

	#region Constructors

	public MainAppState(
		IOptions<AppSettings> settings,
		IStateManager states,
		ILogger<MainAppState> logger,
		Kernel kernel,
		IRoomRepository roomRepository,
		IRoomNavigationService navigationService
	) : base(states)
	{
		_settings = settings.Value;
		_logger = logger;
		_kernel = kernel;
		_roomRepository = roomRepository;
		_navigationService = navigationService;
	}

	#endregion

	#region Methods

	public override async Task OnEnterAsync()
	{
		// Subscribe to room change events
		_navigationService.RoomChanged += OnRoomChanged;

		RenderHeader();

		await RenderRoomAsync("look around");
	}

	public override async Task OnLoadAsync()
	{
		// Load configuration if exists.
		await LoadConfigurationAsync();

		// Load world data through repository.
		await _roomRepository.LoadRoomsAsync();

		// Initialize persistent history with system prompt.
		_persistentHistory = new ChatHistory("You are an environment description renderer for a text adventure game.");

		// Create and register plugins.

		var promptsPath = Path.Combine(_settings.AssetsPath, _settings.PromptAssetsPath);

		// 1. Intent Analyzer Plugin (NEW)
		var intentLogger = _kernel.LoggerFactory.CreateLogger<IntentAnalyzerPlugin>();
		var intentPlugin = new IntentAnalyzerPlugin(promptsPath, _kernel, intentLogger);
		_kernel.Plugins.AddFromObject(intentPlugin, "IntentAnalyzer");

		// 2. Room Renderer Plugin
		var rendererLogger = _kernel.LoggerFactory.CreateLogger<RoomRendererPlugin>();
		var rendererPlugin = new RoomRendererPlugin(promptsPath, _kernel, _persistentHistory, rendererLogger);
		_kernel.Plugins.AddFromObject(rendererPlugin, "RoomRenderer");

		// 3. Room Validator Plugin
		var validatorLogger = _kernel.LoggerFactory.CreateLogger<RoomValidatorPlugin>();
		var validatorPlugin = new RoomValidatorPlugin(promptsPath, _kernel, validatorLogger);
		_kernel.Plugins.AddFromObject(validatorPlugin, "RoomValidator");

		// 4. Room Orchestration Plugin
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
		_logger.LogInformation("Using AI-powered intent analysis with Polly retry logic");

		await Task.CompletedTask;
	}

	private void OnRoomChanged(object? sender, RoomChangedEventArgs e)
	{
		_logger.LogInformation("Room changed from {Previous} to {New}", e.PreviousRoomKey, e.NewRoomKey);
	}

	private async Task LoadConfigurationAsync()
	{
		try
		{
			if (File.Exists(Path.Combine(_settings.AssetsPath, "config.yaml")))
			{
				var yamlContent = await File.ReadAllTextAsync(Path.Combine(_settings.AssetsPath, "config.yaml"));
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
		// Unsubscribe to room change events.
		_navigationService.RoomChanged -= OnRoomChanged;

		await Task.CompletedTask;
	}

	public override async Task OnUnloadAsync()
	{
		// Unsubscribe from events
		_navigationService.RoomChanged -= OnRoomChanged;

		// Remove all plugins
		_kernel.Plugins.Remove("IntentAnalyzer");
		_kernel.Plugins.Remove("RoomRenderer");
		_kernel.Plugins.Remove("RoomValidator");
		_kernel.Plugins.Remove("RoomOrchestration");

		_logger.LogInformation("All plugins unloaded");
		await Task.CompletedTask;
	}

	public override async Task OnUpdateAsync()
	{
		var input = await ReadInputAsync();
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
			// First, analyze the user's intent
			var intent = await _kernel.InvokeAsync<UserIntent>(
				"IntentAnalyzer",
				"AnalyzeIntent",
				new KernelArguments
				{
					["userInput"] = input
				});

			if (intent == null)
			{
				throw new NullReferenceException("Unable to parse user intent.");
			}

			// Log the analysis result
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				AnsiConsole.MarkupLine($"[grey]Intent: {intent.Intent}, Focus: {intent.Focus ?? "(none)"}[/]");
			}

			// Handle different intents
			switch (intent.Intent)
			{
				case IntentTypes.Look:
				case IntentTypes.Examine:
				case IntentTypes.Smell:
				case IntentTypes.Listen:
				case IntentTypes.Touch:
					await HandleObservationIntent(intent);
					break;

				case IntentTypes.Go:
					await HandleMovementIntent(intent);
					break;

				case IntentTypes.Take:
					await HandleTakeIntent(intent);
					break;

				case IntentTypes.Use:
					await HandleUseIntent(intent);
					break;

				default:
					// Fallback to general room description
					await HandleObservationIntent(intent);
					break;
			}
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Error processing user input");
			if (_logger.IsEnabled(LogLevel.Debug))
			{
				AnsiConsole.WriteException(ex);
			}
			else
			{
				AnsiConsole.MarkupLine("[red]Error: Failed to process your command[/]");
			}
		}
	}

	private async Task HandleObservationIntent(UserIntent intent)
	{
		// Get current room data from navigation service.
		var worldData = _navigationService.CurrentRoom;
		if (worldData == null)
		{
			AnsiConsole.MarkupLine("[red]Error: Current room data not found[/]");
			return;
		}

		// Convert world data to YAML for the renderer.
		var roomData = worldData.ToYaml();

		// Map intent to focus for renderer.
		var focus = intent.Intent switch
		{
			IntentTypes.Smell => "smells and odors",
			IntentTypes.Listen => "sounds and ambient noise",
			IntentTypes.Touch => "textures and physical sensations",
			IntentTypes.Examine when !string.IsNullOrEmpty(intent.Focus) => $"detailed examination of {intent.Focus}",
			_ => intent.Focus
		};

		// Use the orchestration plugin to handle rendering with validation.
		var _ = await _kernel.InvokeAsync<string>(
			"RoomOrchestration",
			"RenderValidatedRoom",
			new KernelArguments
			{
				["roomData"] = roomData,
				["userInput"] = intent.OriginalInput,
				["focus"] = focus
			});
	}

	private async Task HandleMovementIntent(UserIntent intent)
	{
		if (string.IsNullOrEmpty(intent.Focus))
		{
			AnsiConsole.MarkupLine("[yellow]Where do you want to go?[/]");
			return;
		}

		// Try to find a matching room
		var possibleRoom = _roomRepository.GetRoomKeys()
			.FirstOrDefault(k => k.Contains(intent.Focus, StringComparison.OrdinalIgnoreCase));

		if (possibleRoom != null)
		{
			var result = await _navigationService.NavigateToAsync(possibleRoom);

			if (result.Success)
			{
				var newRoom = _navigationService.CurrentRoom;
				AnsiConsole.MarkupLine($"[green]You move to: {newRoom?.Room.Name}[/]");

				// Auto-render the new room
				await RenderRoomAsync("look around");
			}
			else
			{
				AnsiConsole.MarkupLine($"[yellow]{result.Message}[/]");
				ShowAvailableExits();
			}
		}
		else
		{
			AnsiConsole.MarkupLine($"[yellow]You can't go to '{intent.Focus}' from here.[/]");
			ShowAvailableExits();
		}
	}

	private void ShowAvailableExits()
	{
		var availableDestinations = _navigationService.GetAvailableDestinations().ToList();
		if (availableDestinations.Any())
		{
			AnsiConsole.MarkupLine("[grey]Available locations: " +
				string.Join(", ", availableDestinations) + "[/]");
		}
	}

	private async Task HandleTakeIntent(UserIntent intent)
	{
		if (string.IsNullOrEmpty(intent.Focus))
		{
			AnsiConsole.MarkupLine("[yellow]What do you want to take?[/]");
			return;
		}

		// For now, just indicate the action isn't implemented
		AnsiConsole.MarkupLine($"[yellow]You can't take the {intent.Focus} right now.[/]");
		await Task.CompletedTask;
	}

	private async Task HandleUseIntent(UserIntent intent)
	{
		if (string.IsNullOrEmpty(intent.Focus))
		{
			AnsiConsole.MarkupLine("[yellow]What do you want to use?[/]");
			return;
		}

		// For now, just indicate the action isn't implemented
		AnsiConsole.MarkupLine($"[yellow]You can't use the {intent.Focus} right now.[/]");
		await Task.CompletedTask;
	}

	#endregion

	#region UI Helpers

	private static void RenderHeader()
	{
		AnsiConsole.Clear();
		AnsiConsole.Write(new HeaderRenderable(
			"Adventure.LLM",
			"Type commands or descriptions. :help for options."
		));
	}

	private static async Task<string> ReadInputAsync() =>
		await AnsiConsole.PromptAsync(new TextPrompt<string>("[bold green]>[/] ").AllowEmpty());

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
				AnsiConsole.Write(new HelpRenderable());
				break;
			case ":history":
				AnsiConsole.Write(new HistoryRenderable(_persistentHistory));
				break;
			case ":plugins":
				AnsiConsole.Write(new PluginsRenderable(_kernel.Plugins));
				break;
			case ":debug":
				ToggleDebugMode();
				break;
			case ":config":
				AnsiConsole.Write(new ConfigurationRenderable(_config));
				break;
			case ":reload":
				await ReloadTemplatesAsync();
				break;
			case ":rooms":
				var allRooms = _roomRepository.GetAllRooms();
				var currentKey = _navigationService.CurrentRoomKey;
				AnsiConsole.Write(new RoomsRenderable(allRooms, currentKey));
				break;
			case ":goto":
				await ChangeRoomAsync(args);
				break;
			case ":room":
				AnsiConsole.Write(new CurrentRoomRenderable(_navigationService.CurrentRoom));
				break;
		}
	}

	private async Task ChangeRoomAsync(string roomKey)
	{
		if (string.IsNullOrWhiteSpace(roomKey))
		{
			AnsiConsole.MarkupLine("[yellow]Usage: :goto <room_key>[/]");
			return;
		}

		var result = await _navigationService.NavigateToAsync(roomKey);

		if (result.Success)
		{
			var newRoom = _navigationService.CurrentRoom;
			AnsiConsole.MarkupLine($"[green]Moved to: {newRoom?.Room.Name}[/]");

			// Optionally auto-render the new room
			await RenderRoomAsync("look around");
		}
		else
		{
			AnsiConsole.MarkupLine($"[red]{result.Message}[/]");
		}
	}

	private async Task ReloadTemplatesAsync()
	{
		try
		{
			AnsiConsole.Status()
				.Start("Reloading templates and rooms...", ctx =>
				{
					ctx.Spinner(Spinner.Known.Star);
					ctx.SpinnerStyle(Style.Parse("green"));
				});

			// Reload rooms
			await _roomRepository.ReloadRoomsAsync();

			// Unload and reload plugins
			await OnUnloadAsync();
			await OnLoadAsync();

			AnsiConsole.MarkupLine("[green]✓ Templates and rooms reloaded successfully[/]");
		}
		catch (Exception ex)
		{
			_logger.LogError(ex, "Failed to reload templates");
			AnsiConsole.MarkupLine("[red]✗ Failed to reload templates[/]");
		}
	}

	private void ToggleDebugMode()
	{
		// This would typically toggle a logger configuration
		var currentLevel = _logger.IsEnabled(LogLevel.Debug);
		AnsiConsole.MarkupLine($"[yellow]Debug mode: {(!currentLevel ? "ON" : "OFF")}[/]");
	}

	#endregion
}

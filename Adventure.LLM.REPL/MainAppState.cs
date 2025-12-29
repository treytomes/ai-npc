using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.LLM.REPL;

#region Plugin Definitions

internal sealed class RoomRendererPlugin
{
	private const string RENDERER_SYSTEM_PROMPT = @"
You are an environment description renderer for a text adventure game.

Rules:
- Use ONLY the information provided in the input JSON.
- Do NOT invent or infer facts.
- Second-person, present tense.
- Output ONE paragraph of 3–5 sentences.
- No lists, no dialogue, no game mechanics.
- Describe inactive or powered-off objects as such.
- Prefer concrete sensory details.

You are a renderer, not a storyteller.
";

	private readonly IChatCompletionService _chatService;
	private readonly Kernel _kernel;
	private readonly ChatHistory _persistentHistory;
	private readonly ILogger<RoomRendererPlugin> _logger;

	public RoomRendererPlugin(
		Kernel kernel,
		ChatHistory persistentHistory,
		ILogger<RoomRendererPlugin> logger)
	{
		_kernel = kernel;
		_chatService = kernel.GetRequiredService<IChatCompletionService>();
		_persistentHistory = persistentHistory;
		_logger = logger;
	}

	[KernelFunction("RenderRoom")]
	[Description("Renders a room description from JSON data")]
	public async Task<string> RenderRoomAsync(
		[Description("Room JSON")] string roomJson,
		[Description("User input")] string userInput)
	{
		// Create ephemeral history
		var ephemeralHistory = new ChatHistory(_persistentHistory);
		ephemeralHistory.AddUserMessage(roomJson);
		ephemeralHistory.AddUserMessage(userInput);

		// Stream the response with visual feedback
		var result = await StreamRenderAsync(ephemeralHistory);

		// Update persistent history
		_persistentHistory.AddUserMessage(userInput);
		_persistentHistory.AddAssistantMessage(result);

		return result;
	}

	private async Task<string> StreamRenderAsync(ChatHistory history)
	{
		var sb = new StringBuilder();
		var layout = (IRenderable)new Rows();
		var sentenceBuffer = new StringBuilder();

		var executionSettings = new PromptExecutionSettings
		{
			ExtensionData = new Dictionary<string, object>
			{
				["temperature"] = 0.15f,
				["max_tokens"] = 120,
				["stop"] = new[] { "\n\n" }
			}
		};

		await AnsiConsole.Live(layout).StartAsync(async ctx =>
		{
			await foreach (var update in _chatService.GetStreamingChatMessageContentsAsync(
				history,
				executionSettings,
				_kernel))
			{
				var text = update.Content;
				if (string.IsNullOrWhiteSpace(text))
					continue;

				sentenceBuffer.Append(text);
				sb.Append(sentenceBuffer);
				sentenceBuffer.Clear();

				layout = new Panel(sb.ToString())
					.Header("[green]Narrator[/]")
					.Border(BoxBorder.Rounded)
					.BorderColor(Color.Green);

				ctx.UpdateTarget(layout);
			}
		});

		return sb.Append(sentenceBuffer).ToString().Trim();
	}
}

internal sealed class RoomValidatorPlugin
{
	private const string VALIDATOR_SYSTEM_PROMPT = @"
You are a compliance checker.

Check the assistant output against these rules:
- Second-person present tense
- 3–5 sentences
- No invented objects
- No lists or dialogue

Return ONLY one word:
OK or REGENERATE
";

	private readonly IChatCompletionService _chatService;
	private readonly Kernel _kernel;
	private readonly ILogger<RoomValidatorPlugin> _logger;

	public RoomValidatorPlugin(
		Kernel kernel,
		ILogger<RoomValidatorPlugin> logger)
	{
		_kernel = kernel;
		_chatService = kernel.GetRequiredService<IChatCompletionService>();
		_logger = logger;
	}

	[KernelFunction("ValidateRoomDescription")]
	[Description("Validates a room description meets requirements")]
	[return: Description("Returns true if valid, false if regeneration needed")]
	public async Task<bool> ValidateRoomDescriptionAsync(
		[Description("The room description to validate")] string description)
	{
		var history = new ChatHistory(VALIDATOR_SYSTEM_PROMPT);
		history.AddUserMessage(description);

		var settings = new PromptExecutionSettings
		{
			ExtensionData = new Dictionary<string, object>
			{
				["temperature"] = 0,
				["max_tokens"] = 5
			}
		};

		var response = await _chatService.GetChatMessageContentAsync(history, settings, _kernel);
		var verdict = response.Content?.Trim();

		_logger.LogInformation("Validator verdict: {Verdict}", verdict);

		return verdict == "OK";
	}
}

internal sealed class RoomOrchestrationPlugin
{
	private readonly Kernel _kernel;
	private readonly ILogger<RoomOrchestrationPlugin> _logger;

	public RoomOrchestrationPlugin(
		Kernel kernel,
		ILogger<RoomOrchestrationPlugin> logger)
	{
		_kernel = kernel;
		_logger = logger;
	}

	[KernelFunction("RenderValidatedRoom")]
	[Description("Renders a room with automatic validation and retry")]
	public async Task<string> RenderValidatedRoomAsync(
		[Description("Room JSON data")] string roomJson,
		[Description("User input")] string userInput)
	{
		string result = string.Empty;
		int attempts = 0;
		const int maxAttempts = 2;

		do
		{
			attempts++;
			_logger.LogInformation("Rendering attempt {Attempt}/{MaxAttempts}", attempts, maxAttempts);

			// Render the room
			result = await _kernel.InvokeAsync<string>(
				"RoomRenderer",
				"RenderRoom",
				new KernelArguments
				{
					["roomJson"] = roomJson,
					["userInput"] = userInput
				});

			// Validate the result
			var isValid = await _kernel.InvokeAsync<bool>(
				"RoomValidator",
				"ValidateRoomDescription",
				new KernelArguments
				{
					["description"] = result
				});

			if (isValid)
			{
				_logger.LogInformation("Validation passed on attempt {Attempt}", attempts);
				break;
			}

			_logger.LogWarning("Validation failed on attempt {Attempt}", attempts);
		}
		while (attempts < maxAttempts);

		_logger.LogInformation("Total render attempts: {Attempts}", attempts);
		return result;
	}
}

#endregion

internal sealed class MainAppState : AppState
{
	#region Fields

	private readonly ILogger<MainAppState> _logger;
	private readonly Kernel _kernel;
	private ChatHistory _persistentHistory = null!;

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
		// Initialize persistent history with system prompt
		_persistentHistory = new ChatHistory(
			"You are an environment description renderer for a text adventure game.");

		// Create and register plugins

		// 1. Room Renderer Plugin
		var rendererLogger = _kernel.LoggerFactory.CreateLogger<RoomRendererPlugin>();
		var rendererPlugin = new RoomRendererPlugin(_kernel, _persistentHistory, rendererLogger);
		_kernel.Plugins.AddFromObject(rendererPlugin, "RoomRenderer");

		// 2. Room Validator Plugin
		var validatorLogger = _kernel.LoggerFactory.CreateLogger<RoomValidatorPlugin>();
		var validatorPlugin = new RoomValidatorPlugin(_kernel, validatorLogger);
		_kernel.Plugins.AddFromObject(validatorPlugin, "RoomValidator");

		// 3. Room Orchestration Plugin
		var orchestrationLogger = _kernel.LoggerFactory.CreateLogger<RoomOrchestrationPlugin>();
		var orchestrationPlugin = new RoomOrchestrationPlugin(_kernel, orchestrationLogger);
		_kernel.Plugins.AddFromObject(orchestrationPlugin, "RoomOrchestration");

		_logger.LogInformation("Plugins registered: {PluginCount}", _kernel.Plugins.Count);

		await Task.CompletedTask;
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
		switch (input)
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
			.AddColumn("[cyan]Functions[/]");

		foreach (var plugin in _kernel.Plugins)
		{
			var functions = string.Join("\n", plugin.Select(f => f.Name));
			table.AddRow(plugin.Name, functions);
		}

		AnsiConsole.Write(table);
		AnsiConsole.WriteLine();
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

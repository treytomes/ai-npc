using System.ComponentModel;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.LLM.REPL;

#region Plugin Definition

public sealed class RoomRenderingPlugin
{
	#region Prompts

	public const string RENDERER_SYSTEM_PROMPT = @"
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

	public const string VALIDATOR_SYSTEM_PROMPT = @"
You are a compliance checker.

Check the assistant output against these rules:
- Second-person present tense
- 3–5 sentences
- No invented objects
- No lists or dialogue

Return ONLY one word:
OK or REGENERATE
";

	#endregion

	private readonly ILogger<RoomRenderingPlugin> _logger;
	private readonly IChatCompletionService _renderer;
	private readonly IChatCompletionService _validator;
	private readonly Kernel _kernel;
	private readonly ChatHistory _persistentHistory;

	public RoomRenderingPlugin(
		ILogger<RoomRenderingPlugin> logger,
		Kernel kernel,
		ChatHistory persistentHistory)
	{
		_logger = logger;
		_kernel = kernel;
		_persistentHistory = persistentHistory;

		_renderer = _kernel.GetRequiredService<IChatCompletionService>();
		_validator = _kernel.GetRequiredService<IChatCompletionService>();
	}

	[KernelFunction("RenderRoomWithValidation")]
	[Description("Renders a room description with automatic validation and retry")]
	public async Task<string> RenderRoomWithValidationAsync(
		[Description("The room JSON data")] string roomJson,
		[Description("User input or command")] string userInput)
	{
		// Create ephemeral history for this render
		var ephemeralHistory = new ChatHistory(_persistentHistory);
		ephemeralHistory.AddUserMessage(roomJson);
		ephemeralHistory.AddUserMessage(userInput);

		string finalText;
		int attempts = 0;

		do
		{
			attempts++;
			finalText = await RenderAsync(ephemeralHistory);
		}
		while (attempts < 2 && !await ValidateAsync(finalText));

		_logger.LogInformation("Render attempts: {Attempts}", attempts);

		// Commit to persistent history
		_persistentHistory.AddUserMessage(userInput);
		_persistentHistory.AddAssistantMessage(finalText);

		return finalText;
	}

	[KernelFunction("StreamRenderRoom")]
	[Description("Renders a room description with live streaming output")]
	public async Task<string> StreamRenderRoomAsync(
		[Description("The chat history")] ChatHistory history)
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
			await foreach (var update in _renderer.GetStreamingChatMessageContentsAsync(
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

	private async Task<string> RenderAsync(ChatHistory history)
	{
		return await StreamRenderRoomAsync(history);
	}

	private async Task<bool> ValidateAsync(string text)
	{
		var validationHistory = new ChatHistory(VALIDATOR_SYSTEM_PROMPT);
		validationHistory.AddUserMessage(text);

		var executionSettings = new PromptExecutionSettings
		{
			ExtensionData = new Dictionary<string, object>
			{
				["temperature"] = 0,
				["max_tokens"] = 5
			}
		};

		var response = await _validator.GetChatMessageContentAsync(
			validationHistory,
			executionSettings,
			_kernel);

		var verdict = response.Content?.Trim();
		_logger.LogInformation("Validator verdict: {Verdict}", verdict);

		return verdict == "OK";
	}
}

#endregion

internal sealed class MainAppState : AppState
{
	#region Fields

	private readonly ILogger<MainAppState> _logger;
	private readonly Kernel _kernel;
	private ChatHistory _persistentHistory = null!;
	private RoomRenderingPlugin _renderingPlugin = null!;

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
		// Initialize persistent history
		_persistentHistory = new ChatHistory(RoomRenderingPlugin.RENDERER_SYSTEM_PROMPT);

		// Create plugin instance
		var pluginLogger = _kernel.LoggerFactory.CreateLogger<RoomRenderingPlugin>();
		_renderingPlugin = new RoomRenderingPlugin(pluginLogger, _kernel, _persistentHistory);

		// Register plugin with kernel.
		_kernel.Plugins.AddFromObject(_renderingPlugin, "RoomRenderer");

		await Task.CompletedTask;
	}

	public override async Task OnLeaveAsync()
	{
		await Task.CompletedTask;
	}

	public override async Task OnUnloadAsync()
	{
		// Remove plugin
		_kernel.Plugins.Remove(_kernel.Plugins.Single(x => x.Name == "RoomRenderer"));
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
			// Use the plugin function
			var result = await _kernel.InvokeAsync<string>(
				"RoomRenderer",
				"RenderRoomWithValidation",
				new KernelArguments
				{
					["roomJson"] = MainLabJson,
					["userInput"] = input
				});

			// Result is already displayed by the streaming renderer
			// Additional processing could go here if needed
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

#region Alternative Plugin Implementations

// If you want even more separation, you could create individual plugins for each concern:

public sealed class RoomRendererPlugin
{
	private readonly IChatCompletionService _chatService;
	private readonly ChatHistory _systemHistory;

	public RoomRendererPlugin(IChatCompletionService chatService)
	{
		_chatService = chatService;
		_systemHistory = new ChatHistory(RoomRenderingPlugin.RENDERER_SYSTEM_PROMPT);
	}

	[KernelFunction("RenderRoom")]
	[Description("Renders a room description from JSON data")]
	public async Task<string> RenderRoomAsync(
		[Description("Room JSON")] string roomJson,
		[Description("User input")] string userInput)
	{
		var history = new ChatHistory(_systemHistory);
		history.AddUserMessage(roomJson);
		history.AddUserMessage(userInput);

		var settings = new PromptExecutionSettings
		{
			ExtensionData = new Dictionary<string, object>
			{
				["temperature"] = 0.15f,
				["max_tokens"] = 120,
				["stop"] = new[] { "\n\n" }
			}
		};

		var response = await _chatService.GetChatMessageContentAsync(history, settings);
		return response.Content ?? "";
	}
}

public sealed class RoomValidatorPlugin
{
	private readonly IChatCompletionService _chatService;

	public RoomValidatorPlugin(IChatCompletionService chatService)
	{
		_chatService = chatService;
	}

	[KernelFunction("ValidateRoomDescription")]
	[Description("Validates a room description meets requirements")]
	[return: Description("Returns true if valid, false if regeneration needed")]
	public async Task<bool> ValidateRoomDescriptionAsync(
		[Description("The room description to validate")] string description)
	{
		var history = new ChatHistory(RoomRenderingPlugin.VALIDATOR_SYSTEM_PROMPT);
		history.AddUserMessage(description);

		var settings = new PromptExecutionSettings
		{
			ExtensionData = new Dictionary<string, object>
			{
				["temperature"] = 0,
				["max_tokens"] = 5
			}
		};

		var response = await _chatService.GetChatMessageContentAsync(history, settings);
		return response.Content?.Trim() == "OK";
	}
}

// You could then compose these plugins using a planner or orchestrator:
public sealed class RoomOrchestrationPlugin
{
	private readonly Kernel _kernel;

	public RoomOrchestrationPlugin(Kernel kernel)
	{
		_kernel = kernel;
	}

	[KernelFunction("RenderValidatedRoom")]
	public async Task<string> RenderValidatedRoomAsync(
		string roomJson,
		string userInput)
	{
		string result;
		int attempts = 0;

		do
		{
			result = await _kernel.InvokeAsync<string>(
				"RoomRenderer",
				"RenderRoom",
				new() { ["roomJson"] = roomJson, ["userInput"] = userInput });

			var isValid = await _kernel.InvokeAsync<bool>(
				"RoomValidator",
				"ValidateRoomDescription",
				new() { ["description"] = result });

			if (isValid) break;

			attempts++;
		}
		while (attempts < 2);

		return result;
	}
}

#endregion
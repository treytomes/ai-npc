using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.LLM.REPL;

internal sealed class MainAppState : AppState
{
	#region Prompts

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

	#endregion

	#region Fields

	private readonly ILogger<MainAppState> _logger;
	private readonly Kernel _kernel;
	private readonly IChatCompletionService _renderer;
	private readonly IChatCompletionService _validator;

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

		// Get the named services from the kernel
		_renderer = _kernel.GetRequiredService<IChatCompletionService>();
		_validator = _kernel.GetRequiredService<IChatCompletionService>();
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
		_persistentHistory = new ChatHistory(RENDERER_SYSTEM_PROMPT);
		await Task.CompletedTask;
	}

	public override async Task OnLeaveAsync()
	{
		await Task.CompletedTask;
	}

	public override async Task OnUnloadAsync()
	{
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
		// Create ephemeral history for this render
		var ephemeralHistory = new ChatHistory(_persistentHistory);

		// Inject world state ephemerally
		ephemeralHistory.AddUserMessage(MainLabJson);
		ephemeralHistory.AddUserMessage(input);

		string finalText;
		int attempts = 0;

		do
		{
			attempts++;
			finalText = await StreamRenderAsync(ephemeralHistory);
		}
		while (attempts < 2 && !await ValidateAsync(finalText));

		_logger.LogInformation("Render attempts: {Attempts}", attempts);

		// Commit the final response to persistent history
		_persistentHistory.AddUserMessage(input);
		_persistentHistory.AddAssistantMessage(finalText);
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
		}
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
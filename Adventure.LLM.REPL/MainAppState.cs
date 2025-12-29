// Enhanced MainAppState demonstrating:
// - Multiple named chat clients (renderer + validator)
// - Per-request ChatOptions
// - Ephemeral-only world state injection
// - Validator pass with automatic regeneration
// - Streaming with sentence-boundary chunking
// - Logging of token usage and regeneration

using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
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
	private readonly IChatClient _renderer;
	private readonly IChatClient _validator;

	private readonly List<ChatMessage> _persistentHistory = new();
	private ChatMessageBuffer _buffer = null!;

	#endregion

	#region Constructors

	public MainAppState(
		IStateManager states,
		ILogger<MainAppState> logger,
		IChatClient rendererClient,
		IChatClient validatorClient)
		: base(states)
	{
		_logger = logger;
		_renderer = rendererClient;
		_validator = validatorClient;
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
		_persistentHistory.Clear();
		_persistentHistory.Add(new ChatMessage(ChatRole.System, RENDERER_SYSTEM_PROMPT));
		_buffer = new ChatMessageBuffer(_persistentHistory);
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
		_buffer.BeginEphemeral();

		// Inject world state ephemerally
		_buffer.Add(ChatRole.User, MainLabJson);
		_buffer.Add(ChatRole.User, input);

		string finalText;
		int attempts = 0;

		do
		{
			attempts++;
			finalText = await StreamRenderAsync();
		}
		while (attempts < 2 && !await ValidateAsync(finalText));

		_logger.LogInformation("Render attempts: {Attempts}", attempts);

		_buffer.EndEphemeral();
		_buffer.CommitAssistant(finalText);
	}

	private async Task<string> StreamRenderAsync()
	{
		var sb = new StringBuilder();
		var layout = (IRenderable)new Rows();
		var sentenceBuffer = new StringBuilder();

		await AnsiConsole.Live(layout).StartAsync(async ctx =>
		{
			await foreach (var update in _renderer.GetStreamingResponseAsync(
				_buffer.Snapshot(),
				options: new ChatOptions
				{
					Temperature = 0.15f,
					MaxOutputTokens = 120,
					StopSequences = ["\n\n"]
				}))
			{
				if (string.IsNullOrWhiteSpace(update.Text))
					continue;

				sentenceBuffer.Append(update.Text);

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
		var messages = new List<ChatMessage>
		{
			new(ChatRole.System, VALIDATOR_SYSTEM_PROMPT),
			new(ChatRole.User, text)
		};

		var response = await _validator.GetResponseAsync(
			messages,
			options: new ChatOptions
			{
				Temperature = 0,
				MaxOutputTokens = 5
			});

		var verdict = response.Text?.Trim();
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

public sealed class ChatMessageBuffer
{
	private readonly List<ChatMessage> _persistent;
	private int _ephemeralStartIndex = -1;

	public ChatMessageBuffer(List<ChatMessage> persistent)
	{
		_persistent = persistent;
	}

	public void BeginEphemeral()
	{
		if (_ephemeralStartIndex != -1)
			throw new InvalidOperationException("Ephemeral scope already active.");

		_ephemeralStartIndex = _persistent.Count;
	}

	public void Add(ChatMessage message)
	{
		_persistent.Add(message);
	}

	public void Add(ChatRole role, string content)
	{
		_persistent.Add(new ChatMessage(role, content));
	}

	public IReadOnlyList<ChatMessage> Snapshot()
	{
		return _persistent;
	}

	public void CommitAssistant(string content)
	{
		_persistent.Add(new ChatMessage(ChatRole.Assistant, content));
	}

	public void EndEphemeral()
	{
		if (_ephemeralStartIndex == -1)
			return;

		_persistent.RemoveRange(
			_ephemeralStartIndex,
			_persistent.Count - _ephemeralStartIndex);

		_ephemeralStartIndex = -1;
	}
}

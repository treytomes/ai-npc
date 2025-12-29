
using System.Text;
using Adventure.LLM.OllamaRuntime;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using OllamaSharp.Models.Chat;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.LLM.REPL;

internal class MainAppState : AppState
{
	#region Constructors

	private const string SYSTEM_PROMPT = @"
You are an environment description renderer for a text adventure game.

Your task:
- Generate a short description of the player’s current location.
- Use ONLY the information provided in the input JSON.
- Do NOT invent, infer, or assume any facts.

Rules:
1. Do not introduce objects, characters, exits, or events not listed.
2. Do not explain game mechanics, flags, or conditions.
3. Do not resolve mysteries or provide story conclusions.
4. Use second-person, present tense.
5. Favor concrete sensory details (sight, sound, smell).
6. If information is conditional, include it only when the condition is true.
7. If something is powered off, inactive, or sealed, describe it as such.
8. Do not mention flags, IDs, or JSON structure.
9. Output ONE short paragraph (3–5 sentences).
10. Do not include lists, bullet points, or dialogue.

Tone and style:
- Calm, neutral, slightly tense.
- Observational, not emotional.
- No metaphors unless directly supported by the data.

Failure handling:
- If required information is missing or unclear, describe less, not more.
- If no dynamic conditions apply, rely on static features and ambient details.

Remember:
You are a renderer, not a storyteller.
	";

	private const string MAIN_LAB_DESCRIPTION = @"
{
  ""room"": {
    ""id"": ""main_lab"",
    ""name"": ""Main Laboratory"",
    ""purpose"": ""tutorial_hub"",
    ""baseline_tone"": [""sterile"", ""abandoned"", ""tense""],

    ""spatial_summary"": {
      ""shape"": ""rectangular"",
      ""size"": ""medium"",
      ""lighting"": ""flickering_overhead"",
      ""smell"": [""ozone"", ""cleaning_agent""]
    },

    ""player_entry"": {
      ""first_time_only"": true,
      ""beats"": [
        ""You arrive in a laboratory clearly abandoned in a hurry."",
        ""The lights stutter overhead, briefly plunging the room into shadow.""
      ]
    },

    ""static_features"": [
      {
        ""id"": ""workbench"",
        ""type"": ""furniture"",
        ""salience"": ""high"",
        ""affordances"": [""look"", ""examine""],
        ""facts"": {
          ""material"": ""steel"",
          ""condition"": ""recently_used"",
          ""details"": [
            ""scattered instruments"",
            ""partially wiped surface"",
            ""dried residue""
          ]
        }
      },
      {
        ""id"": ""sealed_door"",
        ""type"": ""exit"",
        ""salience"": ""high"",
        ""affordances"": [""look"", ""open"", ""use""],
        ""facts"": {
          ""leads_to"": ""containment"",
          ""lock_type"": ""powered_card_reader"",
          ""power_required"": true
        }
      },
      {
        ""id"": ""terminal"",
        ""type"": ""device"",
        ""salience"": ""medium"",
        ""affordances"": [""look"", ""use"", ""activate""],
        ""facts"": {
          ""powered"": false,
          ""screen_state"": ""dark"",
          ""card_reader_attached"": true
        }
      }
    ],

    ""dynamic_descriptions"": [
      {
        ""when"": {
          ""flag"": ""terminal_powered"",
          ""equals"": false
        },
        ""include"": [
          ""A terminal sits against the far wall, its screen dark.""
        ]
      },
      {
        ""when"": {
          ""flag"": ""terminal_powered"",
          ""equals"": true
        },
        ""include"": [
          ""The terminal hums softly, lines of dormant text glowing on its screen.""
        ]
      }
    ],

    ""ambient_details"": {
      ""always"": [
        ""A low electrical hum vibrates through the floor.""
      ],
      ""conditional"": [
        {
          ""when"": {
            ""flag"": ""door_unsealed"",
            ""equals"": false
          },
          ""text"": ""A sealed door dominates one wall, its reader lifeless.""
        },
        {
          ""when"": {
            ""flag"": ""door_unsealed"",
            ""equals"": true
          },
          ""text"": ""The door to containment stands unlocked, its seal broken.""
        }
      ]
    },

    ""exits"": [
      {
        ""direction"": ""west"",
        ""target"": ""storage"",
        ""visibility"": ""obvious""
      },
      {
        ""direction"": ""east"",
        ""target"": ""observation"",
        ""visibility"": ""obvious""
      }
    ],

    ""llm_instructions"": {
      ""role"": ""environment_renderer"",
      ""constraints"": [
        ""Do not introduce objects not listed"",
        ""Do not resolve mysteries"",
        ""Do not reference game mechanics or flags"",
        ""Favor sensory details over exposition"",
        ""Maintain second-person present tense""
      ],
      ""output_length"": ""short_paragraph""
    }
  }
}
	";

	#endregion

	#region Fields

	private readonly ILogger<MainAppState> _logger;
	private readonly OllamaRepo _ollamaRepo;
	private Chat _chat = null!;

	#endregion

	#region Constructors

	public MainAppState(IStateManager states, ILogger<MainAppState> logger, OllamaRepo ollamaRepo)
		: base(states)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_ollamaRepo = ollamaRepo ?? throw new ArgumentNullException(nameof(ollamaRepo));
	}

	#endregion

	#region Methods

	public override async Task OnEnterAsync()
	{
		RenderHeader();
		await Task.CompletedTask;
	}

	public override async Task OnLeaveAsync()
	{
		await Task.CompletedTask;
	}

	public override async Task OnLoadAsync()
	{
		_chat = _ollamaRepo.CreateChat(SYSTEM_PROMPT);
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

		await ProcessInputAsync(input);
	}

	private void RenderHeader()
	{
		AnsiConsole.Clear();

		AnsiConsole.Write(
			new FigletText("Adventure.LLM")
				.Color(Color.Cyan));

		AnsiConsole.MarkupLine(
			"[grey]Type natural language commands. Use :help for options.[/]");
		AnsiConsole.WriteLine();
	}

	private void RenderHelp()
	{
		var table = new Table()
			.AddColumn("Command")
			.AddColumn("Description");

		table.AddRow(":help", "Show this help");
		table.AddRow(":exit", "Exit REPL");
		table.AddRow(":clear", "Clear screen");

		AnsiConsole.Write(table);
	}

	private string ReadInput()
	{
		return AnsiConsole.Prompt(
			new TextPrompt<string>("[bold green]>[/] ")
				.AllowEmpty());
	}

	private async Task ProcessInputAsync(string input)
	{
		AnsiConsole.WriteLine();

		try
		{
			var isSystemCommand = input.TrimStart().First() == ':';

			if (isSystemCommand)
			{
				await EvaluateSystemCommandAsync(input);
				return;
			}

			await EvaluateLLMResponseAsync(input);
		}
		catch (Exception ex)
		{
			AnsiConsole.WriteException(ex);
		}

		AnsiConsole.WriteLine();
	}

	private async Task EvaluateLLMResponseAsync(string input)
	{
		var cancellationToken = CancellationToken.None;
		var actorName = "Narrator";

		// Create a layout to organize the output.
		IRenderable layout = new Rows();

		var responseBuilder = new StringBuilder();
		var infoItems = new List<IRenderable>();

		var roomDescription = new Message(ChatRole.User, MAIN_LAB_DESCRIPTION);
		_chat.Messages.Add(roomDescription);

		await AnsiConsole.Live(layout)
			.StartAsync(async ctx =>
			{
				await foreach (var chunk in _chat!.SendAsync(input, cancellationToken))
				{
					if (chunk == null) continue;

					responseBuilder.Append(chunk);

					var responsePanel = new Panel(responseBuilder.ToString())
						.Header($"[green]{actorName}[/]")
						.Border(BoxBorder.Rounded)
						.BorderColor(Color.Green);

					// Update the layout.
					layout = responsePanel;
					ctx.UpdateTarget(layout);
				}
			});

		// AnsiConsole.WriteLine();
		_chat.Messages.Remove(roomDescription);
	}

	private async Task EvaluateSystemCommandAsync(string input)
	{
		switch (input)
		{
			case ":exit":
				await LeaveAsync();
				break;
			case ":help":
				RenderHelp();
				break;
			case ":clear":
				RenderHeader();
				break;
		}
	}

	#endregion
}
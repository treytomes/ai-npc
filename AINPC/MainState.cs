using AINPC.Entities;
using AINPC.OllamaRuntime;
using AINPC.Tools;
using AINPC.ValueObjects;
using Microsoft.Extensions.Logging;
using OllamaSharp;
using Spectre.Console;

namespace AINPC;

class MainState : AppState
{
	#region Fields

	private readonly ILogger<MainState> _logger;
	private readonly OllamaRepo _ollamaRepo;

	private readonly CharacterFactory _characters;
	private readonly VillageFactory _villages;
	private readonly RoleFactory _roles;
	private readonly ToolFactory _tools;
	private readonly ItemFactory _items;
	private readonly ActorFactory _actors;
	private readonly IIntentClassifier _intentClassifier;

	private Actor _actor;

	#endregion

	#region Constructors

	public MainState(
		IStateManager states,
		ILogger<MainState> logger,
		OllamaRepo ollamaRepo)
		: base(states)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_ollamaRepo = ollamaRepo ?? throw new ArgumentNullException(nameof(ollamaRepo));

		_characters = new();
		_villages = new();
		_roles = new(_characters, _villages);
		_tools = new();
		_items = new();
		_intentClassifier = new SimpleIntentClassifier();
		_actors = new(_roles, _tools, _items, _intentClassifier);

		// _actor = _actors.CreateGatekeeper();
		_actor = _actors.CreateShopkeeperPrompt();
	}

	#endregion

	#region Methods

	public override async Task LoadAsync()
	{
		await _actor.LoadAsync(_ollamaRepo);

		AnsiConsole.MarkupLine("[bold]Type your message. Press ENTER on an empty line to quit.[/]\n");
	}

	public override async Task UnloadAsync()
	{
		await _actor.UnloadAsync();
	}

	public override async Task UpdateAsync()
	{
		var userMsg = AnsiConsole.Prompt(
			new TextPrompt<string>("[cyan]You:[/] ")
				.AllowEmpty());

		if (string.IsNullOrWhiteSpace(userMsg))
		{
			await LeaveAsync();
			return;
		}

		AnsiConsole.Markup($"[green]{_actor.Name}:[/] ");

		await foreach (var token in await _actor.ChatAsync(userMsg))
		{
			// Stream tokens directly to console.
			AnsiConsole.Write(token);
		}

		AnsiConsole.WriteLine();
	}

	#endregion
}

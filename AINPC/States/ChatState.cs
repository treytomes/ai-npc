using AINPC.Entities;
using AINPC.Factories;
using AINPC.Intent.Classification;
using AINPC.OllamaRuntime;
using AINPC.Tools;
using Microsoft.Extensions.Logging;
using Spectre.Console;

namespace AINPC.States;

class ChatState : AppState
{
	#region Fields

	private readonly ILogger<ChatState> _logger;
	private readonly OllamaRepo _ollamaRepo;

	private readonly CharacterFactory _characters;
	private readonly VillageFactory _villages;
	private readonly RoleFactory _roles;
	private readonly ToolFactory _tools;
	private readonly ItemFactory _items;
	private readonly ActorFactory _actors;
	private readonly IIntentEngine<Actor> _intentEngine;
	private readonly IItemResolver _itemResolver;

	private Actor _actor;

	#endregion

	#region Constructors

	public ChatState(IStateManager states, ILogger<ChatState> logger, OllamaRepo ollamaRepo)
		: base(states)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
		_ollamaRepo = ollamaRepo ?? throw new ArgumentNullException(nameof(ollamaRepo));

		_characters = new();
		_villages = new();
		_roles = new(_villages);
		_tools = new();
		_items = new();
		_intentEngine = new IntentEngine();
		_itemResolver = new ItemResolver();
		_actors = new(_characters, _roles, _tools, _items, _intentEngine, _itemResolver);

		// _actor = _actors.CreateGatekeeper();
		_actor = _actors.CreateShopkeeperPrompt();
	}

	#endregion

	#region Methods

	public override async Task OnLoadAsync()
	{
		await _actor.LoadAsync(_ollamaRepo);

		AnsiConsole.MarkupLine("[bold]Type your message. Press ENTER on an empty line to quit.[/]\n");
	}

	public override async Task OnUnloadAsync()
	{
		await _actor.UnloadAsync();
	}

	public override async Task OnEnterAsync()
	{
		await Task.CompletedTask;
	}

	public override async Task OnLeaveAsync()
	{
		await Task.CompletedTask;
	}

	public override async Task OnUpdateAsync()
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

using System.Text;
using Adventure.Entities;
using Adventure.Enums;
using Adventure.Factories;
using Adventure.Intent.Classification;
using Adventure.OllamaRuntime;
using Adventure.Renderables;
using Adventure.Tools;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.States;

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

		// Create a layout to organize the output
		IRenderable layout = new Rows();

		var responseBuilder = new StringBuilder();
		var infoItems = new List<IRenderable>();

		await AnsiConsole.Live(layout)
			.StartAsync(async ctx =>
			{
				await foreach (var chunk in _actor.ChatAsync(userMsg))
				{
					switch (chunk)
					{
						case RuleChunk ruleChunk:
							infoItems.AddRange(new BulletListPanelRenderable("Processing Rules", ruleChunk.FiredRules));
							break;

						case IntentChunk intentChunk:
							var bestIntent = intentChunk.Intents
								.OrderByDescending(x => x.Confidence)
								.FirstOrDefault();

							if (bestIntent != null)
							{
								infoItems.AddRange(new IntentPanelRenderable(bestIntent, userMsg, true));
							}
							break;

						case ItemResolutionChunk itemChunk:
							// You could add a visual representation of item resolution
							if (itemChunk.Result.Status == ItemResolutionStatus.Ambiguous)
							{
								var itemPanel = new Panel($"Multiple items match: {string.Join(", ", itemChunk.Result.Candidates.Select(i => i.Name))}")
									.Header("[yellow]Item Resolution[/]")
									.Border(BoxBorder.Rounded)
									.BorderColor(Color.Yellow);
								infoItems.Add(itemPanel);
							}
							break;

						case ToolResultChunk toolChunk:
							// Display tool execution results.
							var toolPanel = new Panel($"{toolChunk.ToolName}: {toolChunk.Result}")
								.Header("[blue]Tool Executed[/]")
								.Border(BoxBorder.Rounded)
								.BorderColor(Color.Blue);
							infoItems.Add(toolPanel);
							break;

						case TextChunk textChunk:
							responseBuilder.Append(textChunk.Text);
							break;
					}

					var responsePanel = new Panel(responseBuilder.ToString())
						.Header($"[green]{_actor.Name}[/]")
						.Border(BoxBorder.Rounded)
						.BorderColor(Color.Green);

					// Update the layout.
					if (infoItems.Any())
					{
						layout = new Rows(infoItems.Concat([responsePanel]));
						// layout["Info"].Update(new Rows(infoItems));
					}
					else
					{
						layout = responsePanel;
					}
					// .Expand();

					// layout["Response"].Update(responsePanel);
					ctx.UpdateTarget(layout);
				}
			});

		AnsiConsole.WriteLine();
	}

	#endregion
}

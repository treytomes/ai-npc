using AINPC.Entities;
using AINPC.Factories;
using AINPC.Intent.Classification;
using AINPC.Intent.Classification.Facts;
using AINPC.Renderables;
using AINPC.Tools;
using AINPC.ValueObjects;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace AINPC.States;

internal class ShopkeeperIntentState : AppState
{
	#region Constants

	private const int MAX_QUERY_LENGTH = 200;
	private const int MAX_INTENTS_TO_SHOW = 3;

	#endregion

	#region Fields

	private readonly ILogger<ShopkeeperIntentState> _logger;

	private readonly CharacterFactory _characters;
	private readonly VillageFactory _villages;
	private readonly RoleFactory _roles;
	private readonly ToolFactory _tools;
	private readonly ItemFactory _items;
	private readonly ActorFactory _actors;
	private readonly IIntentEngine<Actor> _intentEngine;
	private readonly IItemResolver _itemResolver;

	private IntentEngine? _engine;
	private Actor _actor;
	private RecentIntent? _recentIntent = null;
	private Panel _helpPanel;

	#endregion

	#region Constructors

	public ShopkeeperIntentState(IStateManager states, ILogger<ShopkeeperIntentState> logger)
		: base(states)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		_characters = new();
		_villages = new();
		_roles = new(_villages);
		_tools = new();
		_items = new();
		_intentEngine = new IntentEngine();
		_itemResolver = new ItemResolver();
		_actors = new(_characters, _roles, _tools, _items, _intentEngine, _itemResolver);

		_actor = _actors.CreateShopkeeperPrompt();

		_helpPanel = new Panel(
			"Ask about:\n" +
			"• Item prices: \"How much is the sword?\"\n" +
			"• Item availability: \"Do you have any potions?\"\n" +
			"• Item details: \"Tell me about the cheese\"\n" +
			"• Buying items: \"I want to buy bread\"\n" +
			"• Type 'help' for this message or 'back' to exit")
			.Header("[cyan]How to interact[/]")
			.Border(BoxBorder.Rounded);
	}

	#endregion

	#region Methods

	public override async Task OnLoadAsync()
	{
		await Task.CompletedTask;
	}

	public override async Task OnUnloadAsync()
	{
		await Task.CompletedTask;
	}

	public override async Task OnEnterAsync()
	{
		AnsiConsole.Clear();
		AnsiConsole.Write(
			new Rule("[yellow]Shopkeeper Intent Classification[/]")
				.RuleStyle("grey")
				.LeftJustified());

		_engine = await InitializeIntentEngine();

		DisplayInventory(_actor);

		AnsiConsole.Write(_helpPanel);

		await Task.CompletedTask;
	}

	public override async Task OnLeaveAsync()
	{
		AnsiConsole.MarkupLine("[yellow]Thanks for visiting the shop![/]");

		await Task.CompletedTask;
	}

	public override async Task OnUpdateAsync()
	{
		if (_engine == null || _actor == null) throw new InvalidOperationException("State is not initialized.");

		var query = AnsiConsole.Prompt(
			new TextPrompt<string>($"[blue]Ask the shopkeeper:[/]")
				.AllowEmpty()
				.ValidationErrorMessage($"[red]Query too long! Maximum {MAX_QUERY_LENGTH} characters.[/]")
				.Validate(q => q?.Length <= MAX_QUERY_LENGTH));

		if (string.IsNullOrWhiteSpace(query) || query.Equals("back", StringComparison.OrdinalIgnoreCase))
		{
			await LeaveAsync();
			return;
		}

		if (query.Equals("help", StringComparison.OrdinalIgnoreCase))
		{
			AnsiConsole.Write(_helpPanel);
			return;
		}

		try
		{
			var result = await ProcessQuery(_engine, _actor, query, _recentIntent);
			_recentIntent = result.UpdatedRecentIntent;
			DisplayResults(query, result);
		}
		catch (Exception ex)
		{
			AnsiConsole.MarkupLine($"[red]Error processing query: {ex.Message}[/]");
		}

		AnsiConsole.WriteLine();
	}

	private async Task<IntentEngineResult> ProcessQuery(IntentEngine engine, Actor actor, string query, RecentIntent? recentIntent)
	{
		return await AnsiConsole.Status()
			.Spinner(Spinner.Known.Dots2)
			.StartAsync("Thinking...", async ctx =>
			{
				var result = await engine.ProcessAsync(
					query,
					actor,
					new IntentEngineContext
					{
						RecentIntent = recentIntent
					});

				await Task.Delay(100); // Brief delay for realism.
				return result;
			});
	}

	private void DisplayResults(string query, IntentEngineResult result)
	{
		// Display fired rules if any
		if (result.FiredRules.Any())
		{
			new BulletListPanelRenderable("Processing Rules", result.FiredRules).Render();
		}

		// Handle no intents found
		if (!result.Intents.Any())
		{
			var responses = new[]
			{
				"I don't understand what you're asking about.",
				"Could you rephrase that?",
				"I'm not sure what you mean.",
				"Sorry, I didn't catch that."
			};

			var response = responses[Math.Abs(query.GetHashCode()) % responses.Length];
			TextRenderable.Error(response).Render();
			return;
		}

		// Display intents (best first)
		var intentsToShow = result.Intents
			.OrderByDescending(x => x.Confidence)
			.Take(MAX_INTENTS_TO_SHOW)
			.ToList();

		var bestIntent = intentsToShow.First();

		// Show primary intent.
		new IntentPanelRenderable(bestIntent, query, true).Render();

		// Show alternative interpretations if confidence is low
		if (bestIntent.Confidence < 0.8 && intentsToShow.Count > 1)
		{
			AnsiConsole.MarkupLine("\n[grey]Alternative interpretations:[/]");

			foreach (var intent in intentsToShow.Skip(1))
			{
				new IntentPanelRenderable(intent, query, false).Render();
			}
		}

		// Generate shopkeeper response
		var shopkeeperResponse = GenerateResponse(bestIntent);
		if (!string.IsNullOrEmpty(shopkeeperResponse))
		{
			var responsePanel = new Panel(shopkeeperResponse)
				.Header("[green]Shopkeeper says:[/]")
				.Border(BoxBorder.Rounded)
				.BorderColor(Color.Green);

			AnsiConsole.WriteLine();
			AnsiConsole.Write(responsePanel);
		}
	}

	private string GenerateResponse(Intent.Classification.Facts.Intent intent)
	{
		return intent.Name switch
		{
			"item.check.price" when intent.Slots.TryGetValue("item", out var item) =>
				$"Ah, the {item}? That'll be {Random.Shared.Next(10, 100)} gold pieces.",

			"item.check.availability" when intent.Slots.TryGetValue("item", out var item) =>
				$"Let me check... Yes, I have {item} in stock!",

			"item.buy" when intent.Slots.TryGetValue("item", out var item) =>
				$"Excellent choice! One {item} coming right up.",

			"item.describe" when intent.Slots.TryGetValue("item", out var item) =>
				$"The {item} is one of our finest products. Very popular with adventurers!",

			"greeting" =>
				"Welcome to my shop! How can I help you today?",

			"farewell" =>
				"Thank you for visiting! Safe travels!",

			_ =>
				"I don't know what you mean.",
		};
	}

	private async Task<IntentEngine> InitializeIntentEngine()
	{
		return await AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.StartAsync("Loading shopkeeper demo...", async ctx =>
			{
				ctx.Status("Loading inventory...");
				var inventory = new ItemFactory().GetGeneralStoreItems();

				if (inventory == null || !inventory.Any())
				{
					throw new InvalidOperationException("Failed to load inventory items");
				}

				ctx.Status("Initializing intent engine...");
				var engine = new IntentEngine();

				ctx.Status("Ready!");
				await Task.Delay(300); // Brief pause for visibility.

				return engine;
			});
	}

	private void DisplayInventory(Actor actor)
	{
		// Categorize items properly
		var categories = CategorizeItems(actor.Inventory);

		var grid = new Grid();

		// Add columns for each category
		foreach (var _ in categories)
		{
			grid.AddColumn();
		}

		// Add headers
		var headers = categories.Select(c =>
			new Panel($"[{GetCategoryColor(c.Key)}]{c.Key}[/]")
				.Border(BoxBorder.Rounded))
			.ToArray();
		grid.AddRow(headers);

		// Add items
		var itemLists = categories.Select(c =>
		{
			var items = c.Value.Take(10).Select(i => $"• {i.Name}");
			var itemText = string.Join("\n", items);

			if (c.Value.Count > 10)
			{
				itemText += $"\n[grey]...and {c.Value.Count - 10} more[/]";
			}

			return itemText;
		}).ToArray();

		grid.AddRow(itemLists);

		AnsiConsole.Write(grid);
		AnsiConsole.WriteLine();
		AnsiConsole.MarkupLine($"[grey]Total items in inventory: {actor.Inventory.Count}[/]");
		AnsiConsole.WriteLine();
	}

	private Dictionary<string, List<ItemInfo>> CategorizeItems(IReadOnlyList<ItemInfo> inventory)
	{
		var categories = new Dictionary<string, List<ItemInfo>>();

		foreach (var item in inventory)
		{
			var category = DetermineCategory(item);

			if (!categories.ContainsKey(category))
			{
				categories[category] = new List<ItemInfo>();
			}

			categories[category].Add(item);
		}

		// Sort categories by item count
		return categories.OrderByDescending(c => c.Value.Count)
						.Take(3) // Show top 3 categories
						.ToDictionary(c => c.Key, c => c.Value);
	}

	private string DetermineCategory(ItemInfo item)
	{
		var name = item.Name.ToLowerInvariant();

		if (name.Contains("sword") || name.Contains("axe") || name.Contains("bow") ||
			name.Contains("dagger") || name.Contains("mace"))
			return "Weapons";

		if (name.Contains("cheese") || name.Contains("bread") || name.Contains("meat") ||
			name.Contains("apple") || name.Contains("water"))
			return "Food & Drink";

		if (name.Contains("armor") || name.Contains("shield") || name.Contains("helm") ||
			name.Contains("boots") || name.Contains("gloves"))
			return "Armor";

		if (name.Contains("potion") || name.Contains("elixir") || name.Contains("salve"))
			return "Potions";

		return "General Goods";
	}

	private string GetCategoryColor(string category) => category switch
	{
		"Weapons" => "red",
		"Food & Drink" => "yellow",
		"Armor" => "blue",
		"Potions" => "green",
		_ => "white"
	};

	#endregion
}
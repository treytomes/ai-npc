using LLM.Intent.Classification;
using LLM.Intent.Classification.Facts;
using LLM.Intent.Entities;
using LLM.Intent.Items.Factories;
using LLM.Intent.Items.ValueObjects;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace LLM.Intent;

internal static class ShopkeeperDemo
{
	private const int MaxQueryLength = 200;
	private const int MaxIntentsToShow = 3;

	public static async Task<int> RunAsync()
	{
		try
		{
			AnsiConsole.Clear();
			AnsiConsole.Write(
				new Rule("[yellow]Shopkeeper Intent Classification[/]")
					.RuleStyle("grey")
					.LeftJustified());

			var (engine, actor) = await InitializeShopkeeper();

			DisplayInventory(actor);

			await RunInteractiveSession(engine, actor);

			return 0;
		}
		catch (Exception ex)
		{
			AnsiConsole.WriteException(ex);
			return 1;
		}
	}

	private static async Task<(IntentEngine, Actor)> InitializeShopkeeper()
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

				var actor = new Actor
				{
					Role = "shopkeeper",
					Inventory = inventory,
				};

				ctx.Status("Initializing intent engine...");
				var engine = new IntentEngine();

				ctx.Status("Ready!");
				await Task.Delay(300); // Brief pause for visibility

				return (engine, actor);
			});
	}

	private static void DisplayInventory(Actor actor)
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

	private static Dictionary<string, List<ItemInfo>> CategorizeItems(IReadOnlyList<ItemInfo> inventory)
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

	private static string DetermineCategory(ItemInfo item)
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

	private static string GetCategoryColor(string category) => category switch
	{
		"Weapons" => "red",
		"Food & Drink" => "yellow",
		"Armor" => "blue",
		"Potions" => "green",
		_ => "white"
	};

	private static async Task RunInteractiveSession(IntentEngine engine, Actor actor)
	{
		RecentIntent? recentIntent = null;
		var queryCount = 0;
		const int maxQueries = 50;

		var helpPanel = new Panel(
			"Ask about:\n" +
			"• Item prices: \"How much is the sword?\"\n" +
			"• Item availability: \"Do you have any potions?\"\n" +
			"• Item details: \"Tell me about the cheese\"\n" +
			"• Buying items: \"I want to buy bread\"\n" +
			"• Type 'help' for this message or 'back' to exit")
			.Header("[cyan]How to interact[/]")
			.Border(BoxBorder.Rounded);

		AnsiConsole.Write(helpPanel);

		while (queryCount < maxQueries)
		{
			var query = AnsiConsole.Prompt(
				new TextPrompt<string>($"[blue]Ask the shopkeeper ({queryCount + 1}/{maxQueries}):[/]")
					.AllowEmpty()
					.ValidationErrorMessage($"[red]Query too long! Maximum {MaxQueryLength} characters.[/]")
					.Validate(q => q?.Length <= MaxQueryLength));

			if (string.IsNullOrWhiteSpace(query) || query.Equals("back", StringComparison.OrdinalIgnoreCase))
				break;

			if (query.Equals("help", StringComparison.OrdinalIgnoreCase))
			{
				AnsiConsole.Write(helpPanel);
				continue;
			}

			queryCount++;

			try
			{
				var result = await ProcessQuery(engine, actor, query, recentIntent);
				recentIntent = result.UpdatedRecentIntent;
				DisplayResults(query, result);
			}
			catch (Exception ex)
			{
				AnsiConsole.MarkupLine($"[red]Error processing query: {ex.Message}[/]");
			}

			AnsiConsole.WriteLine();
		}

		if (queryCount >= maxQueries)
		{
			AnsiConsole.MarkupLine($"[yellow]Maximum number of queries ({maxQueries}) reached.[/]");
		}

		AnsiConsole.MarkupLine("[yellow]Thanks for visiting the shop![/]");
	}

	private static async Task<IntentEngineResult> ProcessQuery(IntentEngine engine, Actor actor, string query, RecentIntent? recentIntent)
	{
		return await AnsiConsole.Status()
			.Spinner(Spinner.Known.Dots2)
			.StartAsync("Thinking...", async ctx =>
			{
				var result = engine.Process(
					query,
					actor,
					new IntentEngineContext
					{
						RecentIntent = recentIntent
					});

				await Task.Delay(100); // Brief delay for realism
				return result;
			});
	}

	private static void DisplayResults(string query, IntentEngineResult result)
	{
		// Display fired rules if any
		if (result.FiredRules.Any())
		{
			var rulePanel = new Panel(
				string.Join("\n", result.FiredRules.Select(r => $"• {r}")))
				.Header("[cyan]Processing Rules[/]")
				.Border(BoxBorder.Rounded)
				.BorderColor(Color.Cyan1);

			AnsiConsole.Write(rulePanel);
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
			AnsiConsole.MarkupLine($"[red]{response}[/]");
			return;
		}

		// Display intents (best first)
		var intentsToShow = result.Intents
			.OrderByDescending(x => x.Confidence)
			.Take(MaxIntentsToShow)
			.ToList();

		var bestIntent = intentsToShow.First();

		// Show primary intent
		var primaryPanel = CreateIntentPanel(bestIntent, query, isPrimary: true);
		AnsiConsole.Write(primaryPanel);

		// Show alternative interpretations if confidence is low
		if (bestIntent.Confidence < 0.8 && intentsToShow.Count > 1)
		{
			AnsiConsole.MarkupLine("\n[grey]Alternative interpretations:[/]");

			foreach (var intent in intentsToShow.Skip(1))
			{
				var altPanel = CreateIntentPanel(intent, query, isPrimary: false);
				AnsiConsole.Write(altPanel);
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

	private static Panel CreateIntentPanel(Classification.Facts.Intent intent, string query, bool isPrimary)
	{
		var confidence = intent.Confidence;
		var confidenceColor = confidence switch
		{
			>= 0.8 => "green",
			>= 0.5 => "yellow",
			_ => "red"
		};

		var rows = new List<IRenderable>
		{
			new Markup($"[blue]Intent:[/] {intent.Name}"),
			new Markup($"[{confidenceColor}]Confidence:[/] {confidence:P0}")
		};

		if (intent.Slots.Any())
		{
			rows.Add(new Rule().RuleStyle("grey"));
			rows.Add(new Markup("[blue]Extracted Information:[/]"));

			var slotTable = new Table()
				.Border(TableBorder.None)
				.HideHeaders()
				.AddColumn("Key")
				.AddColumn("Value");

			foreach (var slot in intent.Slots.OrderBy(s => s.Key))
			{
				slotTable.AddRow(
					$"[grey]{slot.Key}:[/]",
					$"[yellow]{Markup.Escape(slot.Value)}[/]");
			}

			rows.Add(slotTable);
		}

		var title = isPrimary
			? $"[yellow]Understanding: \"{Markup.Escape(query)}\"[/]"
			: "[grey]Alternative[/]";

		return new Panel(new Rows(rows))
			.Header(title)
			.Border(BoxBorder.Rounded)
			.BorderColor(isPrimary ? Color.Yellow : Color.Grey)
			.Expand();
	}

	private static string GenerateResponse(Classification.Facts.Intent intent)
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
}
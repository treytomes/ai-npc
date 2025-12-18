using LLM.Intent.Classification;
using LLM.Intent.Classification.Facts;
using LLM.Intent.Entities;
using LLM.Intent.Items.Factories;
using Spectre.Console;

namespace LLM.Intent;

internal static class ShopkeeperDemo
{
	public static async Task<int> RunAsync()
	{
		RecentIntent? recentIntent = null;

		AnsiConsole.Clear();
		AnsiConsole.Write(
			new Rule("[yellow]Shopkeeper Intent Classification[/]")
				.RuleStyle("grey")
				.LeftJustified());

		var (engine, actor) = await AnsiConsole.Status()
			.Spinner(Spinner.Known.Star)
			.StartAsync("Loading shopkeeper demo...", async ctx =>
			{
				var inventory = new ItemFactory().GetGeneralStoreItems();

				var actor = new Actor
				{
					Role = "shopkeeper",
					Inventory = inventory,
				};

				var engine = new IntentEngine();

				ctx.Status("Ready!");
				await Task.Delay(500);
				return (engine, actor);
			});

		// Display inventory in a nice grid
		var grid = new Grid()
			.AddColumn()
			.AddColumn()
			.AddColumn();

		grid.AddRow(
			new Panel("[yellow]Food Items[/]").Border(BoxBorder.Rounded),
			new Panel("[cyan]Weapons[/]").Border(BoxBorder.Rounded),
			new Panel("[green]Other Items[/]").Border(BoxBorder.Rounded));

		var foodItems = actor.Inventory.Where(i => i.Name.Contains("Cheese") || i.Name.Contains("Bread")).ToList();
		var weaponItems = actor.Inventory.Where(i => i.Name.Contains("Sword") || i.Name.Contains("Axe")).ToList();
		var otherItems = actor.Inventory.Except(foodItems).Except(weaponItems).ToList();

		grid.AddRow(
			string.Join("\n", foodItems.Select(i => $"• {i.Name}")),
			string.Join("\n", weaponItems.Select(i => $"• {i.Name}")),
			string.Join("\n", otherItems.Select(i => $"• {i.Name}")));

		AnsiConsole.Write(grid);
		AnsiConsole.WriteLine();

		// Interactive query loop
		while (true)
		{
			var query = AnsiConsole.Prompt(
				new TextPrompt<string>("[blue]What would you like to know?[/] [grey](or 'back' to return)[/]")
					.AllowEmpty());

			if (string.IsNullOrWhiteSpace(query) || query.Equals("back", StringComparison.OrdinalIgnoreCase))
				break;

			var result = engine.Process(
				query,
				actor,
				new IntentEngineContext
				{
					RecentIntent = recentIntent
				});

			if (result.FiredRules.Any())
			{
				var rulePanel = new Panel(
					string.Join("\n", result.FiredRules.Select(r => $"• {r}")))
					.Header("[cyan]Rules Fired[/]")
					.Border(BoxBorder.Rounded);

				AnsiConsole.Write(rulePanel);
			}

			if (!result.Intents.Any())
			{
				AnsiConsole.MarkupLine("[red]I don't understand what you're asking about.[/]");
			}
			else
			{
				foreach (var intent in result.Intents.OrderBy(x => x.Confidence))
				{
					var intentPanel = new Panel(
						new Rows(
							new Markup($"[green]Intent:[/] {intent.Name}"),
							new Rule(),
							new Markup("[blue]Slots:[/]"),
							new Rows(intent.Slots.Select(s => new Markup($"  • {s.Key}: [yellow]{s.Value}[/]")))
						))
						.Header($"[yellow]Understanding: {query} ({intent.Confidence:P0})[/]")
						.Border(BoxBorder.Rounded)
						.Expand();

					AnsiConsole.Write(intentPanel);
				}

				var strongest = result.Intents
					.OrderByDescending(i => i.Confidence)
					.FirstOrDefault();

				recentIntent = result.UpdatedRecentIntent;
			}

			AnsiConsole.WriteLine();
		}

		return 0;
	}
}
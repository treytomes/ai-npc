using Spectre.Console;
using Spectre.Console.Rendering;

namespace AINPC.Renderables;

using Intent = Intent.Classification.Facts.Intent;

internal class IntentPanelRenderable(Intent intent, string query, bool isPrimary) : Renderable
{
	#region Properties

	public Intent Intent { get; init; } = intent;
	public string Query { get; init; } = query;
	public bool IsPrimary { get; init; } = isPrimary;

	private double Confidence => Intent.Confidence;
	private string ConfidenceColor => Confidence switch
	{
		>= 0.8 => "green",
		>= 0.5 => "yellow",
		_ => "red"
	};

	#endregion

	#region Methods

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		var rows = new List<IRenderable>
		{
			new Markup($"[blue]Intent:[/] {Intent.Name}"),
			new Markup($"[{ConfidenceColor}]Confidence:[/] {Confidence:P0}")
		};

		if (Intent.Slots.Any())
		{
			rows.Add(new Rule().RuleStyle("grey"));
			rows.Add(new Markup("[blue]Extracted Information:[/]"));

			var slotTable = new Table()
				.Border(TableBorder.None)
				.HideHeaders()
				.AddColumn("Key")
				.AddColumn("Value");

			foreach (var slot in Intent.Slots.OrderBy(s => s.Key))
			{
				slotTable.AddRow(
					$"[grey]{slot.Key}:[/]",
					$"[yellow]{Markup.Escape(slot.Value)}[/]");
			}

			rows.Add(slotTable);
		}

		var title = IsPrimary
			? $"[yellow]Understanding: \"{Markup.Escape(Query)}\"[/]"
			: "[grey]Alternative[/]";

		yield return new Panel(new Rows(rows))
			.Header(title)
			.Border(BoxBorder.Rounded)
			.BorderColor(IsPrimary ? Color.Yellow : Color.Grey)
			.Expand();
	}

	#endregion
}
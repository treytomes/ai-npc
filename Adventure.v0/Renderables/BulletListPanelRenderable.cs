using System.Collections;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.Renderables;

internal class BulletListPanelRenderable(string title, IReadOnlyList<string> items) : Renderables.Renderable
{
	#region Properties

	public string Title { get; init; } = title;
	public IReadOnlyList<string> Items { get; init; } = items;

	#endregion

	#region Methods

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		yield return new Panel(GetBulletedItems())
			.Header($"[cyan]{Title}[/]")
			.Border(BoxBorder.Rounded)
			.BorderColor(Color.Cyan1);
	}

	private string GetBulletedItems() => string.Join("\n", Items.Select(GetBulletedItem));

	private string GetBulletedItem(string item) => $"• {item}";

	#endregion
}
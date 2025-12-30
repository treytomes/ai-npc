using Adventure.LLM.REPL.ValueObjects;
using Adventure.Renderables;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.LLM.REPL.Renderables;

internal sealed class RoomsRenderable(Dictionary<string, WorldData> worldData, string currentRoom) : Adventure.Renderables.Renderable
{
	private readonly Dictionary<string, WorldData> _worldData = worldData ?? throw new ArgumentNullException(nameof(worldData));
	private readonly string _currentRoom = currentRoom ?? throw new ArgumentNullException(nameof(currentRoom));

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		var table = new Table()
			.Border(TableBorder.Rounded)
			.Title("[yellow]Available Rooms[/]")
			.AddColumn("[cyan]Key[/]")
			.AddColumn("[cyan]Name[/]")
			.AddColumn("[cyan]Features[/]");

		foreach (var (key, data) in _worldData)
		{
			var featureCount = data.Room.StaticFeatures.Count;
			var current = key == _currentRoom ? " [green](current)[/]" : "";
			table.AddRow(
				key + current,
				data.Room.Name,
				$"{featureCount} feature(s)"
			);
		}

		yield return table;
		yield return new NewLineRenderable();
	}
}
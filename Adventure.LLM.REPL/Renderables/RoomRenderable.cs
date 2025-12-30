using Adventure.LLM.REPL.ValueObjects;
using Adventure.Renderables;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.LLM.REPL.Renderables;

internal sealed class RoomRenderable(Room room) : Adventure.Renderables.Renderable
{
	private readonly Room _room = room ?? throw new ArgumentNullException(nameof(room));

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		yield return new Panel($"""
            [yellow]Name:[/] {room.Name}
            [yellow]Shape:[/] {room.SpatialSummary.Shape}
            [yellow]Size:[/] {room.SpatialSummary.Size}
            [yellow]Lighting:[/] {room.SpatialSummary.Lighting}
            [yellow]Smells:[/] {string.Join(", ", room.SpatialSummary.Smell)}
            [yellow]Features:[/] {room.StaticFeatures.Count}
            """)
			.Header($"[cyan]Room Details[/]")
			.Border(BoxBorder.Rounded)
			.BorderColor(Color.Cyan);

		yield return new NewLineRenderable();
	}
}
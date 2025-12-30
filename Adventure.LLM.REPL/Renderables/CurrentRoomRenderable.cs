using Adventure.LLM.REPL.ValueObjects;
using Adventure.Renderables;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.LLM.REPL.Renderables;

internal sealed class CurrentRoomRenderable(WorldData? worldData) : Adventure.Renderables.Renderable
{
	private readonly WorldData? _worldData = worldData;

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		if (_worldData == null)
		{
			yield return new Markup("Error: Current room data not found", new Style(Color.Red));
			yield return new NewLineRenderable();
			yield break;
		}

		var room = _worldData.Room;
		yield return new RoomRenderable(room);
	}
}
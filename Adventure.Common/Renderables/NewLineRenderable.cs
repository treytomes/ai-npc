
using Spectre.Console;

namespace Adventure.Renderables;

public sealed class NewLineRenderable : Renderable
{
	public override IEnumerator<Spectre.Console.Rendering.IRenderable> GetEnumerator()
	{
		yield return new Text(Environment.NewLine);
	}
}
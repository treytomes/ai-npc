using Spectre.Console;
using Spectre.Console.Rendering;
using System.Collections;

namespace Adventure.Renderables;

internal abstract class Renderable : IEnumerable<IRenderable>
{
	public void Render()
	{
		foreach (var item in this)
		{
			AnsiConsole.Write(item);
		}
	}

	public abstract IEnumerator<IRenderable> GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
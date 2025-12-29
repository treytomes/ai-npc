using Spectre.Console.Rendering;

namespace Adventure.Renderables;

public class SeparatorRenderable : Renderable
{
	private readonly int _count;

	public SeparatorRenderable(int count = 1)
	{
		_count = Math.Max(1, count);
	}

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		for (int i = 0; i < _count; i++)
		{
			yield return RenderingColors.EmptyLine();
		}
	}
}
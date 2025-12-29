using Spectre.Console.Rendering;

namespace Adventure.Renderables;

public class CompositeRenderable : Renderable
{
	private readonly IEnumerable<IRenderable> _renderables;

	public CompositeRenderable(params IRenderable[] renderables)
	{
		_renderables = renderables ?? throw new ArgumentNullException(nameof(renderables));
	}

	public CompositeRenderable(IEnumerable<IRenderable> renderables)
	{
		_renderables = renderables ?? throw new ArgumentNullException(nameof(renderables));
	}

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		foreach (var renderable in _renderables)
		{
			yield return renderable;
		}
	}
}
using Spectre.Console.Rendering;
using System.Collections;

namespace Adventure.Renderables;

public abstract class Renderable : IEnumerable<IRenderable>, IRenderable
{
	public abstract IEnumerator<IRenderable> GetEnumerator();

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	// IRenderable implementation
	public Measurement Measure(RenderOptions options, int maxWidth)
	{
		// Combine measurements of all components.
		var min = int.MaxValue;
		var max = 0;

		foreach (var renderable in this)
		{
			var measurement = renderable.Measure(options, maxWidth);
			min = Math.Min(min, measurement.Min);
			max += measurement.Max;
		}

		return new Measurement(min, max);
	}

	public IEnumerable<Segment> Render(RenderOptions options, int maxWidth)
	{
		foreach (var renderable in this)
		{
			foreach (var segment in renderable.Render(options, maxWidth))
			{
				yield return segment;
			}
		}
	}
}
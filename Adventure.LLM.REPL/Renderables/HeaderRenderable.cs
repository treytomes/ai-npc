using Adventure.Renderables;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.LLM.REPL.Renderables;

internal sealed class HeaderRenderable(string title, string subtext) : Adventure.Renderables.Renderable
{
	private Color _headerColor = Color.Cyan;
	private Color _subtextColor = Color.Grey;
	private readonly string _title = title ?? throw new ArgumentNullException(nameof(title));
	private readonly string _subtext = subtext ?? throw new ArgumentNullException(nameof(subtext));

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		yield return new FigletText(_title).Color(_headerColor);
		yield return new Markup(_subtext, new Style(_subtextColor));
		yield return new NewLineRenderable();
		yield return new NewLineRenderable();
	}
}
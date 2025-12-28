using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.Renderables;

internal class HeaderRenderable(string title, string subtitle) : Renderable
{
	#region Properties

	public string Title { get; init; } = title;
	public string Subtitle { get; init; } = subtitle;

	#endregion

	#region Methods

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		yield return new FigletText(Title)
			.Centered()
			.Color(Color.Blue);

		yield return new Rule($"[grey]{Subtitle}[/]")
			.RuleStyle("blue");

		yield return new Text("\n");
	}

	#endregion
}
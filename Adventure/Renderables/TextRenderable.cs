
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Adventure.Renderables;

internal class TextRenderable(string text) : Renderable
{
	#region Properties

	public string Text { get; init; } = text;
	public Color ForegroundColor { get; init; } = Color.Gray;
	public Color? BackgroundColor { get; init; } = null;

	public Decoration Decoration { get; init; } = Decoration.None;

	#endregion

	#region Methods

	public static TextRenderable Error(string text)
	{
		return new(text)
		{
			ForegroundColor = Color.Red,
		};
	}

	public override IEnumerator<IRenderable> GetEnumerator()
	{
		yield return new Text(Text, new Style(ForegroundColor, BackgroundColor, Decoration));
	}

	#endregion
}
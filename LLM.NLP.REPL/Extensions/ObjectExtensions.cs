using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

namespace LLM.NPL.REPL;

public static class ObjectExtensions
{
	public static string ToJson<T>(this T @this)
	{
		return JsonSerializer.Serialize(@this, new JsonSerializerOptions()
		{
			WriteIndented = true,
		});
	}

	public static IRenderable ToJsonRenderable<T>(this T @this)
	{
		var jsonText = @this.ToJson();
		return new JsonText(jsonText)
			.BracesColor(Color.Grey)
			.MemberColor(Color.CornflowerBlue)
			.StringColor(Color.Green)
			.NumberColor(Color.Aqua)
			.BooleanColor(Color.Magenta)
			.NullColor(Color.Grey);
	}
}
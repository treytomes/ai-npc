using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;

namespace Adventure;

public static class ObjectExtensions
{
	public static string ToJson<T>(this T @this, bool isCompact = false)
	{
		return JsonSerializer.Serialize(@this, new JsonSerializerOptions()
		{
			WriteIndented = !isCompact,
		});
	}

	public static IRenderable ToJsonRenderable<T>(this T @this, bool isCompact = false)
	{
		var jsonText = @this.ToJson(isCompact);
		return new JsonText(jsonText)
			.BracesColor(Color.Grey)
			.MemberColor(Color.CornflowerBlue)
			.StringColor(Color.Green)
			.NumberColor(Color.Aqua)
			.BooleanColor(Color.Magenta)
			.NullColor(Color.Grey);
	}
}
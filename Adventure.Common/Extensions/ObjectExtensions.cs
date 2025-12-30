using System.Text.Json;
using Spectre.Console;
using Spectre.Console.Json;
using Spectre.Console.Rendering;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

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

	public static string ToYaml<T>(this T @this)
	{
		var serializer = new SerializerBuilder()
			.WithNamingConvention(UnderscoredNamingConvention.Instance)
			.Build();

		return serializer.Serialize(@this);
	}
}
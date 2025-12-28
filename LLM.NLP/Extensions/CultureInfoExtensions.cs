using System.Globalization;

namespace LLM.NLP;

internal static class CultureInfoExtensions
{
	public static Mosaik.Core.Language ToMosaikLanguage(this CultureInfo @this)
	{
		return @this.TwoLetterISOLanguageName switch
		{
			"en" => Mosaik.Core.Language.English,
			_ => throw new InvalidCastException($"Unknown language: {@this.TwoLetterISOLanguageName}"),
		};
	}
}
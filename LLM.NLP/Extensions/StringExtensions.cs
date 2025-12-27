namespace LLM.NLP;

internal static class StringExtensions
{
	public static bool IsQuestionWord(this string @this) =>
		@this is "who" or "whom" or "whose" or "what" or "which"
			or "where" or "when" or "why" or "how";
}
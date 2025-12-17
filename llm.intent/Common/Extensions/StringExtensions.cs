namespace LLM.Intent;

internal static class StringExtensions
{
	public static string Join<T>(this IEnumerable<T> @this, string separator)
	{
		return string.Join(separator, @this.Select(x => x?.ToString() ?? "null"));
	}
}
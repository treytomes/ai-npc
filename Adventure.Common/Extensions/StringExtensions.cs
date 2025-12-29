namespace Adventure;

public static class StringExtensions
{
	public static bool ContainsAny(this string @this, params string[] strings)
	{
		foreach (var s in strings)
		{
			if (@this.Contains(s))
			{
				return true;
			}
		}
		return false;
	}

	public static string Join<T>(this IEnumerable<T> @this, string separator)
	{
		return string.Join(separator, @this.Select(x => x?.ToString() ?? "null"));
	}
}
namespace AINPC;

internal static class StringExtensions
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
}
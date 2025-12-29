namespace Adventure;

public static class DictionaryExtensions
{
	public static T? GetValueOrDefault<T>(this IReadOnlyDictionary<string, object?>? @this, string key, T? defaultValue = default, bool throwOnError = true)
	{
		try
		{
			if (@this == null || !@this.ContainsKey(key))
			{
				throw new NullReferenceException($"There is no key named '{key}'.");
			}
			return (T?)Convert.ChangeType(@this[key], typeof(T));
		}
		catch
		{
			if (throwOnError)
			{
				throw;
			}
			return defaultValue;
		}
	}

}
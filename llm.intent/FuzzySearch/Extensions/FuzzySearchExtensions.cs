namespace LLM.Intent.FuzzySearch;

/// <summary>
/// Provides extension methods for fuzzy search functionality.
/// </summary>
public static class FuzzySearchExtensions
{
	/// <summary>
	/// Creates a fuzzy search engine for a collection of strings.
	/// </summary>
	/// <param name="source">The source collection.</param>
	/// <param name="options">Optional search configuration.</param>
	/// <returns>A configured fuzzy search engine.</returns>
	public static IFuzzySearchEngine ToSearchEngine(this IEnumerable<string> source, SearchOptions? options = null)
	{
		return new FuzzySearchEngine(source, options);
	}

	/// <summary>
	/// Performs a fuzzy search on a collection of strings.
	/// </summary>
	/// <param name="source">The source collection.</param>
	/// <param name="query">The search query.</param>
	/// <param name="maxResults">Maximum number of results to return.</param>
	/// <returns>Matching items ordered by relevance.</returns>
	public static async Task<IEnumerable<string>> FuzzySearchAsync(this IEnumerable<string> source, string query, int maxResults = 10)
	{
		var engine = new FuzzySearchEngine(source);
		var results = await engine.SearchAsync(query);
		return results.Take(maxResults).Select(r => r.Text);
	}
}

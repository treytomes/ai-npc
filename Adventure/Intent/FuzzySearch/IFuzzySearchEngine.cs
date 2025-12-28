namespace Adventure.Intent.FuzzySearch;

/// <summary>
/// Provides fuzzy string search functionality using character vector similarity.
/// </summary>
public interface IFuzzySearchEngine
{
	/// <summary>
	/// Searches for items matching the specified query.
	/// </summary>
	/// <param name="query">The search query.</param>
	/// <returns>A collection of search results ordered by relevance.</returns>
	Task<IEnumerable<ISearchResult>> SearchAsync(string? query);
}

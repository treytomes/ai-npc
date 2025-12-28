namespace Adventure.Intent.FuzzySearch;

/// <summary>
/// Represents a search result with relevance score.
/// </summary>
public interface ISearchResult : IComparable<ISearchResult>
{
	/// <summary>
	/// Gets the text of the matched item.
	/// </summary>
	string Text { get; }

	/// <summary>
	/// Gets the similarity score (0-1, where 1 is perfect match).
	/// </summary>
	double Score { get; }
}

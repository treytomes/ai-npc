namespace LLM.Intent.FuzzySearch;

/// <inheritdoc/>
internal class SearchResult : ISearchResult
{
	#region Properties

	/// <inheritdoc/>
	public string Text { get; }

	/// <inheritdoc/>
	public double Score { get; }

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="SearchResult"/> class.
	/// </summary>
	/// <param name="id">The ID of the matched item.</param>
	/// <param name="text">The text of the matched item.</param>
	/// <param name="score">The similarity score.</param>
	public SearchResult(string text, double score)
	{
		Text = text ?? throw new ArgumentNullException(nameof(text));
		Score = Math.Max(0, Math.Min(1, score));
	}

	#endregion

	#region Methods

	/// <summary>
	/// Compares this result to another based on score.
	/// </summary>
	public int CompareTo(ISearchResult? other)
	{
		if (other == null) return 1;
		return other.Score.CompareTo(Score);
	}

	/// <summary>
	/// Returns a string representation of this search result.
	/// </summary>
	public override string ToString()
	{
		return $"{Text} (Score: {Score:P1})";
	}

	#endregion
}

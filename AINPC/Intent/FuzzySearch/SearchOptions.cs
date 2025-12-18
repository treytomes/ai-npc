namespace AINPC.Intent.FuzzySearch;

/// <summary>
/// Configuration options for fuzzy search operations.
/// </summary>
public record SearchOptions
{
	#region Properties

	/// <summary>
	/// Gets or sets the minimum n-gram size for character-level analysis.
	/// Default is 1.
	/// </summary>
	public int MinNgramSize { get; init; } = 1;

	/// <summary>
	/// Gets or sets the maximum n-gram size for character-level analysis.
	/// Default is 3.
	/// </summary>
	public int MaxNgramSize { get; init; } = 3;

	/// <summary>
	/// Gets or sets whether to include word-level n-grams.
	/// Default is true.
	/// </summary>
	public bool IncludeWordNgrams { get; init; } = true;

	/// <summary>
	/// Gets or sets the maximum word n-gram size.
	/// Default is 2.
	/// </summary>
	public int MaxWordNgramSize { get; init; } = 2;

	/// <summary>
	/// Gets or sets the minimum similarity threshold for results.
	/// Default is 0.1 (10% similarity).
	/// </summary>
	public double MinimumSimilarity { get; init; } = 0.1;

	#endregion

	#region Methods

	/// <summary>
	/// Validates the options and throws if invalid.
	/// </summary>
	public void Validate()
	{
		if (MinNgramSize < 1)
			throw new ArgumentException("MinNgramSize must be at least 1.", nameof(MinNgramSize));

		if (MaxNgramSize < MinNgramSize)
			throw new ArgumentException("MaxNgramSize must be greater than or equal to MinNgramSize.", nameof(MaxNgramSize));

		if (MaxWordNgramSize < 1)
			throw new ArgumentException("MaxWordNgramSize must be at least 1.", nameof(MaxWordNgramSize));

		if (MinimumSimilarity < 0 || MinimumSimilarity > 1)
			throw new ArgumentException("MinimumSimilarity must be between 0 and 1.", nameof(MinimumSimilarity));
	}

	#endregion
}

namespace LLM.Intent.FuzzySearch;

/// <summary>
/// Represents a character-based vector for text similarity comparison.
/// </summary>
public class CharacterVector
{
	#region Fields

	private readonly Dictionary<string, int> _ngramFrequencies;
	private readonly double _magnitude;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="CharacterVector"/> class.
	/// </summary>
	/// <param name="text">The text to create a vector from.</param>
	/// <param name="options">The options for vector creation.</param>
	public CharacterVector(string text, SearchOptions options)
	{
		if (string.IsNullOrEmpty(text))
		{
			throw new ArgumentException("Text cannot be null or empty.", nameof(text));
		}

		_ngramFrequencies = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

		// Add character n-grams.
		for (var n = options.MinNgramSize; n <= Math.Min(options.MaxNgramSize, text.Length); n++)
		{
			for (var i = 0; i <= text.Length - n; i++)
			{
				var ngram = text.Substring(i, n);
				_ngramFrequencies.TryGetValue(ngram, out var count);
				_ngramFrequencies[ngram] = count + 1;
			}
		}

		// Add word-level n-grams if enabled.
		if (options.IncludeWordNgrams)
		{
			var words = text.Split([' ', '\t', '-', '_', '.'], StringSplitOptions.RemoveEmptyEntries);
			if (words.Length > 1)
			{
				for (var n = 1; n <= Math.Min(options.MaxWordNgramSize, words.Length); n++)
				{
					for (var i = 0; i <= words.Length - n; i++)
					{
						var wordNgram = string.Join(" ", words.Skip(i).Take(n));
						_ngramFrequencies.TryGetValue(wordNgram, out var count);
						_ngramFrequencies[wordNgram] = count + 1;
					}
				}
			}
		}

		// Calculate magnitude.
		_magnitude = Math.Sqrt(_ngramFrequencies.Values.Sum(count => count * count));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the frequency of a specific n-gram.
	/// </summary>
	/// <param name="ngram">The n-gram to look up.</param>
	/// <returns>The frequency of the n-gram, or 0 if not present.</returns>
	public int this[string ngram] => _ngramFrequencies.GetValueOrDefault(ngram, 0);

	/// <summary>
	/// Gets all n-grams in this vector.
	/// </summary>
	public IEnumerable<string> Ngrams => _ngramFrequencies.Keys;

	/// <summary>
	/// Gets the magnitude (length) of this vector.
	/// </summary>
	public double Magnitude => _magnitude;

	#endregion

	#region Methods

	/// <summary>
	/// Calculates the cosine similarity between two character vectors.
	/// </summary>
	/// <param name="vector1">The first vector.</param>
	/// <param name="vector2">The second vector.</param>
	/// <returns>A value between 0 and 1 indicating similarity (1 = identical).</returns>
	public static double CalculateSimilarity(CharacterVector vector1, CharacterVector vector2)
	{
		if (vector1 == null) throw new ArgumentNullException(nameof(vector1));
		if (vector2 == null) throw new ArgumentNullException(nameof(vector2));

		// Handle edge cases.
		if (vector1._magnitude == 0 || vector2._magnitude == 0)
			return 0;

		// Calculate dot product.
		var dotProduct = 0.0;

		// Iterate over the smaller set for efficiency.
		var smallerVector = vector1._ngramFrequencies.Count <= vector2._ngramFrequencies.Count ? vector1 : vector2;
		var largerVector = smallerVector == vector1 ? vector2 : vector1;

		foreach (var kvp in smallerVector._ngramFrequencies)
		{
			if (largerVector._ngramFrequencies.TryGetValue(kvp.Key, out var otherCount))
			{
				dotProduct += kvp.Value * otherCount;
			}
		}

		// Calculate cosine similarity
		var cosineSimilarity = dotProduct / (vector1._magnitude * vector2._magnitude);

		// Ensure result is in valid range due to floating-point precision
		return Math.Max(0, Math.Min(1, cosineSimilarity));
	}

	/// <summary>
	/// Calculates the Jaccard similarity between two character vectors.
	/// </summary>
	/// <param name="vector1">The first vector.</param>
	/// <param name="vector2">The second vector.</param>
	/// <returns>A value between 0 and 1 indicating similarity (1 = identical).</returns>
	public static double CalculateJaccardSimilarity(CharacterVector vector1, CharacterVector vector2)
	{
		if (vector1 == null) throw new ArgumentNullException(nameof(vector1));
		if (vector2 == null) throw new ArgumentNullException(nameof(vector2));

		var allNgrams = new HashSet<string>(vector1._ngramFrequencies.Keys);
		allNgrams.UnionWith(vector2._ngramFrequencies.Keys);

		if (allNgrams.Count == 0)
			return 0;

		var intersection = vector1._ngramFrequencies.Keys.Intersect(vector2._ngramFrequencies.Keys).Count();
		return (double)intersection / allNgrams.Count;
	}

	/// <summary>
	/// Returns a string representation of this vector.
	/// </summary>
	public override string ToString()
	{
		var topNgrams = _ngramFrequencies
			.OrderByDescending(kvp => kvp.Value)
			.Take(5)
			.Select(kvp => $"{kvp.Key}:{kvp.Value}");

		return $"CharacterVector[{_ngramFrequencies.Count} n-grams, magnitude={_magnitude:F2}, top={string.Join(", ", topNgrams)}]";
	}

	#endregion
}

using System.Collections.Concurrent;

namespace AINPC.Intent.FuzzySearch;

/// <inheritdoc/>
internal class FuzzySearchEngine : IFuzzySearchEngine
{
	#region Fields

	private readonly List<string> _items;
	private readonly ConcurrentDictionary<string, CharacterVector> _vectorCache;
	private readonly SearchOptions _options;

	#endregion

	#region Constructors

	/// <summary>
	/// Initializes a new instance of the <see cref="FuzzySearchEngine"/> class.
	/// </summary>
	/// <param name="items">The collection of items to search.</param>
	/// <param name="options">Optional search configuration.</param>
	public FuzzySearchEngine(IEnumerable<string> items, SearchOptions? options = null)
	{
		if (items == null)
			throw new ArgumentNullException(nameof(items));

		_items = items.ToList();

		_vectorCache = new ConcurrentDictionary<string, CharacterVector>(StringComparer.OrdinalIgnoreCase);
		_options = options ?? new SearchOptions();
	}

	#endregion

	#region Methods

	/// <inheritdoc/>
	public async Task<IEnumerable<ISearchResult>> SearchAsync(string? query)
	{
		if (string.IsNullOrWhiteSpace(query))
			return Enumerable.Empty<SearchResult>();

		// Perform fuzzy text search
		return await SearchByTextAsync(query);
	}

	/// <summary>
	/// Performs fuzzy text search using character vectors.
	/// </summary>
	private async Task<IEnumerable<ISearchResult>> SearchByTextAsync(string query)
	{
		var queryVector = GetOrCreateVector(query);
		var results = new ConcurrentBag<SearchResult>();

		// Process items in parallel for better performance
		var parallelOptions = new ParallelOptions
		{
			MaxDegreeOfParallelism = Environment.ProcessorCount
		};

		await Task.Run(() =>
		{
			Parallel.ForEach(_items, parallelOptions, text =>
			{
				var itemVector = GetOrCreateVector(text);
				var similarity = CharacterVector.CalculateSimilarity(queryVector, itemVector);

				if (similarity >= _options.MinimumSimilarity)
				{
					results.Add(new SearchResult(text, similarity));
				}
			});
		});

		return results.OrderByDescending(r => r.Score)
					 .ThenBy(r => r.Text.Length);
	}

	/// <summary>
	/// Gets or creates a character vector for the specified text.
	/// </summary>
	private CharacterVector GetOrCreateVector(string text)
	{
		var normalizedText = text.Trim().ToUpperInvariant();
		return _vectorCache.GetOrAdd(normalizedText, t => new CharacterVector(t, _options));
	}

	#endregion
}

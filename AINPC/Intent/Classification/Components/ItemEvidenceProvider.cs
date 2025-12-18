using AINPC.Entities;
using AINPC.Intent.Classification.Facts;
using AINPC.Intent.FuzzySearch;
using NRules;

namespace AINPC.Intent.Classification;

/// <summary>
/// Build fuzzy engine from inventory names + aliases.
/// </summary>
internal sealed class ItemEvidenceProvider
	: IEvidenceProvider<Actor>
{
	public void Provide(ISession session, string utterance, Actor actor)
	{
		var items = actor.Inventory
			.SelectMany(i => new[] { i.Name }.Concat(i.Aliases))
			.ToList();

		var engine = items.ToSearchEngine(new SearchOptions
		{
			MinimumSimilarity = 0.2
		});

		var results = engine
			.SearchAsync(utterance)
			.GetAwaiter()
			.GetResult();

		var matchedItems = new List<FuzzyItemMatch>();
		foreach (var result in results)
		{
			var item = actor.Inventory.First(i =>
				i.Name.Equals(result.Text, StringComparison.OrdinalIgnoreCase) ||
				i.Aliases.Any(a => a.Equals(result.Text, StringComparison.OrdinalIgnoreCase)));

			matchedItems.Add(new FuzzyItemMatch(item.Name, result.Score));
		}

		// Debug: List out the matched items.
		// foreach (var item in matchedItems
		// 	.GroupBy(i => i.ItemName)
		// 	.Select(g => g.MaxBy(i => i.Score)!)) Console.WriteLine($"Item: {item.ItemName}, {item.Score}");

		// Insert distinct FuzzyItemMatch with the highest scores. 
		session.InsertAll(matchedItems
			.GroupBy(i => i.ItemName)
			.Select(g => g.MaxBy(i => i.Score)!));
	}
}

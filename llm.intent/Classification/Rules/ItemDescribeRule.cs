using LLM.Intent.Classification.Facts;
using LLM.Intent.Facts;
using NRules.Fluent.Dsl;
using NRules.RuleModel;

namespace LLM.Intent.Classification.Rules;

internal sealed class ItemDescribeRule : Rule
{
	public override void Define()
	{
		ActorRole role = default!;
		FuzzyIntentHint intentHint = default!;
		FuzzyItemMatch itemMatch = default!;

		When()
			.Match(() => role, r => r.Role == "shopkeeper")
			.Match(() => intentHint,
				h => h.Intent == "item.describe" &&
					 h.Confidence > 0.5)
			.Match(() => itemMatch,
				m => m.Score >= 0.4);
		// Enabling this line will cause a single match on the best scoring choice:
		//.Not<FuzzyItemMatch>(m => m.Score > itemMatch.Score); // highest-score winner

		Then()
			.Do(ctx => ctx.Insert(new RuleFired(nameof(ItemDescribeRule))))
			.Do(ctx => ctx.Insert(
				new Intent(
					"item.describe",
					intentHint.Confidence + itemMatch.Score
				).WithSlot("item_name", itemMatch.ItemName)
			));
	}
}


// public sealed class ItemDescribeRule : Rule
// {
// 	public override void Define()
// 	{
// 		UserUtterance utterance = default!;
// 		IEnumerable<FuzzyItemMatch> matches = default!;

// 		When()
// 			.Match(() => utterance)
// 			.Collect(() => matches,
// 				m => m.Score >= 0.35);

// 		Then()
// 			.Do(ctx => EmitIntent(ctx, matches));
// 	}

// 	private static void EmitIntent(IContext ctx, IEnumerable<FuzzyItemMatch> matches)
// 	{
// 		var items = matches
// 			.OrderByDescending(m => m.Score)
// 			.Select(m => m.ItemName)
// 			.Distinct()
// 			.ToList();

// 		if (!items.Any())
// 			return;

// 		ctx.Insert(new Intent(
// 			name: "item.describe",
// 			confidence: matches.Max(m => m.Score),
// 			slots: new Dictionary<string, IReadOnlyList<string>>
// 			{
// 				["items"] = items
// 			}));
// 	}
// }

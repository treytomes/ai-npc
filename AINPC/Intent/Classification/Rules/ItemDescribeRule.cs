using AINPC.Intent.Classification.Facts;
using AINPC.Intent.Facts;
using NRules.Fluent.Dsl;
using NRules.RuleModel;

namespace AINPC.Intent.Classification.Rules;

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
				new Facts.Intent(
					"item.describe",
					Math.Min(1.0, intentHint.Confidence + itemMatch.Score)
				).WithSlot("item_name", itemMatch.ItemName)
			));
	}
}

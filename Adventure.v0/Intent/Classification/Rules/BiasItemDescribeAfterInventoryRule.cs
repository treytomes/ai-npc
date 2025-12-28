using Adventure.Intent.Classification.Facts;
using NRules.Fluent.Dsl;
using NRules.RuleModel;

namespace Adventure.Intent.Classification.Rules;

public sealed class BiasItemDescribeAfterInventoryRule : Rule
{
	public override void Define()
	{
		FuzzyIntentHint hint = default!;
		RecentIntent recent = default!;

		When()
			.Match(() => hint,
				h => h.Intent == "item.describe" && !h.IsBiased)
			.Match(() => recent,
				r => r.Name == "shop.inventory.list");

		Then()
			.Do(ctx => ctx.Insert(new RuleFired(nameof(BiasItemDescribeAfterInventoryRule))))
			.Do(ctx => Replace(ctx, hint, new FuzzyIntentHint(hint.Intent, Math.Min(1.0, hint.Confidence + 0.15), true)));
	}

	private static IContext Replace(IContext ctx, FuzzyIntentHint oldHint, FuzzyIntentHint newHint)
	{
		ctx.Retract(oldHint);
		ctx.Insert(newHint);
		return ctx;
	}
}


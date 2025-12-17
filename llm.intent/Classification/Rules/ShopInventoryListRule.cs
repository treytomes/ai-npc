using LLM.Intent.Classification.Facts;
using LLM.Intent.Facts;
using NRules.Fluent.Dsl;

namespace LLM.Intent.Classification.Rules;

internal sealed class ShopInventoryListRule : Rule
{
	public override void Define()
	{
		ActorRole role = default!;
		FuzzyIntentHint hint = default!;

		When()
			.Match(() => role, r => r.Role == "shopkeeper")
			.Match(() => hint,
				h => h.Intent == "shop.inventory.list" &&
					 h.Confidence > 0.6);
		Then()
			.Do(ctx => ctx.Insert(new RuleFired(nameof(ShopInventoryListRule))))
			.Do(ctx => ctx.Insert(new Intent(
				"shop.inventory.list",
				hint.Confidence
			)));
	}
}

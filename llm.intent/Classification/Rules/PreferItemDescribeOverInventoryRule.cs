using LLM.Intent.Classification.Facts;
using NRules.Fluent.Dsl;

namespace LLM.Intent.Classification.Rules;

public sealed class PreferItemDescribeOverInventoryRule : Rule
{
	public override void Define()
	{
		Facts.Intent inventory = default!;
		Facts.Intent describe = default!;

		When()
			.Match(() => inventory,
				i => i.Name == "shop.inventory.list")
			.Match(() => describe,
				i => i.Name == "item.describe" &&
					 i.Confidence > inventory.Confidence);

		Then()
			.Do(ctx => ctx.Insert(new RuleFired(nameof(PreferItemDescribeOverInventoryRule))))
			.Do(ctx => ctx.Insert(new SuppressedIntent("shop.inventory.list")));
	}
}

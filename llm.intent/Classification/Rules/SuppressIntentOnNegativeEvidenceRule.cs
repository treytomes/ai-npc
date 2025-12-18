using LLM.Intent.Classification.Facts;
using NRules.Fluent.Dsl;

namespace LLM.Intent.Classification.Rules;

public sealed class SuppressIntentOnNegativeEvidenceRule : Rule
{
	public override void Define()
	{
		Facts.Intent intent = default!;
		NegativeIntentHint neg = default!;

		When()
			.Match(() => intent)
			.Match(() => neg,
				n => n.Intent == intent.Name &&
					 n.Strength > intent.Confidence);

		Then()
			.Do(ctx => ctx.Insert(new RuleFired(nameof(SuppressIntentOnNegativeEvidenceRule))))
			.Do(ctx => ctx.Retract(intent));
	}
}

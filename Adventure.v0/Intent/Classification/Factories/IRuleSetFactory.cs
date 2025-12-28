using NRules.RuleModel;

namespace Adventure.Intent.Classification.Factories;

internal interface IRuleSetFactory
{
	IRuleSet GetRules(string actorRole);
}
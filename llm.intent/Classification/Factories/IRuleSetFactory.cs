using NRules.RuleModel;

namespace LLM.Intent.Classification.Factories;

internal interface IRuleSetFactory
{
	IRuleSet GetRules(string actorRole);
}
using Adventure.Intent.Classification.Rules;
using NRules.Fluent;
using NRules.RuleModel;

namespace Adventure.Intent.Classification.Factories;

internal class RuleSetFactory : IRuleSetFactory
{
	#region Fields

	private readonly RuleDefinitionFactory _ruleFactory = new();

	#endregion

	#region Methods

	public IRuleSet GetRules(string actorRole)
	{
		return actorRole.ToLower().Trim() switch
		{
			"shopkeeper" => GetShopkeeperRules(),
			_ => throw new ArgumentException($"Unknown role: {actorRole}", nameof(actorRole)),
		};
	}

	// TODO: I'd love for this to be more data-driven.  Load rules from file.
	private IRuleSet GetShopkeeperRules()
	{
		var rules = new RuleSet("shopkeeper");
		rules.Add(_ruleFactory.Create(new BiasItemDescribeAfterInventoryRule()));
		rules.Add(_ruleFactory.Create(new ItemDescribeRule()));
		rules.Add(_ruleFactory.Create(new PreferItemDescribeOverInventoryRule()));
		rules.Add(_ruleFactory.Create(new ShopInventoryListRule()));
		rules.Add(_ruleFactory.Create(new SuppressIntentOnNegativeEvidenceRule()));
		return rules;
	}

	#endregion
}
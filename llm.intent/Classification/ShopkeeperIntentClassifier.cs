using LLM.Intent.Classification.Facts;
using LLM.Intent.Classification.Rules;
using LLM.Intent.Entities;
using NRules;
using NRules.Fluent;
using NRules.RuleModel;

namespace LLM.Intent.Classification;

internal sealed class ShopkeeperIntentClassifier
{
	#region Fields

	private readonly ISessionFactory _sessionFactory;
	private readonly ISessionInitializer<Actor> _initializer = new ShopkeeperSessionInitializer();
	private readonly IReadOnlyList<IEvidenceProvider<Actor>> _providers = [
		new NegativeIntentEvidenceProvider(),
		new FuzzyIntentEvidenceProvider(),
		new ItemEvidenceProvider(),
	];
	private readonly IIntentAggregator _aggregator = new HighestConfidenceIntentAggregator();

	#endregion

	#region Constructors

	public ShopkeeperIntentClassifier()
	{
		var ruleFactory = new RuleDefinitionFactory();

		var rules = new RuleSet("shopkeeper");
		rules.Add(ruleFactory.Create(new BiasItemDescribeAfterInventoryRule()));
		rules.Add(ruleFactory.Create(new ItemDescribeRule()));
		rules.Add(ruleFactory.Create(new PreferItemDescribeOverInventoryRule()));
		rules.Add(ruleFactory.Create(new ShopInventoryListRule()));
		rules.Add(ruleFactory.Create(new SuppressIntentOnNegativeEvidenceRule()));

		var repository = new RuleRepository();
		repository.Add(rules);

		_sessionFactory = repository.Compile();
	}

	#endregion

	#region Methods

	public IntentClassificationResult Classify(string utterance, Actor actor, RecentIntent? recentIntent = null)
	{
		var session = _sessionFactory.CreateSession();

		_initializer.Initialize(session, utterance, actor, recentIntent);

		foreach (var provider in _providers)
		{
			provider.Provide(session, utterance, actor);
		}

		session.Fire();

		var intents = _aggregator.Aggregate(session);

		var firedRules = session
			.Query<RuleFired>()
			.Select(r => r.RuleName)
			.Distinct()
			.ToList();

		return new IntentClassificationResult(intents, firedRules);
	}

	#endregion
}

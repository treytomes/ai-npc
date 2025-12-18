using LLM.Intent.Classification.Factories;
using LLM.Intent.Classification.Facts;
using LLM.Intent.Entities;
using LLM.Intent.Lexicons;

namespace LLM.Intent.Classification;

internal sealed class ShopkeeperIntentClassifier
{
	#region Fields

	private readonly IActorSessionFactory _actorSessionFactory = new ActorSessionFactory();
	private readonly ISessionInitializer<Actor> _initializer = new ShopkeeperSessionInitializer();
	private readonly IIntentLexiconFactory _intentLexiconFactory = new IntentLexiconFactory();
	private readonly IIntentAggregator _aggregator = new HighestConfidenceIntentAggregator();
	private readonly IReadOnlyList<IEvidenceProvider<Actor>> _providers;

	#endregion

	#region Constructors

	public ShopkeeperIntentClassifier()
	{
		_providers = [
			new NegativeIntentEvidenceProvider(_intentLexiconFactory),
			new PositiveIntentEvidenceProvider(_intentLexiconFactory),
			new ItemEvidenceProvider(),
		];
	}

	#endregion

	#region Methods

	public IntentClassificationResult Classify(string utterance, Actor actor, RecentIntent? recentIntent = null)
	{
		var session = _actorSessionFactory.CreateSession(actor.Role);

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

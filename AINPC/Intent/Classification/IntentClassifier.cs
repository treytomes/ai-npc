using AINPC.Entities;
using AINPC.Intent.Classification.Factories;
using AINPC.Intent.Classification.Facts;
using AINPC.Intent.Lexicons;

namespace AINPC.Intent.Classification;

internal sealed class IntentClassifier : IIntentClassifier
{
	#region Fields

	private readonly IActorSessionFactory _actorSessionFactory = new ActorSessionFactory();
	private readonly ISessionInitializer<Actor> _initializer = new SessionInitializer();
	private readonly IIntentLexiconFactory _intentLexiconFactory = new IntentLexiconFactory();
	private readonly IIntentAggregator _aggregator = new HighestConfidenceIntentAggregator();
	private readonly IReadOnlyList<IEvidenceProvider<Actor>> _providers;

	#endregion

	#region Constructors

	public IntentClassifier()
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
		var session = _actorSessionFactory.CreateSession(actor.Role.Name);

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

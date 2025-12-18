using NRules;
using NRules.Fluent;

namespace LLM.Intent.Classification.Factories;

internal class ActorSessionFactory : IActorSessionFactory
{
	#region Fields

	private IRuleSetFactory _ruleSetFactory = new RuleSetFactory();
	private Dictionary<string, ISessionFactory> _sessionFactories = new();

	#endregion

	#region Methods

	public ISession CreateSession(string actorRole)
	{
		return GetSessionFactory(actorRole).CreateSession();
	}

	private ISessionFactory GetSessionFactory(string actorRole)
	{
		if (!_sessionFactories.ContainsKey(actorRole))
		{
			CreateSessionFactory(actorRole);
		}
		return _sessionFactories[actorRole];
	}

	private void CreateSessionFactory(string actorRole)
	{
		var rules = _ruleSetFactory.GetRules(actorRole);

		var repository = new RuleRepository();
		repository.Add(rules);

		_sessionFactories[actorRole] = repository.Compile();
	}

	#endregion
}
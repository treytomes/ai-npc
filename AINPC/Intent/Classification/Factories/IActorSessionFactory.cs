using NRules;

namespace AINPC.Intent.Classification.Factories;

internal interface IActorSessionFactory
{
	ISession CreateSession(string actorRole);
}
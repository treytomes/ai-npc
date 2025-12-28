using NRules;

namespace Adventure.Intent.Classification;

interface IEvidenceProvider<TActor>
{
	Task ProvideAsync(ISession session, string utterance, TActor actor);
}

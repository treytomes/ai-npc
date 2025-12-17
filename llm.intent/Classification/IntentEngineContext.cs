using LLM.Intent.Classification.Facts;

namespace LLM.Intent.Classification;

internal sealed class IntentEngineContext
{
	public RecentIntent? RecentIntent { get; init; }
}

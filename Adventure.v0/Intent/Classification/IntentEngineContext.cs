using Adventure.Intent.Classification.Facts;

namespace Adventure.Intent.Classification;

internal sealed class IntentEngineContext
{
	public RecentIntent? RecentIntent { get; init; }
}

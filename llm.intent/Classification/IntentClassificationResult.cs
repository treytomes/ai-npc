namespace LLM.Intent.Classification;

internal sealed record IntentClassificationResult(
	IReadOnlyList<Intent> Intents,
	IReadOnlyList<string> FiredRules);

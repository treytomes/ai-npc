namespace LLM.Intent.Classification;

internal sealed record IntentClassificationResult(
	IReadOnlyList<Facts.Intent> Intents,
	IReadOnlyList<string> FiredRules);

namespace LLM.NLP;

/// <summary>
/// Human-readable part-of-speech tags used by LLM.NLP.
/// Mirrors Catalyst POS values.
/// </summary>
public enum NlpPartOfSpeech
{
	None,

	Adjective,
	Adposition,
	Adverb,
	AuxiliaryVerb,

	CoordinatingConjunction,
	Determiner,
	Interjection,

	Noun,
	Numeral,

	Particle,
	Pronoun,
	ProperNoun,

	Punctuation,
	SubordinatingConjunction,

	Symbol,
	Verb,

	Other
}

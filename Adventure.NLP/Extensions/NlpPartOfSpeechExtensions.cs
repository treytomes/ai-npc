using Catalyst;

namespace Adventure.NLP;

public static class NlpPartOfSpeechExtensions
{
	public static PartOfSpeech ToCatalyst(this NlpPartOfSpeech pos)
	{
		return pos switch
		{
			NlpPartOfSpeech.None => PartOfSpeech.NONE,

			NlpPartOfSpeech.Adjective => PartOfSpeech.ADJ,
			NlpPartOfSpeech.Adposition => PartOfSpeech.ADP,
			NlpPartOfSpeech.Adverb => PartOfSpeech.ADV,
			NlpPartOfSpeech.AuxiliaryVerb => PartOfSpeech.AUX,

			NlpPartOfSpeech.CoordinatingConjunction => PartOfSpeech.CCONJ,
			NlpPartOfSpeech.Determiner => PartOfSpeech.DET,
			NlpPartOfSpeech.Interjection => PartOfSpeech.INTJ,

			NlpPartOfSpeech.Noun => PartOfSpeech.NOUN,
			NlpPartOfSpeech.Numeral => PartOfSpeech.NUM,

			NlpPartOfSpeech.Particle => PartOfSpeech.PART,
			NlpPartOfSpeech.Pronoun => PartOfSpeech.PRON,
			NlpPartOfSpeech.ProperNoun => PartOfSpeech.PROPN,

			NlpPartOfSpeech.Punctuation => PartOfSpeech.PUNCT,
			NlpPartOfSpeech.SubordinatingConjunction => PartOfSpeech.SCONJ,

			NlpPartOfSpeech.Symbol => PartOfSpeech.SYM,
			NlpPartOfSpeech.Verb => PartOfSpeech.VERB,

			NlpPartOfSpeech.Other or _ => PartOfSpeech.X,
		};
	}

	public static NlpPartOfSpeech ToNlp(this PartOfSpeech pos)
	{
		return pos switch
		{
			PartOfSpeech.NONE => NlpPartOfSpeech.None,

			PartOfSpeech.ADJ => NlpPartOfSpeech.Adjective,
			PartOfSpeech.ADP => NlpPartOfSpeech.Adposition,
			PartOfSpeech.ADV => NlpPartOfSpeech.Adverb,
			PartOfSpeech.AUX => NlpPartOfSpeech.AuxiliaryVerb,

			PartOfSpeech.CCONJ => NlpPartOfSpeech.CoordinatingConjunction,
			PartOfSpeech.DET => NlpPartOfSpeech.Determiner,
			PartOfSpeech.INTJ => NlpPartOfSpeech.Interjection,

			PartOfSpeech.NOUN => NlpPartOfSpeech.Noun,
			PartOfSpeech.NUM => NlpPartOfSpeech.Numeral,

			PartOfSpeech.PART => NlpPartOfSpeech.Particle,
			PartOfSpeech.PRON => NlpPartOfSpeech.Pronoun,
			PartOfSpeech.PROPN => NlpPartOfSpeech.ProperNoun,

			PartOfSpeech.PUNCT => NlpPartOfSpeech.Punctuation,
			PartOfSpeech.SCONJ => NlpPartOfSpeech.SubordinatingConjunction,

			PartOfSpeech.SYM => NlpPartOfSpeech.Symbol,
			PartOfSpeech.VERB => NlpPartOfSpeech.Verb,

			PartOfSpeech.X => NlpPartOfSpeech.Other,

			_ => NlpPartOfSpeech.Other
		};
	}
}

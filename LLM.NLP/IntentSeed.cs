namespace LLM.NLP;

/// <summary>
/// Represents the core grammatical components extracted from a parsed sentence,
/// providing a structured foundation for intent recognition and semantic analysis.
/// </summary>
/// <param name="Verb">
/// <summary>
/// The main action or state described in the sentence.
/// </summary>
/// <example>
/// "show" in "Show me the door"
/// "have" in "What do you have?"
/// </example>
/// </param>
/// <param name="Subject">
/// <summary>
/// The entity performing the action or being described by the verb.
/// In imperative sentences, this is typically null (implied "you").
/// </summary>
/// <example>
/// "you" in "You have three items"
/// "the cat" in "The cat sits on the mat"
/// null in "Show me the door" (imperative)
/// </example>
/// </param>
/// <param name="DirectObject">
/// <summary>
/// The entity directly affected by or receiving the action of the verb.
/// Answers "what?" or "whom?" after the verb.
/// </summary>
/// <example>
/// "the door" in "Show me the door"
/// "what" in "What do you have?"
/// "three items" in "You have three items"
/// </example>
/// </param>
/// <param name="IndirectObject">
/// <summary>
/// The entity indirectly affected by the action, typically the recipient or beneficiary.
/// Often answers "to whom?" or "for whom?" the action is performed.
/// </summary>
/// <example>
/// "me" in "Show me the door" (show to whom? me)
/// "him" in "Give him the book"
/// "the customer" in "Send the customer a receipt"
/// </example>
/// </param>
/// <param name="Prepositions">
/// <summary>
/// Prepositional phrases that modify the main action or provide additional context.
/// Keyed by the preposition that introduces each phrase.
/// </summary>
/// <example>
/// {"for": "sale"} in "What do you have for sale?"
/// {"on": "the table", "with": "a spoon"} in "Put it on the table with a spoon"
/// {"out of": "the chest"} in "Take the key out of the chest"
/// </example>
/// </param>
public sealed record IntentSeed(
	string? Verb,
	NounPhrase? Subject,
	NounPhrase? DirectObject,
	NounPhrase? IndirectObject,
	IReadOnlyDictionary<string, NounPhrase> Prepositions
);
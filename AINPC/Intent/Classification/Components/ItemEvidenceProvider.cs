using AINPC.Entities;
using AINPC.Intent.Classification.Facts;
using AINPC.Intent.FuzzySearch;
using NRules;
using Catalyst;
using Catalyst.Models;
using Mosaik.Core;

using P = Catalyst.PatternUnitPrototype;

namespace AINPC.Intent.Classification;

/// <summary>
/// Uses Catalyst NLP to extract noun phrases before fuzzy matching items.
/// </summary>
internal sealed class ItemEvidenceProvider : IEvidenceProvider<Actor>
{
	// private readonly Pipeline _nlpPipeline;

	// public ItemEvidenceProvider()
	// {
	// 	// // Initialize Catalyst
	// 	// // Download models first time: English.All() downloads pre-trained models
	// 	// Catalyst.Models.English.Register(); // Register English language

	// 	// // Load the pipeline with necessary components
	// 	// Storage.Current = new DiskStorage("catalyst-models");
	// }

	public async Task ProvideAsync(ISession session, string utterance, Actor actor)
	{
		// Build the Catalyst pipeline.
		var _nlpPipeline = Pipeline.For(Language.English);

		// 1. Tokenizer (usually included by default)

		// 2. Part-of-speech tagger
		// _nlpPipeline.Add(await AveragePerceptronTagger.FromStoreAsync(
		// 	language: Language.English,
		// 	version: Mosaik.Core.Version.Latest,
		// 	tag: "WikiNER"
		// ));

		// // 3. Dependency parser - THIS IS KEY for noun phrase extraction
		// _nlpPipeline.Add(await AveragePerceptronDependencyParser.FromStoreAsync(
		// 	language: Language.English,
		// 	version: Mosaik.Core.Version.Latest,
		// 	tag: "WikiNER"
		// ));

		// // 4. Entity recognizer (optional but helpful)
		// _nlpPipeline.Add(await AveragePerceptronEntityRecognizer.FromStoreAsync(
		// 	language: Language.English,
		// 	version: Mosaik.Core.Version.Latest,
		// 	tag: "WikiNER"
		// ));
		_nlpPipeline.Add(await AveragePerceptronEntityRecognizer.FromStoreAsync(language: Language.English, version: Mosaik.Core.Version.Latest, tag: "WikiNER"));

		// var isApattern = new PatternSpotter(Language.English, 0, tag: "is-a-pattern", captureTag: "IsA");
		// isApattern.NewPattern(
		// 	"Is+Noun",
		// 	mp => mp.Add(
		// 	new PatternUnit(P.Single().WithToken("is").WithPOS(NlpPartOfSpeech.Verb)),
		// 	new PatternUnit(P.Multiple().WithPOS(NlpPartOfSpeech.Noun, NlpPartOfSpeech.ProperNoun, NlpPartOfSpeech.AuxiliaryVerb, NlpPartOfSpeech.Determiner, NlpPartOfSpeech.Adjective))
		// ));
		// _nlpPipeline.Add(isApattern);


		// Parse the utterance with Catalyst
		var doc = new Document(utterance, Language.English);
		_nlpPipeline.ProcessSingle(doc);

		PrintDocumentEntities(doc);

		// STEP 1: Extract noun phrases with their grammatical roles
		var nounPhrasesWithRoles = ExtractNounPhrasesWithRoles(doc);

		if (!nounPhrasesWithRoles.Any())
		{
			// Fallback: if no noun phrases found, try basic extraction
			var basicPhrases = ExtractNounPhrases(doc);
			nounPhrasesWithRoles = basicPhrases
				.Select(p => (p, "unknown"))
				.ToArray();
		}

		if (!nounPhrasesWithRoles.Any())
		{
			return; // No entities found
		}

		// STEP 2: Build fuzzy engine from inventory
		var itemSearchTerms = actor.Inventory
			.SelectMany(i => new[] { i.Name }.Concat(i.Aliases))
			.ToList();

		if (!itemSearchTerms.Any())
		{
			return; // No items to match
		}

		var engine = itemSearchTerms.ToSearchEngine(new SearchOptions
		{
			MinimumSimilarity = 0.5 // Higher threshold since input is cleaner
		});

		var matchedItems = new List<FuzzyItemMatch>();

		// STEP 3: Fuzzy match each extracted noun phrase
		foreach (var (nounPhrase, role) in nounPhrasesWithRoles)
		{
			var results = engine
				.SearchAsync(nounPhrase)
				.GetAwaiter()
				.GetResult()
				.Take(2); // Top 2 matches per phrase

			foreach (var result in results)
			{
				var item = actor.Inventory.FirstOrDefault(i =>
					i.Name.Equals(result.Text, StringComparison.OrdinalIgnoreCase) ||
					i.Aliases.Any(a => a.Equals(result.Text, StringComparison.OrdinalIgnoreCase)));

				if (item != null)
				{
					matchedItems.Add(new FuzzyItemMatch(
						item.Name,
						result.Score,
						nounPhrase,  // Original phrase player used
						role         // Grammatical role (direct object, indirect object, etc.)
					));
				}
			}
		}

		// STEP 4: Insert distinct matches with highest scores per item
		// If same item appears in multiple roles, keep all role variations
		var distinctMatches = matchedItems
			.GroupBy(m => (m.ItemName, m.Role))
			.Select(g => g.MaxBy(m => m.Score)!)
			.ToList();

		session.InsertAll(distinctMatches);
	}

	private static void PrintDocumentEntities(IDocument doc)
	{
		Console.WriteLine($"Input text:\n\t'{doc.Value}'\n\nTokenized Value:\n\t'{doc.TokenizedValue(mergeEntities: true)}'\n\nEntities: \n{string.Join("\n", doc.SelectMany(span => span.GetEntities()).Select(e => $"\t{e.Value} [{e.EntityType.Type}]"))}");
	}

	/// <summary>
	/// Extracts noun phrases (noun chunks) from the document.
	/// </summary>
	private IEnumerable<string> ExtractNounPhrases(Document doc)
	{
		var phrases = new List<string>();

		foreach (var sentence in doc)
		{
			// Method 1: Use noun chunks (groups of nouns with their modifiers)
			var chunks = sentence
				.Where(t => t.POS == PartOfSpeech.NOUN || t.POS == PartOfSpeech.PROPN || t.POS == PartOfSpeech.PRON || t.POS == PartOfSpeech.ADJ)
				.Select(c => c.Value.ToLowerInvariant().Trim())
				.Where(s => !string.IsNullOrWhiteSpace(s));

			phrases.AddRange(chunks);

			// Method 2: Fallback - extract standalone nouns if no chunks found
			if (!chunks.Any())
			{
				var nouns = sentence.Tokens
					.Where(t => t.POS == PartOfSpeech.NOUN || t.POS == PartOfSpeech.PROPN)
					.Select(t => t.Value.ToLowerInvariant());

				phrases.AddRange(nouns);
			}
		}

		Console.WriteLine($"Noun phrases: {phrases.Distinct().Join(", ")}");

		return phrases.Distinct();
	}

	/// <summary>
	/// Enhanced version that identifies item roles (direct object, indirect object, etc.)
	/// </summary>
	private (string Phrase, string Role)[] ExtractNounPhrasesWithRoles(Document doc)
	{
		var phrasesWithRoles = new List<(string, string)>();

		foreach (var span in doc)
		{
			foreach (var entity in span.GetEntities())
			{
				foreach (var token in entity.Children)
				{
					// Look for nouns and their dependency relations
					if (token.POS == PartOfSpeech.NOUN || token.POS == PartOfSpeech.PROPN)
					{
						var role = token.DependencyType;
						//  switch
						// {
						// 	DependencyType.DirectObject => ItemRole.DirectObject,
						// 	DependencyType.IndirectObject => ItemRole.IndirectObject,
						// 	DependencyType.PrepositionalObject => ItemRole.PrepositionalObject,
						// 	_ => ItemRole.Unknown
						// };

						// Get the full noun phrase (including modifiers)
						var phrase = GetFullNounPhrase(span, token);
						phrasesWithRoles.Add((phrase, role));
					}
				}
			}
		}

		Console.WriteLine("Item phrases with roles:");
		foreach (var phrase in phrasesWithRoles)
		{
			Console.WriteLine($"{phrase.Item1}: {phrase.Item2}");
		}

		return phrasesWithRoles.ToArray();
	}

	private string GetFullNounPhrase(Span sentence, IToken noun)
	{
		// Find all tokens that modify this noun (adjectives, determiners, etc.)
		var modifiers = sentence.Tokens
			.Where(t => t.Head == noun.Index &&
					   (t.POS == PartOfSpeech.ADJ || t.POS == PartOfSpeech.DET))
			.OrderBy(t => t.Index);

		var phrase = string.Join(" ",
			modifiers.Select(m => m.Value).Concat(new[] { noun.Value }));

		return phrase.ToLowerInvariant().Trim();
	}

	public enum ItemRole
	{
		Unknown,
		DirectObject,    // "take SWORD"
		IndirectObject,  // "give key to GUARD"
		PrepositionalObject // "look in CHEST"
	}
}
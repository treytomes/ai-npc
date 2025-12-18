using System.Text.Json;

namespace AINPC.Intent.Lexicons;

/// <summary>
/// Provides intent patterns for shopkeeper NLU from a JSON configuration file.
/// </summary>
internal sealed class IntentLexicon : IIntentLexicon
{
	#region Constants

	private const string PATH = "assets";

	#endregion

	#region Fields

	private readonly List<IntentLexiconDefinition> _intents;

	#endregion

	#region Constructors

	private IntentLexicon(IEnumerable<IntentLexiconDefinition> intents)
	{
		_intents = intents?.ToList() ?? throw new ArgumentNullException(nameof(intents));
	}

	#endregion

	#region Properties

	/// <summary>
	/// Gets the intent patterns.
	/// </summary>
	public IReadOnlyList<IntentLexiconDefinition> Intents => _intents.AsReadOnly();

	#endregion

	#region Methods

	/// <summary>
	/// Loads the lexicon from the embedded or external JSON file.
	/// </summary>
	public static IntentLexicon Load(string filename = "negative_intent_lexicon.json")
	{
		try
		{
			// Try to load from file first
			var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, PATH, filename);
			if (File.Exists(filePath))
			{
				return LoadFromFile(filePath);
			}

			throw new FileNotFoundException($"File does not exist: {filePath}");
		}
		catch (Exception ex)
		{
			throw new InvalidOperationException("Failed to load shopkeeper intent lexicon.", ex);
		}
	}

	/// <summary>
	/// Loads the lexicon from a file.
	/// </summary>
	private static IntentLexicon LoadFromFile(string filePath)
	{
		var json = File.ReadAllText(filePath);
		return LoadFromJson(json);
	}

	/// <summary>
	/// Loads the lexicon from JSON string.
	/// </summary>
	private static IntentLexicon LoadFromJson(string json)
	{
		var config = JsonSerializer.Deserialize<IntentLexiconConfigDto>(json, new JsonSerializerOptions()
		{
			PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
			WriteIndented = true,
			ReadCommentHandling = JsonCommentHandling.Skip,
			AllowTrailingCommas = true,
			PropertyNameCaseInsensitive = true
		}) ?? throw new InvalidOperationException("Failed to deserialize intent configuration.");

		var intents = config.Intents.Select(i => new IntentLexiconDefinition()
		{
			Name = i.Name,
			Patterns = i.Patterns.AsReadOnly(),
		});

		return new IntentLexicon(intents);
	}

	#endregion
}

using Catalyst;
using Mosaik.Core;

namespace LLM.NLP;

internal sealed class NlpRuntime : INlpRuntime
{
	#region Fields

	private static bool _initialized;
	private static readonly object _lock = new();

	private readonly Pipeline _pipeline;
	private readonly Language _language;

	#endregion

	#region Constructors

	public NlpRuntime(NlpRuntimeOptions options)
	{
		EnsureInitialized(options);

		_language = options.Language;
		_pipeline = Pipeline.For(_language);
	}

	#endregion

	#region Methods

	/// <summary>
	/// Processes raw input text using the configured Catalyst pipeline.
	/// </summary>
	/// <param name="input">The raw user input string.</param>
	/// <returns>A fully processed Catalyst document.</returns>
	public Document Process(string input)
	{
		if (string.IsNullOrWhiteSpace(input))
		{
			return new Document(string.Empty, _language);
		}

		var document = new Document(input, _language);
		_pipeline.ProcessSingle(document);
		return document;
	}

	private static void EnsureInitialized(NlpRuntimeOptions options)
	{
		if (_initialized) return;

		lock (_lock)
		{
			if (_initialized) return;

			Storage.Current = new DiskStorage(options.DataPath);

			if (options.Language == Language.English)
			{
				Catalyst.Models.English.Register();
			}
			else
			{
				throw new NotSupportedException(
					$"Language '{options.Language}' is not registered.");
			}

			// Force pipeline initialization once
			Pipeline.For(options.Language);

			_initialized = true;
		}
	}

	#endregion
}

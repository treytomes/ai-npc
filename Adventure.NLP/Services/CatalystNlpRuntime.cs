using System.Globalization;
using System.Runtime.InteropServices;
using Catalyst;
using Mosaik.Core;

namespace Adventure.NLP.Services;

internal sealed class CatalystNlpRuntime : INlpRuntime
{
	#region Fields

	private static bool _initialized;
	private static readonly object _lock = new();

	private readonly Pipeline _pipeline;
	private readonly Language _language;

	#endregion

	#region Constructors

	public CatalystNlpRuntime()
	{
		_language = CultureInfo.CurrentUICulture.ToMosaikLanguage();
		EnsureInitialized();
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

	private static string GetInstallDir()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
		{
			var baseDir = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
			return Path.Combine(baseDir, "Adventure", "catalyst");
		}

		// Linux/macOS-like path
		var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		return Path.Combine(home, ".adventure", "catalyst");
	}

	private void EnsureInitialized()
	{
		if (_initialized) return;

		lock (_lock)
		{
			if (_initialized) return;

			Storage.Current = new DiskStorage(GetInstallDir());

			if (_language == Language.English)
			{
				Catalyst.Models.English.Register();
			}
			else
			{
				throw new NotSupportedException($"Language '{_language}' is not registered.");
			}

			// Force pipeline initialization once.
			Pipeline.For(_language);

			_initialized = true;
		}
	}

	#endregion
}

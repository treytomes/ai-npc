using Catalyst;
using Microsoft.Extensions.Logging;
using Mosaik.Core;

namespace Adventure.CatalystRuntime;

public class CatalystManager
{
	#region Fields

	private readonly ILogger<CatalystManager> _logger;

	#endregion

	#region Constructors

	public CatalystManager(ILogger<CatalystManager> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	#endregion

	#region Properties

	public Pipeline? Pipeline { get; private set; }

	#endregion

	#region Methods

	public async Task InitializeAsync()
	{
		// Set storage location for models.
		Storage.Current = new DiskStorage("catalyst-models");

		// Register and download English models (only needed once).
		Catalyst.Models.English.Register();

		// Pre-load the pipeline to trigger download.
		Pipeline = Pipeline.For(Language.English);

		_logger.LogInformation("Catalyst models ready.");

		await Task.CompletedTask;
	}

	#endregion
}
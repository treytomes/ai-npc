using Catalyst;
using Mosaik.Core;

namespace AINPC.CatalystRuntime;

public class CatalystManager
{
	public async Task InitializeAsync()
	{
		// Set storage location for models.
		Storage.Current = new DiskStorage("catalyst-models");

		// Register and download English models (only needed once).
		Catalyst.Models.English.Register();

		// Pre-load the pipeline to trigger download.
		var pipeline = Pipeline.For(Language.English);

		Console.WriteLine("Catalyst models ready.");
	}
}
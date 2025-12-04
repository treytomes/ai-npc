using AINPC.Gpu.Services;
using AINPC.Services;

namespace AINPC.Gpu;

sealed class WindowGpuDetector : IGpuDetector
{
	#region Fields

	private readonly IProcessService _processService;
	private readonly IGpuVendorFactory _gpuVendorFactory;

	#endregion

	#region Constructors

	public WindowGpuDetector(IProcessService processService, IGpuVendorFactory gpuVendorFactory)
	{
		_processService = processService ?? throw new ArgumentNullException(nameof(processService));
		_gpuVendorFactory = gpuVendorFactory ?? throw new ArgumentNullException(nameof(gpuVendorFactory));
	}

	#endregion

	#region Methods

	public IReadOnlyList<GpuInfo> Detect()
	{
		try
		{
			// Use wmic (deprecated) but still functional on all Windows versions
			// wmic path win32_VideoController get Name,DriverVersion
			var output = _processService.RunProcess("wmic", "path win32_VideoController get Name,DriverVersion /format:list");

			var results = new List<GpuInfo>();

			var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			string? name = null;
			string? driver = null;

			foreach (var line in lines)
			{
				if (line.StartsWith("Name=", StringComparison.OrdinalIgnoreCase))
					name = line.Substring("Name=".Length).Trim();

				if (line.StartsWith("DriverVersion=", StringComparison.OrdinalIgnoreCase))
					driver = line.Substring("DriverVersion=".Length).Trim();

				if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(driver))
				{
					results.Add(new GpuInfo
					{
						Name = name,
						DriverVersion = driver,
						Vendor = _gpuVendorFactory.GuessVendor(name)
					});

					name = null;
					driver = null;
				}
			}

			return results;
		}
		catch
		{
			return Array.Empty<GpuInfo>();
		}
	}

	#endregion
}

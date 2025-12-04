using AINPC.Gpu.Services;
using AINPC.Services;

namespace AINPC.Gpu;

sealed class LinuxGpuDetector : IGpuDetector
{
	#region Fields

	private readonly IProcessService _processService;
	private readonly IGpuVendorFactory _gpuVendorFactory;

	#endregion

	#region Constructors

	public LinuxGpuDetector(IProcessService processService, IGpuVendorFactory gpuVendorFactory)
	{
		_processService = processService ?? throw new ArgumentNullException(nameof(processService));
		_gpuVendorFactory = gpuVendorFactory ?? throw new ArgumentNullException(nameof(gpuVendorFactory));
	}

	#endregion

	#region Methods

	public IReadOnlyList<GpuInfo> Detect()
	{
		var results = new List<GpuInfo>();

		// lspci output: "VGA compatible controller: NVIDIA Corporation ..."
		var output = _processService.RunProcess("lspci", "");

		var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

		foreach (var line in lines)
		{
			if (!line.Contains("VGA") && !line.Contains("3D"))
				continue;

			if (!line.Contains(":"))
				continue;

			var descIdx = line.IndexOf(":");
			var desc = descIdx >= 0 ? line[(descIdx + 1)..].Trim() : line.Trim();

			results.Add(new GpuInfo
			{
				Name = desc,
				Vendor = _gpuVendorFactory.GuessVendor(desc),
				DriverVersion = "" // could be populated with nvidia-smi, glxinfo, or vainfo later
			});
		}

		return results;
	}

	#endregion
}

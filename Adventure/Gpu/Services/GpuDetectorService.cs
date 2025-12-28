using System.Runtime.InteropServices;
using Adventure.Services;

namespace Adventure.Gpu.Services;

sealed class GpuDetectorService : IGpuDetectorService
{
	#region Fields

	private readonly IProcessService _processService;
	private readonly IGpuVendorFactory _gpuVendorFactory;

	#endregion

	#region Constructors

	public GpuDetectorService(IProcessService processService, IGpuVendorFactory gpuVendorFactory)
	{
		_processService = processService ?? throw new ArgumentNullException(nameof(processService));
		_gpuVendorFactory = gpuVendorFactory ?? throw new ArgumentNullException(nameof(gpuVendorFactory));
	}

	#endregion

	#region Methods

	public GpuVendor GetVendor()
	{
		var infos = Detect().Where(x => x.Vendor != GpuVendor.Unknown);
		if (!infos.Any())
		{
			return GpuVendor.Unknown;
		}
		return infos.First().Vendor;
	}

	public IReadOnlyList<GpuInfo> Detect()
	{
		var detector = GetDetectorForOS();
		return detector?.Detect() ?? Array.Empty<GpuInfo>();
	}

	private IGpuDetector? GetDetectorForOS()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			return new WindowGpuDetector(_processService, _gpuVendorFactory);

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			return new LinuxGpuDetector(_processService, _gpuVendorFactory);

		return null;
	}

	#endregion
}

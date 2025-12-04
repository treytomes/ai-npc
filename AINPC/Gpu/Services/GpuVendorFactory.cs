namespace AINPC.Gpu.Services;

class GpuVendorFactory : IGpuVendorFactory
{
	/// <inheritdoc/>
	public GpuVendor GuessVendor(string name)
	{
		name = name.ToLowerInvariant();

		if (name.Contains("nvidia")) return GpuVendor.Nvidia;
		if (name.Contains("amd") || name.Contains("advanced micro devices") || name.Contains("radeon")) return GpuVendor.Amd;
		if (name.Contains("intel")) return GpuVendor.Intel;

		return GpuVendor.Unknown;
	}
}

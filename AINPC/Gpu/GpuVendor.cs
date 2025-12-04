namespace AINPC.Gpu;

public enum GpuVendor
{
	Unknown,
	Nvidia,
	Amd,
	Intel
}

public static class GpuVendorExtensions
{
	/// <summary>
	/// Get the vendor string to provide to the ollama command-line.
	/// </summary>
	public static string GetVendorString(this GpuVendor @this)
	{
		return @this switch
		{
			GpuVendor.Nvidia => "cuda",
			GpuVendor.Amd => "rocm",
			GpuVendor.Intel => "opencl",
			GpuVendor.Unknown => "none",
			_ => "none",
		};
	}
}
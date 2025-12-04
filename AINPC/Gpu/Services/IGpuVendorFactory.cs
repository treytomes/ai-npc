namespace AINPC.Gpu.Services;

interface IGpuVendorFactory
{
	/// <summary>
	/// Guess which vendor provided your GPU based on the name reported by the OS.
	/// </summary>
	GpuVendor GuessVendor(string name);
}

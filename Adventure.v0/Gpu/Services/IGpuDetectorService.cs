namespace Adventure.Gpu.Services;

interface IGpuDetectorService : IGpuDetector
{
	GpuVendor GetVendor();
}
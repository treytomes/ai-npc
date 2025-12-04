namespace AINPC.Gpu.Services;

interface IGpuDetectorService : IGpuDetector
{
	GpuVendor GetVendor();
}
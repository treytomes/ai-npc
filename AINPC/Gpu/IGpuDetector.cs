namespace AINPC.Gpu;

interface IGpuDetector
{
	IReadOnlyList<GpuInfo> Detect();
}

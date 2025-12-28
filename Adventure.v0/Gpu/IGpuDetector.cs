namespace Adventure.Gpu;

interface IGpuDetector
{
	IReadOnlyList<GpuInfo> Detect();
}

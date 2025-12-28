namespace Adventure.Gpu;

public sealed class GpuInfo
{
	public string Name { get; init; } = "";
	public GpuVendor Vendor { get; init; } = GpuVendor.Unknown;
	public string DriverVersion { get; init; } = "";
	public Dictionary<string, string> Extra { get; init; } = new();
}

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AINPC.OllamaRuntime;

public static class GpuDetector
{
	public static IReadOnlyList<GpuInfo> Detect()
	{
		if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			return DetectWindows();

		if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
			return DetectLinux();

		return Array.Empty<GpuInfo>();
	}

	// ---------------------------
	// WINDOWS DETECTION
	// ---------------------------
	private static IReadOnlyList<GpuInfo> DetectWindows()
	{
		try
		{
			// Use wmic (deprecated) but still functional on all Windows versions
			// wmic path win32_VideoController get Name,DriverVersion
			var output = RunProcess("wmic", "path win32_VideoController get Name,DriverVersion /format:list");

			var results = new List<GpuInfo>();

			var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
			string? name = null;
			string? driver = null;

			foreach (var line in lines)
			{
				if (line.StartsWith("Name=", StringComparison.OrdinalIgnoreCase))
					name = line.Substring("Name=".Length).Trim();

				if (line.StartsWith("DriverVersion=", StringComparison.OrdinalIgnoreCase))
					driver = line.Substring("DriverVersion=".Length).Trim();

				if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(driver))
				{
					results.Add(new GpuInfo
					{
						Name = name,
						DriverVersion = driver,
						Vendor = GuessVendor(name)
					});

					name = null;
					driver = null;
				}
			}

			return results;
		}
		catch
		{
			return Array.Empty<GpuInfo>();
		}
	}

	// ---------------------------
	// LINUX DETECTION
	// ---------------------------
	private static IReadOnlyList<GpuInfo> DetectLinux()
	{
		var results = new List<GpuInfo>();

		// lspci output: "VGA compatible controller: NVIDIA Corporation ..."
		var output = RunProcess("lspci", "");

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
				Vendor = GuessVendor(desc),
				DriverVersion = "" // could be populated with nvidia-smi, glxinfo, or vainfo later
			});
		}

		return results;
	}

	// ---------------------------
	// UTILITIES
	// ---------------------------
	private static GpuVendor GuessVendor(string name)
	{
		name = name.ToLowerInvariant();

		if (name.Contains("nvidia")) return GpuVendor.Nvidia;
		if (name.Contains("amd") || name.Contains("advanced micro devices") || name.Contains("radeon")) return GpuVendor.Amd;
		if (name.Contains("intel")) return GpuVendor.Intel;

		return GpuVendor.Unknown;
	}

	private static string RunProcess(string exe, string args)
	{
		try
		{
			var psi = new ProcessStartInfo
			{
				FileName = exe,
				Arguments = args,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				UseShellExecute = false,
			};

			using var p = Process.Start(psi);
			if (p == null)
				return "";

			var stdout = p.StandardOutput.ReadToEnd();
			var stderr = p.StandardError.ReadToEnd();

			p.WaitForExit(2500);

			return string.IsNullOrWhiteSpace(stdout) ? stderr : stdout;
		}
		catch
		{
			return "";
		}
	}
}

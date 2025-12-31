using System.Diagnostics;
using System.IO.Compression;
using System.Runtime.InteropServices;

public class PythonInstaller
{
	public event Action<int, string> ProgressChanged;
	public event Action<string> OutputReceived;

	private readonly string _appDataPath;
	private readonly string _pythonVersion = "3.11.7";
	private readonly HttpClient _httpClient = new HttpClient();
	private readonly bool _isWindows;
	private readonly bool _isLinux;

	public PythonInstaller()
	{
		_appDataPath = GetInstallDir();
		_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		_isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
	}

	protected string GetInstallDir() =>
		RuntimeInformation.IsOSPlatform(OSPlatform.Linux)
			? GetLinuxInstallDir()
			: (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
				? GetWindowsInstallDir()
				: throw new ApplicationException($"OS type not implemented: {RuntimeInformation.OSDescription}")
			);

	protected string GetWindowsInstallDir() =>
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
			"Adventure"
		);

	protected string GetLinuxInstallDir() =>
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			".adventure"
		);

	public async Task<string> InstallPythonAsync()
	{
		string pythonPath = Path.Combine(_appDataPath, $"python-{_pythonVersion}");

		if (Directory.Exists(pythonPath))
		{
			ReportProgress(100, "Python already installed.");
			return pythonPath;
		}

		// Check Linux dependencies before proceeding
		if (_isLinux)
		{
			ReportProgress(5, "Checking system dependencies...");
			bool dependenciesReady = await LinuxPythonHelper.EnsureDependencies();

			if (!dependenciesReady)
			{
				throw new Exception("Required system dependencies are not installed. " +
					"Please install them manually and try again.");
			}
		}

		Directory.CreateDirectory(_appDataPath);

		// Download with progress
		string archiveUrl = GetPythonDownloadUrl();
		string archivePath = Path.Combine(_appDataPath, Path.GetFileName(archiveUrl));

		await DownloadWithProgress(archiveUrl, archivePath);

		// Extract
		ReportProgress(80, "Extracting Python...");
		await ExtractPython(archivePath, pythonPath);
		File.Delete(archivePath);

		// Configure
		ReportProgress(90, "Configuring Python environment...");
		await ConfigurePythonEnvironment(pythonPath);

		ReportProgress(100, "Python installation complete!");
		return pythonPath;
	}

	private string GetPythonDownloadUrl()
	{
		if (_isWindows)
		{
			return $"https://www.python.org/ftp/python/{_pythonVersion}/python-{_pythonVersion}-embed-amd64.zip";
		}
		else if (_isLinux)
		{
			return $"https://www.python.org/ftp/python/{_pythonVersion}/Python-{_pythonVersion}.tgz";
		}
		else
		{
			throw new PlatformNotSupportedException();
		}
	}

	private async Task ExtractPython(string archivePath, string destinationPath)
	{
		if (_isWindows)
		{
			await Task.Run(() => ZipFile.ExtractToDirectory(archivePath, destinationPath));
		}
		else if (_isLinux)
		{
			Directory.CreateDirectory(destinationPath);

			await RunCommandWithOutput("tar", $"-xzf \"{archivePath}\" -C \"{_appDataPath}\"");

			string extractedDir = Path.Combine(_appDataPath, $"Python-{_pythonVersion}");
			if (Directory.Exists(extractedDir))
			{
				await CompilePythonLinux(extractedDir, destinationPath);
				Directory.Delete(extractedDir, true);
			}
		}
	}

	private async Task CompilePythonLinux(string sourceDir, string installDir)
	{
		ReportProgress(82, "Configuring Python build...");
		ReportOutput("Starting Python configuration...");

		// Configure with prefix to install in our custom directory
		// Added --with-ensurepip=install to ensure pip is included
		await RunCommandWithOutput(
			"/bin/bash",
			$"-c \"cd '{sourceDir}' && ./configure --prefix='{installDir}' --enable-shared --enable-optimizations --with-ensurepip=install\"",
			showRealTimeOutput: true
		);

		ReportProgress(85, "Compiling Python (this may take several minutes)...");
		ReportOutput("\nStarting Python compilation...");
		ReportOutput("This process may take 5-15 minutes depending on your system.\n");

		// Make with parallel jobs
		int cpuCount = Environment.ProcessorCount;
		await RunCommandWithOutput(
			"/bin/bash",
			$"-c \"cd '{sourceDir}' && make -j{cpuCount}\"",
			showRealTimeOutput: true
		);

		ReportProgress(88, "Installing Python...");
		ReportOutput("\nInstalling Python to custom directory...");

		// Install to our directory
		await RunCommandWithOutput(
			"/bin/bash",
			$"-c \"cd '{sourceDir}' && make install\"",
			showRealTimeOutput: true
		);

		// Update ldconfig to register the new shared library
		ReportProgress(89, "Registering shared libraries...");
		string libDir = Path.Combine(installDir, "lib");

		// Set LD_LIBRARY_PATH for the current process
		string currentLdPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";
		if (!currentLdPath.Contains(libDir))
		{
			Environment.SetEnvironmentVariable("LD_LIBRARY_PATH",
				string.IsNullOrEmpty(currentLdPath) ? libDir : $"{libDir}:{currentLdPath}");
		}

		try
		{
			await RunCommandWithOutput("sudo", $"sh -c 'echo \"{libDir}\" > /etc/ld.so.conf.d/python-custom.conf'");
			await RunCommandWithOutput("sudo", "ldconfig");
			ReportOutput("Shared libraries registered successfully.");
		}
		catch
		{
			ReportOutput($"Warning: Could not update ldconfig. You may need to set LD_LIBRARY_PATH={libDir}");
		}
	}

	private async Task DownloadWithProgress(string url, string destinationPath)
	{
		using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
		response.EnsureSuccessStatusCode();

		var totalBytes = response.Content.Headers.ContentLength ?? -1L;
		var canReportProgress = totalBytes != -1;

		using var contentStream = await response.Content.ReadAsStreamAsync();
		using var fileStream = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 8192, true);

		var buffer = new byte[8192];
		var totalRead = 0L;
		var read = 0;

		while ((read = await contentStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
		{
			await fileStream.WriteAsync(buffer, 0, read);
			totalRead += read;

			if (canReportProgress)
			{
				var progress = (int)((totalRead * 70L) / totalBytes);
				ReportProgress(progress, $"Downloading Python... {totalRead / 1024 / 1024}MB / {totalBytes / 1024 / 1024}MB");
			}
		}
	}

	private async Task ConfigurePythonEnvironment(string pythonPath)
	{
		if (_isWindows)
		{
			string pthFile = Directory.GetFiles(pythonPath, "*.pth").FirstOrDefault();
			if (pthFile != null)
			{
				string content = File.ReadAllText(pthFile);
				content = content.Replace("#import site", "import site");
				content += $"\nLib\\site-packages\n";
				File.WriteAllText(pthFile, content);
			}

			string sitePackages = Path.Combine(pythonPath, "Lib", "site-packages");
			Directory.CreateDirectory(sitePackages);

			string pythonExe = Path.Combine(pythonPath, "python.exe");
			await RunCommandWithOutput(pythonExe, "-m ensurepip --upgrade");
			await RunCommandWithOutput(pythonExe, "-m pip install --upgrade pip");
		}
		else if (_isLinux)
		{
			string pythonExe = Path.Combine(pythonPath, "bin", "python3");
			string pipExe = Path.Combine(pythonPath, "bin", "pip3");

			// First, check if pip is already installed
			if (File.Exists(pipExe))
			{
				ReportOutput("pip is already installed, upgrading...");
				try
				{
					await RunCommandWithOutput(pythonExe, "-m pip install --upgrade pip");
				}
				catch (Exception ex)
				{
					ReportOutput($"Warning: Failed to upgrade pip: {ex.Message}");
				}
			}
			else
			{
				// Try ensurepip first
				try
				{
					ReportOutput("Installing pip using ensurepip...");
					await RunCommandWithOutput(pythonExe, "-m ensurepip --upgrade");
					await RunCommandWithOutput(pythonExe, "-m pip install --upgrade pip");
				}
				catch (Exception ex)
				{
					ReportOutput($"ensurepip failed: {ex.Message}");
					ReportOutput("Trying alternative pip installation method...");

					// Alternative method: download get-pip.py
					await InstallPipAlternative(pythonPath);
				}
			}

			// Create python symlink if it doesn't exist
			string pythonLink = Path.Combine(pythonPath, "bin", "python");
			if (!File.Exists(pythonLink))
			{
				try
				{
					await RunCommandWithOutput("ln", $"-s python3 \"{pythonLink}\"");
				}
				catch
				{
					ReportOutput("Warning: Could not create python symlink");
				}
			}
		}
	}

	private async Task InstallPipAlternative(string pythonPath)
	{
		string pythonExe = Path.Combine(pythonPath, "bin", "python3");
		string getPipPath = Path.Combine(_appDataPath, "get-pip.py");

		try
		{
			// Download get-pip.py
			ReportOutput("Downloading get-pip.py...");
			using (var client = new HttpClient())
			{
				var getPipContent = await client.GetStringAsync("https://bootstrap.pypa.io/get-pip.py");
				await File.WriteAllTextAsync(getPipPath, getPipContent);
			}

			// Install pip using get-pip.py
			ReportOutput("Installing pip using get-pip.py...");

			// Set LD_LIBRARY_PATH for this command
			var startInfo = new ProcessStartInfo
			{
				FileName = pythonExe,
				Arguments = $"\"{getPipPath}\"",
				UseShellExecute = false,
				RedirectStandardOutput = true,
				RedirectStandardError = true,
				CreateNoWindow = true,
				WorkingDirectory = pythonPath
			};

			// Ensure LD_LIBRARY_PATH includes Python's lib directory
			string libDir = Path.Combine(pythonPath, "lib");
			startInfo.Environment["LD_LIBRARY_PATH"] = libDir;
			startInfo.Environment["PYTHONHOME"] = pythonPath;

			using var process = Process.Start(startInfo);

			var outputTask = Task.Run(async () =>
			{
				while (!process.StandardOutput.EndOfStream)
				{
					var line = await process.StandardOutput.ReadLineAsync();
					if (!string.IsNullOrWhiteSpace(line))
					{
						ReportOutput(line);
					}
				}
			});

			var errorTask = Task.Run(async () =>
			{
				while (!process.StandardError.EndOfStream)
				{
					var line = await process.StandardError.ReadLineAsync();
					if (!string.IsNullOrWhiteSpace(line))
					{
						ReportOutput($"[ERROR] {line}");
					}
				}
			});

			await Task.WhenAll(outputTask, errorTask);
			await process.WaitForExitAsync();

			if (process.ExitCode != 0)
			{
				throw new Exception("get-pip.py installation failed");
			}

			ReportOutput("pip installed successfully!");

			// Clean up
			if (File.Exists(getPipPath))
			{
				File.Delete(getPipPath);
			}
		}
		catch (Exception ex)
		{
			throw new Exception($"Failed to install pip: {ex.Message}", ex);
		}
	}

	private async Task RunCommandWithOutput(string fileName, string arguments, bool showRealTimeOutput = false)
	{
		var startInfo = new ProcessStartInfo
		{
			FileName = fileName,
			Arguments = arguments,
			UseShellExecute = false,
			RedirectStandardOutput = true,
			RedirectStandardError = true,
			CreateNoWindow = true
		};

		// For Linux, ensure LD_LIBRARY_PATH is set if we're running Python
		if (_isLinux && (fileName.Contains("python") || fileName.Contains("pip")))
		{
			string pythonHome = Path.GetDirectoryName(Path.GetDirectoryName(fileName));
			if (!string.IsNullOrEmpty(pythonHome))
			{
				string libDir = Path.Combine(pythonHome, "lib");
				string currentLdPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";
				if (!currentLdPath.Contains(libDir))
				{
					startInfo.Environment["LD_LIBRARY_PATH"] =
						string.IsNullOrEmpty(currentLdPath) ? libDir : $"{libDir}:{currentLdPath}";
				}
				startInfo.Environment["PYTHONHOME"] = pythonHome;
			}
		}

		using var process = Process.Start(startInfo);

		// Create tasks to read stdout and stderr simultaneously
		var outputTask = Task.Run(async () =>
		{
			while (!process.StandardOutput.EndOfStream)
			{
				var line = await process.StandardOutput.ReadLineAsync();
				if (line != null && showRealTimeOutput)
				{
					ReportOutput(line);
				}
			}
		});

		var errorTask = Task.Run(async () =>
		{
			while (!process.StandardError.EndOfStream)
			{
				var line = await process.StandardError.ReadLineAsync();
				if (line != null && showRealTimeOutput)
				{
					ReportOutput($"[ERROR] {line}");
				}
			}
		});

		await Task.WhenAll(outputTask, errorTask);
		await process.WaitForExitAsync();

		if (process.ExitCode != 0)
		{
			string error = await process.StandardError.ReadToEndAsync();
			throw new Exception($"Command failed: {fileName} {arguments}\nError: {error}");
		}
	}

	private void ReportProgress(int percentage, string message)
	{
		ProgressChanged?.Invoke(percentage, message);
	}

	private void ReportOutput(string message)
	{
		OutputReceived?.Invoke(message);
	}

	public string GetPythonExecutablePath()
	{
		string pythonPath = Path.Combine(_appDataPath, $"python-{_pythonVersion}");

		if (_isWindows)
		{
			return Path.Combine(pythonPath, "python.exe");
		}
		else if (_isLinux)
		{
			return Path.Combine(pythonPath, "bin", "python3");
		}
		else
		{
			throw new PlatformNotSupportedException();
		}
	}

	public string GetPipExecutablePath()
	{
		string pythonPath = Path.Combine(_appDataPath, $"python-{_pythonVersion}");

		if (_isWindows)
		{
			return Path.Combine(pythonPath, "Scripts", "pip.exe");
		}
		else if (_isLinux)
		{
			return Path.Combine(pythonPath, "bin", "pip3");
		}
		else
		{
			throw new PlatformNotSupportedException();
		}
	}
}
using System.Diagnostics;

namespace Adventure.LLM.Training.Installers;

internal sealed class LinuxPythonInstaller(string appName, ITextReader passwordReader)
	: PythonInstaller(passwordReader)
{
	private readonly string _appName = appName;

	protected override string GetPythonDownloadUrl() =>
		$"https://www.python.org/ftp/python/{_pythonVersion}/Python-{_pythonVersion}.tgz";

	protected override string GetInstallDir() =>
		Path.Combine(
			Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
			$".{_appName.ToLower()}"
		);

	protected override ProcessStartInfo PreprocessStartInfo(ProcessStartInfo startInfo)
	{
		// For Linux, ensure LD_LIBRARY_PATH is set if we're running Python
		if (startInfo.FileName.Contains("python") || startInfo.FileName.Contains("pip"))
		{
			var pythonHome = Path.GetDirectoryName(Path.GetDirectoryName(startInfo.FileName));
			if (!string.IsNullOrEmpty(pythonHome))
			{
				var libDir = Path.Combine(pythonHome, "lib");
				var currentLdPath = Environment.GetEnvironmentVariable("LD_LIBRARY_PATH") ?? "";
				if (!currentLdPath.Contains(libDir))
				{
					startInfo.Environment["LD_LIBRARY_PATH"] =
						string.IsNullOrEmpty(currentLdPath) ? libDir : $"{libDir}:{currentLdPath}";
				}
				startInfo.Environment["PYTHONHOME"] = pythonHome;
			}
		}
		return startInfo;
	}

	protected override async Task ExtractPython(string archivePath, string destinationPath)
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

	protected override async Task ConfigurePythonEnvironment(string pythonPath)
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

	public override string GetPythonExecutablePath()
	{
		string pythonPath = Path.Combine(_appDataPath, $"python-{_pythonVersion}");
		return Path.Combine(pythonPath, "bin", "python3");
	}

	public override string GetPipExecutablePath()
	{
		string pythonPath = Path.Combine(_appDataPath, $"python-{_pythonVersion}");
		return Path.Combine(pythonPath, "Scripts", "pip.exe");
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

			using var process = Process.Start(startInfo) ?? throw new NullReferenceException($"Unable to run '{pythonExe}'.");

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

	protected override async Task EnsureDependencies()
	{
		var requiredPackages = new[]
		{
			"build-essential",
			"libssl-dev",
			"zlib1g-dev",
			"libncurses5-dev",
			"libncursesw5-dev",
			"libreadline-dev",
			"libsqlite3-dev",
			"libgdbm-dev",
			"libdb5.3-dev",
			"libbz2-dev",
			"libexpat1-dev",
			"liblzma-dev",
			"libffi-dev",
			"uuid-dev",
			"python3-dev",
			"python3-pip",
		};

		ReportProgress(5, "Checking system dependencies...");
		var helper = new LinuxPackageManager(_passwordReader);
		helper.WhenOutputReceived.Subscribe(args => ReportOutput(args.OutputText));
		bool dependenciesReady = await helper.EnsurePackagesAsync(requiredPackages);

		if (!dependenciesReady)
		{
			throw new Exception("Required system dependencies are not installed. " +
				"Please install them manually and try again.");
		}
	}
}

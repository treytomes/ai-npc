using System.Runtime.InteropServices;
using Python.Runtime;
using Spectre.Console;

namespace Adventure.LLM.Training;

internal static class Program
{
	public static async Task Main(string[] args)
	{
		AnsiConsole.Write(
			new FigletText("Python Installer Test")
				.LeftJustified()
				.Color(Color.Blue));

		// Display system information
		var systemTable = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Property")
			.AddColumn("Value");

		systemTable.AddRow("Operating System", RuntimeInformation.OSDescription);
		systemTable.AddRow("Architecture", RuntimeInformation.ProcessArchitecture.ToString());
		systemTable.AddRow("Framework", RuntimeInformation.FrameworkDescription);

		AnsiConsole.Write(systemTable);
		AnsiConsole.WriteLine();

		try
		{
			// Test 1: Python Installation
			await TestPythonInstallation();

			// Test 2: Environment Setup
			var pythonHome = await TestEnvironmentSetup();

			// Test 3: Package Manager
			await TestPackageManager(pythonHome);

			// Test 4: Python.NET Integration
			await TestPythonNetIntegration();

			// Success summary
			AnsiConsole.Write(
				new Panel(
					new Markup("[green]✓ All tests passed successfully![/]")
				)
				.Border(BoxBorder.Double)
				.BorderColor(Color.Green)
				.Header("[bold]Test Results[/]"));
		}
		catch (Exception ex)
		{
			AnsiConsole.Write(
				new Panel(
					new Markup($"[red]✗ Test failed:[/]\n{Markup.Escape(ex.Message)}")
				)
				.Border(BoxBorder.Double)
				.BorderColor(Color.Red)
				.Header("[bold red]Error[/]"));

			AnsiConsole.WriteLine();
			AnsiConsole.WriteException(ex);
		}
	}

	static async Task TestPythonInstallation()
	{
		var rule = new Rule("[yellow]Test 1: Python Installation[/]");
		rule.LeftJustified();
		AnsiConsole.Write(rule);

		var installer = new PythonInstaller();

		string currentStatus = "Starting installation...";
		bool showingOutput = false;

		installer.WhenProgressChanged.Subscribe(args =>
		{
			currentStatus = args.Message;
			if (!showingOutput)
			{
				AnsiConsole.MarkupLine($"[grey][[{args.Percentage}%]][/] [yellow]{Markup.Escape(args.Message)}[/]");
			}
		});

		installer.WhenOutputReceived.Subscribe(args =>
		{
			showingOutput = true;
			// Display build output in dim color so it's visible but not overwhelming
			AnsiConsole.MarkupLine($"[dim]{Markup.Escape(args.OutputText)}[/]");
		});

		string pythonPath = await installer.InstallPythonAsync();

		var resultsTable = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Check")
			.AddColumn("Status")
			.AddColumn("Details");

		// Verify installation
		bool pathExists = Directory.Exists(pythonPath);
		resultsTable.AddRow(
			"Installation Path",
			pathExists ? "[green]✓[/]" : "[red]✗[/]",
			Markup.Escape(pythonPath)
		);

		string pythonExe = installer.GetPythonExecutablePath();
		bool exeExists = File.Exists(pythonExe);
		resultsTable.AddRow(
			"Python Executable",
			exeExists ? "[green]✓[/]" : "[red]✗[/]",
			Markup.Escape(pythonExe)
		);

		string pipExe = installer.GetPipExecutablePath();
		bool pipExists = File.Exists(pipExe);
		resultsTable.AddRow(
			"Pip Executable",
			pipExists ? "[green]✓[/]" : "[red]✗[/]",
			Markup.Escape(pipExe)
		);

		AnsiConsole.Write(resultsTable);
		AnsiConsole.WriteLine();

		if (!pathExists || !exeExists)
		{
			throw new Exception("Python installation verification failed!");
		}
	}

	static async Task<string> TestEnvironmentSetup()
	{
		string? pythonHome = null;

		var rule = new Rule("[yellow]Test 2: Environment Setup[/]");
		rule.LeftJustified();
		AnsiConsole.Write(rule);

		var envManager = new PythonEnvironmentManager("Adventure");

		// Subscribe to output events
		envManager.WhenOutputReceived.Subscribe(args =>
		{
			AnsiConsole.MarkupLine($"[dim]{Markup.Escape(args.OutputText)}[/]");
		});

		AnsiConsole.MarkupLine("[yellow]Setting up Python environment...[/]");
		bool setupSuccess = await envManager.SetupEnvironmentAsync();

		if (!setupSuccess)
		{
			throw new Exception("Environment setup failed!");
		}

		pythonHome = envManager.GetPythonHome() ?? throw new NullReferenceException("Python home isn't set.");

		var resultsTable = new Table()
			.Border(TableBorder.Rounded)
			.AddColumn("Check")
			.AddColumn("Status");

		resultsTable.AddRow(
			"Environment Setup",
			setupSuccess ? "[green]✓ Completed[/]" : "[red]✗ Failed[/]"
		);

		AnsiConsole.MarkupLine("[yellow]Verifying Python library...[/]");
		bool libraryValid = envManager.VerifyPythonLibrary();
		resultsTable.AddRow(
			"Python Library",
			libraryValid ? "[green]✓ Valid[/]" : "[red]✗ Invalid[/]"
		);

		AnsiConsole.Write(resultsTable);
		AnsiConsole.WriteLine();

		if (!libraryValid)
		{
			AnsiConsole.MarkupLine("[yellow]Warning: Python library verification failed. Python.NET may not work correctly.[/]");
		}

		return pythonHome;
	}

	static async Task TestPackageManager(string pythonHome)
	{
		await AnsiConsole.Status()
			.StartAsync("Testing Package Manager...", async ctx =>
			{
				var rule = new Rule("[yellow]Test 3: Package Manager[/]");
				rule.LeftJustified();
				AnsiConsole.Write(rule);

				var packageManager = new PythonPackageManager(pythonHome);

				var testPackages = new[] { "numpy", "torch", "transformers" };
				var resultsTable = new Table()
					.Border(TableBorder.Rounded)
					.AddColumn("Package")
					.AddColumn("Status")
					.AddColumn("Action");

				foreach (var package in testPackages)
				{
					ctx.Status($"[yellow]Checking {package}...[/]");

					bool isInstalled = await packageManager.IsPackageInstalled(package);

					if (isInstalled)
					{
						resultsTable.AddRow(
							package,
							"[green]✓ Installed[/]",
							"[dim]Already present[/]"
						);
					}
					else
					{
						resultsTable.AddRow(
							package,
							"[yellow]○ Not Installed[/]",
							"[dim]Available for install[/]"
						);
					}
				}

				AnsiConsole.Write(resultsTable);
				AnsiConsole.WriteLine();

				// Test installing a small package
				string testPackage = "six";
				bool sixInstalled = await packageManager.IsPackageInstalled(testPackage);

				if (!sixInstalled)
				{
					AnsiConsole.MarkupLine($"[yellow]Installing test package '{testPackage}'...[/]");

					await AnsiConsole.Progress()
						.Columns(
							new TaskDescriptionColumn(),
							new ProgressBarColumn(),
							new SpinnerColumn()
						)
						.StartAsync(async progressCtx =>
						{
							var task = progressCtx.AddTask($"Installing {testPackage}");
							task.IsIndeterminate = true;

							await packageManager.InstallPackageWithDependencies(testPackage);

							task.Value = 100;
							task.StopTask();
						});

					bool verifyInstall = await packageManager.IsPackageInstalled(testPackage);
					AnsiConsole.MarkupLine(
						verifyInstall
							? $"[green]✓ {testPackage} installed successfully![/]"
							: $"[red]✗ {testPackage} installation failed![/]"
					);
				}
				else
				{
					AnsiConsole.MarkupLine($"[green]✓ Test package '{testPackage}' already installed[/]");
				}

				AnsiConsole.WriteLine();
			});
	}

	static async Task TestPythonNetIntegration()
	{
		await AnsiConsole.Status()
			.StartAsync("Testing Python.NET Integration...", async ctx =>
			{
				var rule = new Rule("[yellow]Test 4: Python.NET Integration[/]");
				rule.LeftJustified();
				AnsiConsole.Write(rule);

				var envManager = new PythonEnvironmentManager("Adventure");
				await envManager.SetupEnvironmentAsync();

				ctx.Status("[yellow]Initializing Python.NET...[/]");
				envManager.Initialize();

				var resultsTable = new Table()
					.Border(TableBorder.Rounded)
					.AddColumn("Test")
					.AddColumn("Status")
					.AddColumn("Details");

				// Test 1: Python engine initialized
				bool isInitialized = PythonEngine.IsInitialized;
				resultsTable.AddRow(
					"Python Engine",
					isInitialized ? "[green]✓[/]" : "[red]✗[/]",
					isInitialized ? "Initialized" : "Not initialized"
				);

				if (!isInitialized)
				{
					throw new Exception("Python.NET initialization failed!");
				}

				// Test 2: Import sys and check version
				try
				{
					using (Py.GIL())
					{
						dynamic sys = Py.Import("sys");
						string version = sys.version.ToString();
						string executable = sys.executable.ToString();

						resultsTable.AddRow(
							"Python Version",
							"[green]✓[/]",
							Markup.Escape(version.Split('\n')[0])
						);

						resultsTable.AddRow(
							"Python Executable",
							"[green]✓[/]",
							Markup.Escape(executable)
						);

						// Test 3: Import installed packages
						var packagesToTest = new[] { "numpy", "six" };

						foreach (var pkg in packagesToTest)
						{
							try
							{
								dynamic module = Py.Import(pkg);
								var pkgVersion = "N/A";

								try
								{
									// Use HasAttr to check if __version__ exists
									if (module.HasAttr("__version__"))
									{
										PyObject versionObj = module.__version__;
										pkgVersion = versionObj.ToString();
									}
								}
								catch { }

								resultsTable.AddRow(
									$"Import {pkg}",
									"[green]✓[/]",
									pkgVersion != "N/A" ? $"v{pkgVersion}" : "Imported"
								);
							}
							catch (Exception ex)
							{
								resultsTable.AddRow(
									$"Import {pkg}",
									"[yellow]○[/]",
									$"[dim]{Markup.Escape(ex.Message.Split('\n')[0])}[/]"
								);
							}
						}

						// Test 4: Execute simple Python code
						try
						{
							using (PyObject result = PythonEngine.Eval("2 + 2"))
							{
								int value = result.As<int>();
								resultsTable.AddRow(
									"Execute Python Code",
									value == 4 ? "[green]✓[/]" : "[red]✗[/]",
									$"2 + 2 = {value}"
								);
							}
						}
						catch (Exception ex)
						{
							resultsTable.AddRow(
								"Execute Python Code",
								"[red]✗[/]",
								Markup.Escape(ex.Message)
							);
						}

						// Test 5: Create Python objects
						try
						{
							using (var scope = Py.CreateScope())
							{
								scope.Exec("x = [1, 2, 3, 4, 5]");
								scope.Exec("y = sum(x)");

								using (PyObject pySum = scope.Get("y"))
								{
									int sum = pySum.As<int>();
									resultsTable.AddRow(
										"Python Objects",
										sum == 15 ? "[green]✓[/]" : "[red]✗[/]",
										$"sum({Markup.Escape("[1,2,3,4,5]")}) = {sum}"
									);
								}
							}
						}
						catch (Exception ex)
						{
							resultsTable.AddRow(
								"Python Objects",
								"[red]✗[/]",
								Markup.Escape(ex.Message)
							);
						}

						// Test 6: NumPy operations (if numpy is installed)
						try
						{
							using (PyObject np = Py.Import("numpy"))
							{
								using (PyObject arr = np.InvokeMethod("array", PyObject.FromManagedObject(new[] { 1, 2, 3, 4, 5 })))
								using (PyObject mean = np.InvokeMethod("mean", arr))
								{
									double meanValue = mean.As<double>();
									resultsTable.AddRow(
										"NumPy Operations",
										Math.Abs(meanValue - 3.0) < 0.001 ? "[green]✓[/]" : "[red]✗[/]",
										$"mean({Markup.Escape("[1,2,3,4,5]")}) = {meanValue:F1}"
									);
								}
							}
						}
						catch (Exception ex)
						{
							resultsTable.AddRow(
								"NumPy Operations",
								"[yellow]○[/]",
								$"[dim]{Markup.Escape(ex.Message.Split('\n')[0])}[/]"
							);
						}
					}
				}
				catch (Exception ex)
				{
					resultsTable.AddRow(
						"Python Integration",
						"[red]✗[/]",
						Markup.Escape(ex.Message)
					);
					throw;
				}

				AnsiConsole.Write(resultsTable);
				AnsiConsole.WriteLine();

				// Shutdown - wrap in try-catch to see if error is here
				try
				{
					ctx.Status("[yellow]Shutting down Python.NET...[/]");
					envManager.Shutdown();
				}
				catch (Exception ex)
				{
					AnsiConsole.MarkupLine($"[yellow]Warning during shutdown: {Markup.Escape(ex.Message)}[/]");
					// Don't rethrow - shutdown errors are often not critical
				}
			});
	}
}

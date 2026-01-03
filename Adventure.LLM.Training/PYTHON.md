# Python Integration Library for .NET

## Overview

This library provides a comprehensive solution for integrating Python with .NET applications on Linux systems. It handles Python installation, environment management, package management, and Python.NET integration with proper privilege elevation when needed.

## Key Features

- **Automated Python Installation**: Downloads and builds Python from source with all necessary dependencies
- **Environment Management**: Creates isolated Python environments for your applications
- **Package Management**: Install and manage Python packages programmatically
- **Privilege Elevation**: Handles sudo/pkexec authentication for system-level operations
- **Python.NET Integration**: Seamlessly execute Python code from C#

## Prerequisites

- Linux-based operating system (Ubuntu, Debian, etc.)
- .NET 6.0 or later
- Internet connection for downloading Python and packages

## Quick Start

### 1. Basic Python Installation

```csharp
using Adventure.LLM.Training;

// Create a Python installer
var installer = new PythonFactory().GetInstaller("MyApp", new ConsolePasswordTextReader());

// Subscribe to progress updates
installer.WhenProgressChanged.Subscribe(args =>
{
    Console.WriteLine($"[{args.Percentage}%] {args.Message}");
});

// Install Python
string pythonPath = await installer.InstallPythonAsync();
Console.WriteLine($"Python installed at: {pythonPath}");
```

### 2. Setting Up a Python Environment

```csharp
// Create an environment manager
var envManager = new PythonFactory().GetEnvironmentManager("AppName");

// Setup the environment
bool success = await envManager.SetupEnvironmentAsync();

if (success)
{
    // Initialize Python.NET
    envManager.Initialize();
    
    // Your Python code here
    using (Py.GIL())
    {
        dynamic sys = Py.Import("sys");
        Console.WriteLine($"Python version: {sys.version}");
    }
    
    // Shutdown when done
    envManager.Shutdown();
}
```

### 3. Installing Python Packages

```csharp
var pythonHome = envManager.GetPythonHome();
var packageManager = new PythonFactory().GetPackageManager(pythonHome);

// Check if a package is installed
bool isInstalled = await packageManager.IsPackageInstalledAsync("numpy");

// Install a package with dependencies
if (!isInstalled)
{
    await packageManager.InstallPackageWithDependenciesAsync("numpy");
}
```

## Core Components

### PythonFactory

The main entry point for creating Python-related components:

```csharp
var factory = new PythonFactory();
var installer = factory.GetInstaller(passwordReader);
var envManager = factory.GetEnvironmentManager("AppName");
var packageManager = factory.GetPackageManager(pythonHome);
```

### SudoSession

Handles privilege elevation for system operations:

```csharp
using (var sudoSession = new SudoSession(new ConsolePasswordTextReader()))
{
    if (await sudoSession.ActivateAsync())
    {
        // Execute elevated commands
        var result = await sudoSession.ExecuteElevatedAsync("apt-get", "update");
        Console.WriteLine(result.StandardOutput);
    }
}
```

### Password Readers

The library supports different password input methods:

```csharp
// Console input (interactive)
var consoleReader = new ConsolePasswordTextReader();

// Environment variable (non-interactive)
class EnvironmentPasswordReader : ITextReader
{
	private readonly string _variableName;

	public EnvironmentPasswordReader(string variableName)
	{
		_variableName = variableName;
	}

	public string Read()
	{
		return Environment.GetEnvironmentVariable(_variableName) ?? string.Empty;
	}
}
var envReader = new EnvironmentPasswordReader("SUDO_PASSWORD");

// Custom implementation
public class CustomPasswordReader : ITextReader
{
    public string Read()
    {
        // Your custom password retrieval logic
        return GetPasswordFromSecureStorage();
    }
}
```

## Advanced Usage

### System Dependencies Installation

```csharp
using (var systemHelper = new LinuxSystemHelper(new ConsolePasswordTextReader()))
{
    // Subscribe to output
    systemHelper.WhenOutputReceived.Subscribe(e => 
        Console.WriteLine($"[System] {e.OutputText}"));

    // Define required packages
    var requiredPackages = new[]
    {
        "build-essential",
        "libssl-dev",
        "python3-dev",
        "python3-pip"
    };

    // Install dependencies
    bool success = await systemHelper.EnsureDependenciesAsync(requiredPackages);
}
```

### Python.NET Integration Examples

```csharp
// Initialize environment
var envManager = new PythonFactory().GetEnvironmentManager("MyApp");
await envManager.SetupEnvironmentAsync();
envManager.Initialize();

using (Py.GIL())
{
    // Import modules
    dynamic np = Py.Import("numpy");
    dynamic torch = Py.Import("torch");
    
    // Create numpy array
    var data = new[] { 1, 2, 3, 4, 5 };
    using (var arr = np.array(data))
    {
        var mean = np.mean(arr);
        Console.WriteLine($"Mean: {mean}");
    }
    
    // Execute Python code
    using (var scope = Py.CreateScope())
    {
        scope.Exec(@"
def fibonacci(n):
    a, b = 0, 1
    for _ in range(n):
        yield a
        a, b = b, a + b

result = list(fibonacci(10))
");
        
        var result = scope.Get("result");
        foreach (var num in result)
        {
            Console.WriteLine(num);
        }
    }
}

// Cleanup
envManager.Shutdown();
```

### Progress Monitoring

```csharp
var installer = new PythonFactory().GetInstaller(passwordReader);

// Progress updates
installer.WhenProgressChanged.Subscribe(args =>
{
    Console.WriteLine($"[{args.Percentage}%] {args.Message}");
});

// Output monitoring
installer.WhenOutputReceived.Subscribe(args =>
{
    if (args.OutputText.Contains("error", StringComparison.OrdinalIgnoreCase))
    {
        Console.ForegroundColor = ConsoleColor.Red;
    }
    Console.WriteLine(args.OutputText);
    Console.ResetColor();
});
```

## Authentication Methods

The library supports multiple authentication methods:

1. **PolicyKit (pkexec)**: Graphical authentication dialog
2. **SSH Askpass**: Various graphical sudo helpers
3. **Console**: Traditional password prompt

The library automatically tries methods in order of user-friendliness:
- First checks for existing sudo access
- Tries pkexec for graphical authentication
- Falls back to SSH askpass helpers
- Finally uses console input if available

## Error Handling

```csharp
try
{
    var installer = new PythonFactory().GetInstaller(passwordReader);
    await installer.InstallPythonAsync();
}
catch (InvalidOperationException ex)
{
    // Handle installation errors
    Console.WriteLine($"Installation failed: {ex.Message}");
}
catch (UnauthorizedAccessException ex)
{
    // Handle permission errors
    Console.WriteLine($"Permission denied: {ex.Message}");
}
```

## Best Practices

1. **Always dispose resources**:
   ```csharp
   using (var sudoSession = new SudoSession(passwordReader))
   {
       // Your code here
   } // Automatically revokes sudo access
   ```

2. **Handle non-interactive scenarios**:
   ```csharp
   ITextReader passwordReader = Console.IsInputRedirected
       ? new EnvironmentPasswordReader("SUDO_PASSWORD")
       : new ConsolePasswordTextReader();
   ```

3. **Monitor output for debugging**:
   ```csharp
   component.WhenOutputReceived.Subscribe(e =>
   {
       logger.LogDebug(e.OutputText);
   });
   ```

4. **Check prerequisites before operations**:
   ```csharp
   if (!await systemHelper.IsPackageInstalled("python3-dev"))
   {
       await systemHelper.EnsureDependenciesAsync(new[] { "python3-dev" });
   }
   ```

## Troubleshooting

### Python Installation Fails

1. Check internet connectivity
2. Ensure sufficient disk space (at least 500MB)
3. Verify system dependencies are installed
4. Check build output for specific errors

### Sudo Access Issues

1. Ensure user has sudo privileges
2. For headless systems, set `SUDO_PASSWORD` environment variable
3. Install `pkexec` for graphical authentication: `sudo apt-get install policykit-1`
4. Check if SSH askpass helpers are installed for GUI environments

### Python.NET Initialization Errors

```csharp
// Verify Python library before initialization
var envManager = new PythonFactory().GetEnvironmentManager("MyApp");
if (!envManager.VerifyPythonLibrary())
{
    Console.WriteLine("Warning: Python library verification failed");
    // May need to rebuild Python or check LD_LIBRARY_PATH
}
```

### Package Installation Failures

```csharp
var packageManager = new PythonFactory().GetPackageManager(pythonHome);

// Enable verbose output for debugging
packageManager.WhenOutputReceived.Subscribe(e =>
{
    Console.WriteLine($"[pip] {e.OutputText}");
});

// Check pip is working
var pipVersion = await packageManager.GetPipVersionAsync();
Console.WriteLine($"Pip version: {pipVersion}");
```

## Configuration Options

### Custom Python Version

```csharp
public class CustomPythonInstaller : PythonInstaller
{
    protected override string PythonVersion => "3.11.6";
    protected override string DownloadUrl => 
        $"https://www.python.org/ftp/python/{PythonVersion}/Python-{PythonVersion}.tgz";
}
```

### Custom Installation Directory

```csharp
public class CustomPythonInstaller : PythonInstaller
{
    public CustomPythonInstaller(ITextReader passwordReader) 
        : base(passwordReader)
    {
    }
    
    protected override string GetInstallationPath()
    {
        return "/opt/mypython";
    }
}
```

### Environment Variables

The library respects several environment variables:

- `PYTHON_INSTALL_DIR`: Override default Python installation directory
- `SUDO_PASSWORD`: Password for non-interactive sudo authentication
- `SUDO_ASKPASS`: Path to graphical sudo helper
- `PYTHONHOME`: Python home directory (set automatically by the library)
- `LD_LIBRARY_PATH`: Library search path (updated automatically)

## Security Considerations

1. **Password Handling**: Passwords are cleared from memory after use
2. **Sudo Session**: Credentials are revoked when `SudoSession` is disposed
3. **Temporary Files**: Build artifacts are cleaned up after installation
4. **Process Isolation**: Each Python environment is isolated from others

## Performance Tips

1. **Cache Package Checks**:
   ```csharp
   private readonly Dictionary<string, bool> _packageCache = new();
   
   public async Task<bool> IsPackageInstalledCached(string package)
   {
       if (_packageCache.TryGetValue(package, out bool cached))
           return cached;
           
       bool installed = await packageManager.IsPackageInstalledAsync(package);
       _packageCache[package] = installed;
       return installed;
   }
   ```

2. **Batch Package Installation**:
   ```csharp
   // Instead of installing one by one
   foreach (var pkg in packages)
   {
       await packageManager.InstallPackageAsync(pkg);
   }
   
   // Install all at once
   await packageManager.InstallPackagesAsync(packages);
   ```

3. **Reuse Python Sessions**:
   ```csharp
   // Initialize once
   envManager.Initialize();
   
   // Use multiple times
   for (int i = 0; i < 100; i++)
   {
       using (Py.GIL())
       {
           // Your Python operations
       }
   }
   
   // Shutdown once
   envManager.Shutdown();
   ```

## Complete Example Application

```csharp
using System;
using System.Threading.Tasks;
using Adventure.LLM.Training;
using Python.Runtime;

class PythonIntegrationApp
{
    static async Task Main(string[] args)
    {
        try
        {
            // Step 1: Setup Python
            var installer = new PythonFactory().GetInstaller(
                new ConsolePasswordTextReader());
            
            Console.WriteLine("Installing Python...");
            var pythonPath = await installer.InstallPythonAsync();
            
            // Step 2: Setup Environment
            var envManager = new PythonFactory()
                .GetEnvironmentManager("DataScience");
            
            Console.WriteLine("Setting up environment...");
            await envManager.SetupEnvironmentAsync();
            
            // Step 3: Install Required Packages
            var packageManager = new PythonFactory()
                .GetPackageManager(envManager.GetPythonHome());
            
            var packages = new[] { "numpy", "pandas", "matplotlib" };
            foreach (var pkg in packages)
            {
                if (!await packageManager.IsPackageInstalledAsync(pkg))
                {
                    Console.WriteLine($"Installing {pkg}...");
                    await packageManager.InstallPackageAsync(pkg);
                }
            }
            
            // Step 4: Run Python Code
            envManager.Initialize();
            
            using (Py.GIL())
            {
                // Data analysis example
                using (var scope = Py.CreateScope())
                {
                    scope.Exec(@"
import numpy as np
import pandas as pd

# Create sample data
data = {
    'Name': ['Alice', 'Bob', 'Charlie', 'David'],
    'Age': [25, 30, 35, 28],
    'Score': [85, 92, 78, 95]
}

df = pd.DataFrame(data)
mean_age = df['Age'].mean()
max_score = df['Score'].max()

summary = f'Average age: {mean_age}, Highest score: {max_score}'
");
                    
                    var summary = scope.Get("summary").ToString();
                    Console.WriteLine(summary);
                }
            }
            
            envManager.Shutdown();
            Console.WriteLine("Done!");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
```

## API Reference

### IPythonInstaller
- `Task<string> InstallPythonAsync()` - Installs Python and returns installation path
- `string GetPythonExecutablePath()` - Gets path to python executable
- `string GetPipExecutablePath()` - Gets path to pip executable
- `IObservable<InstallProgressEventArgs> WhenProgressChanged` - Installation progress
- `IObservable<OutputReceivedEventArgs> WhenOutputReceived` - Output stream

### IPythonEnvironmentManager
- `Task<bool> SetupEnvironmentAsync()` - Sets up Python environment
- `void Initialize()` - Initializes Python.NET
- `void Shutdown()` - Shuts down Python.NET
- `string? GetPythonHome()` - Gets PYTHONHOME path
- `bool VerifyPythonLibrary()` - Verifies Python shared library

### IPythonPackageManager
- `Task<bool> IsPackageInstalledAsync(string packageName)` - Checks if package is installed
- `Task InstallPackageAsync(string packageName)` - Installs a single package
- `Task InstallPackagesAsync(IEnumerable<string> packageNames)` - Installs multiple packages
- `Task InstallPackage(string packageName)` - Installs package with deps
- `Task<string> GetPipVersionAsync()` - Gets pip version

### SudoSession
- `Task<bool> ActivateAsync()` - Activates sudo/pkexec session
- `Task<ProcessResult> ExecuteElevatedAsync(string command, string arguments)` - Executes elevated command
- `bool UsePkexec` - Whether using pkexec instead of sudo
- `IObservable<OutputReceivedEventArgs> WhenOutputReceived` - Output stream

## License

This library is provided under the MIT License. See LICENSE file for details.

## Contributing

Contributions are welcome! Please submit pull requests or issues on the project repository.

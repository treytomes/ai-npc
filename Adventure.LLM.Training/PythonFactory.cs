using System.Runtime.InteropServices;
using Adventure.LLM.Training.Installers;
using Adventure.LLM.Training.PackageManagers;

namespace Adventure.LLM.Training;

internal interface IPythonFactory
{
	IPythonInstaller GetInstaller();
}

internal class PythonFactory
{
	#region Fields

	private readonly bool _isWindows;
	private readonly bool _isLinux;

	#endregion

	#region Constructors

	public PythonFactory()
	{
		_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		_isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

		if (!_isWindows && !_isLinux)
		{
			throw new PlatformNotSupportedException();
		}
	}

	#endregion

	#region Methods

	public IPythonInstaller GetInstaller(ITextReader passwordReader)
	{
		if (_isWindows)
		{
			return new WindowsPythonInstaller(passwordReader);
		}
		return new LinuxPythonInstaller(passwordReader);
	}

	public IPythonPackageManager GetPackageManager(string pythonHome)
	{
		if (_isWindows)
		{
			return new WindowsPythonPackageManager(pythonHome);
		}
		return new LinuxPythonPackageManager(pythonHome);
	}

	#endregion
}
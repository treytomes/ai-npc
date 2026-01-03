using System.Runtime.InteropServices;
using Adventure.LLM.Training.Installers;

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
	private readonly ITextReader _passwordReader;

	#endregion

	#region Constructors

	public PythonFactory(ITextReader passwordReader)
	{
		_passwordReader = passwordReader ?? throw new ArgumentNullException(nameof(passwordReader));
		_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
		_isLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);

		if (!_isWindows && !_isLinux)
		{
			throw new PlatformNotSupportedException();
		}
	}

	#endregion

	#region Methods

	public IPythonInstaller GetInstaller()
	{
		if (_isWindows)
		{
			return new WindowsPythonInstaller(_passwordReader);
		}
		return new LinuxPythonInstaller(_passwordReader);
	}

	#endregion
}
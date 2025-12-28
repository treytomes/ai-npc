namespace Adventure.Services;

interface IProcessService
{
	/// <summary>
	/// Helper function to run a process and return the standard and error string outputs.
	/// </summary>
	string RunProcess(string exe, string args);
}

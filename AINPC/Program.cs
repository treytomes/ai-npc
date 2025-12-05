namespace AINPC;

class Program
{
	static async Task Main(params string[] args)
	{
		await Bootstrap.Start<AppSettings, MainState>(args);
	}
}

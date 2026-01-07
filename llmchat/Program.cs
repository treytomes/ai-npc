using Avalonia;
using Avalonia.Controls;
using System;

namespace llmchat;

sealed class Program
{
	// Initialization code. Don't use any Avalonia, third-party APIs or any
	// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
	// yet and stuff might break.
	[STAThread]
	public static void Main(string[] args)
	{
		// Build host + DI + logging
		var exitCode = Bootstrap.Start(args);
		if (exitCode != 0)
			return;

		BuildAvaloniaApp(Bootstrap.Host.Services)
			.StartWithClassicDesktopLifetime(args, lifetime =>
			{
				lifetime.ShutdownMode = ShutdownMode.OnMainWindowClose;
			});
	}

	public static AppBuilder BuildAvaloniaApp(IServiceProvider services)
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace();
}

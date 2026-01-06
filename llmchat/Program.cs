using Avalonia;
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

		BuildAvaloniaApp()
			.StartWithClassicDesktopLifetime(args);
	}

	public static AppBuilder BuildAvaloniaApp()
		=> AppBuilder.Configure<App>()
			.UsePlatformDetect()
			.WithInterFont()
			.LogToTrace();
}

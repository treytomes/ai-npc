using Adventure.LLM;
using Adventure.LLM.Services;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core.Plugins;
using Avalonia.Markup.Xaml;
using llmchat.Plugins;
using llmchat.Services;
using llmchat.ViewModels;
using llmchat.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace llmchat;

public partial class App : Application
{
	public static IClipboardService ClipboardService { get; private set; } = null!;
	public static bool IsKernelReady { get; private set; } = false;

	public override void Initialize()
	{
		AvaloniaXamlLoader.Load(this);
	}

	public override void OnFrameworkInitializationCompleted()
	{
		if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
		{
			base.OnFrameworkInitializationCompleted();
			return;
		}

		DisableAvaloniaDataAnnotationValidation();

		var loadingVm = new LoadingWindowViewModel();
		var loadingWindow = new LoadingWindow
		{
			DataContext = loadingVm
		};

		desktop.MainWindow = loadingWindow;
		loadingWindow.Show();

		// 🔑 Continue startup AFTER dispatcher is live
		_ = ContinueStartupAsync(desktop, loadingVm, loadingWindow);

		base.OnFrameworkInitializationCompleted();
	}

	private async Task ContinueStartupAsync(
		IClassicDesktopStyleApplicationLifetime desktop,
		LoadingWindowViewModel loadingVm,
		LoadingWindow loadingWindow)
	{
		try
		{
			var services = Bootstrap.Host.Services;
			var logger = services.GetRequiredService<ILogger<App>>();

			var holder = services.GetRequiredService<ChatClientHolder>();

			// Progress updates (UI-thread safe)
			holder.StatusChanged += message =>
			{
				Avalonia.Threading.Dispatcher.UIThread.Post(() =>
				{
					loadingVm.StatusText = message;
				});
			};

			// 🔑 Now it is safe to await
			await holder.Ready;

			var kernel = services.GetRequiredService<Kernel>();
			kernel.Plugins.AddFromObject(
				new AssistantPlugin(kernel),
				"Assistant");

			IsKernelReady = true;

			await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
			{
				var mainWindow = new MainWindow
				{
					DataContext = services.GetRequiredService<MainWindowViewModel>()
				};

				desktop.MainWindow = mainWindow;
				mainWindow.Show();

				loadingWindow.Hide(); // 🔑 do not Close
			});

		}
		catch (Exception ex)
		{
			Console.WriteLine(ex);
			throw;
		}
	}

	private void DisableAvaloniaDataAnnotationValidation()
	{
		// Get an array of plugins to remove
		var dataValidationPluginsToRemove =
			BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

		// remove each entry found
		foreach (var plugin in dataValidationPluginsToRemove)
		{
			BindingPlugins.DataValidators.Remove(plugin);
		}
	}

	private void OnShutdownRequested(object? sender, ShutdownRequestedEventArgs e)
	{
		DisposeLlm();
	}

	private void OnProcessExit(object? sender, EventArgs e)
	{
		DisposeLlm();
	}

	private void DisposeLlm()
	{
		try
		{
			Bootstrap.Host.Services.GetRequiredService<ILlmManager>().Dispose();
		}
		catch
		{
			// Last-gasp cleanup; swallow exceptions.
		}
	}
}
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace llmchat.Services;

public sealed class ClipboardService : IClipboardService
{
	public async Task SetTextAsync(string text)
	{
		var clipboard = Application.Current?
			.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop
			? desktop.MainWindow?.Clipboard
			: null;

		if (clipboard != null)
			await clipboard.SetTextAsync(text);
	}
}

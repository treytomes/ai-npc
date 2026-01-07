using System;
using llmchat.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace llmchat.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
	public MainWindowViewModel(IServiceProvider services, IChatHistoryRepository repo)
	{
		Chat = new(services.GetRequiredService<Kernel>(), repo.CreateEmpty());
	}

	public ChatViewModel Chat { get; }
}

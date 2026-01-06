using llmchat.Services;

namespace llmchat.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
	public MainWindowViewModel(IChatHistoryRepository repo)
	{
		Chat = new(repo.CreateSample());
	}

	public ChatViewModel Chat { get; }
}

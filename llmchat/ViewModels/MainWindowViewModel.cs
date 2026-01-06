using llmchat.Services;

namespace llmchat.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
	public string Title => "llmchat";

	public ChatHistoryViewModel Chat { get; }
	public MessageInputViewModel MessageInput { get; }

	public MainWindowViewModel(IChatHistoryRepository repo)
	{
		Chat = new ChatHistoryViewModel(repo.CreateSample());
		MessageInput = new MessageInputViewModel();
	}
}

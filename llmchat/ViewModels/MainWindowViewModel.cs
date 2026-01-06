namespace llmchat.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
	public string Title => "llmchat";

	public ChatHistoryViewModel Chat { get; }

	public MainWindowViewModel()
	{
		Chat = ChatHistoryViewModel.CreateSample();
	}
}

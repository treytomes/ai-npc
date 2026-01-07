using CommunityToolkit.Mvvm.ComponentModel;

namespace llmchat.ViewModels;

public sealed partial class LoadingWindowViewModel : ViewModelBase
{
	[ObservableProperty]
	private string _statusText = "Starting LLM…";
}

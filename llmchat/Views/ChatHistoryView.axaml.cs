using Avalonia.Controls;
using Avalonia.VisualTree;
using System;
using System.Collections.Specialized;

namespace llmchat.Views;

public partial class ChatHistoryView : UserControl
{
	private ScrollViewer? _scrollViewer;

	public ChatHistoryView()
	{
		InitializeComponent();

		MessagesList.AttachedToVisualTree += (_, _) =>
		{
			void OnLayoutUpdated(object? s, EventArgs e)
			{
				_scrollViewer = MessagesList.FindDescendantOfType<ScrollViewer>();

				if (_scrollViewer != null)
				{
					MessagesList.LayoutUpdated -= OnLayoutUpdated;
				}
			}

			MessagesList.LayoutUpdated += OnLayoutUpdated;
		};

		DataContextChanged += OnDataContextChanged;
	}

	private void OnDataContextChanged(object? sender, EventArgs e)
	{
		if (DataContext is not ViewModels.ChatHistoryViewModel vm)
			return;

		vm.Messages.CollectionChanged += OnMessagesChanged;
	}

	private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
	{
		if (e.Action == NotifyCollectionChangedAction.Add)
		{
			_scrollViewer?.ScrollToEnd();
		}
	}
}

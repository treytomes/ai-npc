using Terminal.Gui;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;
using Attribute = Terminal.Gui.Attribute;

// Terminal.Gui v2-safe ChatHistoryView
// - No reliance on nonexistent SetNeedsLayout / OnLayoutComplete
// - Uses LayoutComplete event
// - ScrollView owns content size
// - Virtualized rendering

public sealed class ChatHistoryView : FrameView
{
	private readonly ScrollView _scroll;
	private readonly View _canvas;

	private readonly List<MessageBlock> _blocks = new();
	private ChatHistory? _pendingHistory;

	private int _viewportWidth;
	private bool _layoutDirty;

	public ChatHistoryView() : base("Chat History")
	{
		Width = Dim.Fill();
		Height = Dim.Fill();

		ColorScheme = new ColorScheme
		{
			Normal = new Attribute(Color.White, Color.Blue),
			Focus = new Attribute(Color.White, Color.Blue)
		};

		_scroll = new ScrollView
		{
			Width = Dim.Fill(),
			Height = Dim.Fill(),
			ShowVerticalScrollIndicator = true,
			ShowHorizontalScrollIndicator = false,
			ColorScheme = ColorScheme
		};

		_canvas = new View
		{
			Width = Dim.Fill(),
			Height = Dim.Fill(),
			ColorScheme = ColorScheme
		};

		_scroll.Add(_canvas);
		Add(_scroll);

		// v2 layout hook — THIS is the correct replacement
		LayoutComplete += OnLayoutComplete;
	}

	// Public API
	public void Render(ChatHistory history)
	{
		_pendingHistory = history;
		_layoutDirty = true;
		SetNeedsDisplay();
	}

	private void OnLayoutComplete(View.LayoutEventArgs args)
	{
		if (_pendingHistory is null)
			return;

		if (_scroll.Bounds.Width <= 0 || _scroll.Bounds.Height <= 0)
			return;

		var usableWidth = _scroll.Bounds.Width - 2; // scrollbar margin

		// Only rebuild if width or history changed
		if (!_layoutDirty && usableWidth == _viewportWidth)
			return;

		_viewportWidth = usableWidth;
		BuildBlocks(_pendingHistory, usableWidth);
		UpdateContentSize();
		RenderVisible();
		ScrollToBottom();

		_pendingHistory = null;
	}

	private void BuildBlocks(ChatHistory history, int maxWidth)
	{
		_blocks.Clear();

		int y = 0;

		foreach (var msg in history)
		{
			var header = $"[{msg.Role.ToString().ToUpperInvariant()}]  {DateTime.Now:HH:mm:ss}";
			var headerHeight = 1;

			var contentLines = WordWrap(msg.Content ?? "[Empty message]", maxWidth - 2);
			var height = headerHeight + contentLines.Count + 1;

			_blocks.Add(new MessageBlock
			{
				Y = y,
				Height = height,
				Role = msg.Role,
				Header = header,
				Lines = contentLines
			});

			y += height;
		}
		_layoutDirty = false;
	}

	private void UpdateContentSize()
	{
		var totalHeight = _blocks.Count == 0 ? 0 : _blocks[^1].Y + _blocks[^1].Height;
		_scroll.ContentSize = new Size(_viewportWidth, totalHeight);
	}

	public override void OnDrawContent(Rect bounds)
	{
		base.OnDrawContent(bounds);
		RenderVisible();
	}

	private void RenderVisible()
	{
		_canvas.RemoveAll();

		var viewTop = _scroll.ContentOffset.Y;
		var viewBottom = viewTop + _scroll.Bounds.Height;

		foreach (var block in _blocks)
		{
			if (block.Y + block.Height < viewTop)
				continue;
			if (block.Y > viewBottom)
				break;

			DrawBlock(block);
		}
	}

	private void DrawBlock(MessageBlock block)
	{
		var scheme = GetScheme(block.Role);

		var header = new Label(block.Header)
		{
			X = 0,
			Y = block.Y,
			ColorScheme = scheme
		};
		_canvas.Add(header);

		var y = block.Y + 1;
		foreach (var line in block.Lines)
		{
			_canvas.Add(new Label(line)
			{
				X = 1,
				Y = y++,
				ColorScheme = ColorScheme
			});
		}
	}

	private void ScrollToBottom()
	{
		if (_scroll.ContentSize.Height > _scroll.Bounds.Height)
		{
			_scroll.ContentOffset = new Point(0, _scroll.ContentSize.Height - _scroll.Bounds.Height);
		}
	}

	private static ColorScheme GetScheme(AuthorRole role)
	{
		var fg = Color.White;
		if (role.Equals(AuthorRole.System)) fg = Color.BrightYellow;
		if (role.Equals(AuthorRole.User)) fg = Color.BrightCyan;
		if (role.Equals(AuthorRole.Assistant)) fg = Color.BrightGreen;
		if (role.Equals(AuthorRole.Tool)) fg = Color.Magenta;

		return new ColorScheme
		{
			Normal = new Attribute(fg, Color.Blue)
		};
	}

	private static List<string> WordWrap(string text, int width)
	{
		if (string.IsNullOrWhiteSpace(text) || width <= 0)
			return new() { text ?? string.Empty };

		var result = new List<string>();
		foreach (var paragraph in text.Split('\n'))
		{
			var words = paragraph.Split(' ');
			var line = new StringBuilder();

			foreach (var word in words)
			{
				if (line.Length > 0 && line.Length + word.Length + 1 > width)
				{
					result.Add(line.ToString());
					line.Clear();
				}

				if (line.Length > 0)
					line.Append(' ');
				line.Append(word);
			}

			if (line.Length > 0)
				result.Add(line.ToString());
		}

		return result;
	}

	private sealed class MessageBlock
	{
		public int Y;
		public int Height;
		public AuthorRole Role;
		public string Header = string.Empty;
		public List<string> Lines = new();
	}
}

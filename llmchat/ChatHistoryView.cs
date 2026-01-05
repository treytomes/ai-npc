using Terminal.Gui;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text;
using Attribute = Terminal.Gui.Attribute;

public sealed class ChatHistoryView : FrameView
{
	private readonly ScrollView _scrollView;
	private readonly View _contentView;
	private readonly ColorScheme _baseScheme;
	private readonly List<View> _messageViews = new();

	public ChatHistoryView() : base("Chat History")
	{
		X = 0;
		Y = 0;
		Width = Dim.Fill();
		Height = Dim.Fill();

		_baseScheme = new ColorScheme
		{
			Normal = new Attribute(Color.White, Color.Blue),
			Focus = new Attribute(Color.White, Color.Blue),
			HotNormal = new Attribute(Color.White, Color.Blue),
			HotFocus = new Attribute(Color.White, Color.Blue),
			Disabled = new Attribute(Color.Gray, Color.Blue)
		};

		ColorScheme = _baseScheme;

		// Create scrollable container
		_scrollView = new ScrollView
		{
			X = 0,
			Y = 0,
			Width = Dim.Fill(),
			Height = Dim.Fill(),
			ShowVerticalScrollIndicator = true,
			ShowHorizontalScrollIndicator = false,
			ColorScheme = _baseScheme
		};

		// Content view that will hold all messages
		_contentView = new View
		{
			X = 0,
			Y = 0,
			Width = Dim.Fill(),
			Height = Dim.Sized(0), // Will grow with content
			ColorScheme = _baseScheme
		};

		_scrollView.Add(_contentView);
		Add(_scrollView);
	}

	public void Render(ChatHistory history)
	{
		// Clear existing messages
		_contentView.RemoveAll();
		_messageViews.Clear();

		int yPos = 0;
		int maxWidth = Math.Max(20, Bounds.Width - 4); // Account for scrollbar

		foreach (var msg in history)
		{
			// Create role header
			var roleScheme = GetColorSchemeForRole(msg.Role);
			var roleText = $"[{msg.Role.ToString().ToUpperInvariant()}]";
			var timestamp = DateTime.Now.ToString("HH:mm:ss");

			var roleLabel = new Label
			{
				X = 0,
				Y = yPos,
				Text = roleText,
				ColorScheme = roleScheme
			};
			_contentView.Add(roleLabel);

			var timestampLabel = new Label
			{
				X = Pos.Right(roleLabel) + 1,
				Y = yPos,
				Text = timestamp,
				ColorScheme = new ColorScheme
				{
					Normal = new Attribute(Color.Gray, Color.Blue)
				}
			};
			_contentView.Add(timestampLabel);

			yPos++;

			// Add message content with word wrapping
			var content = msg.Content ?? "[Empty message]";
			var wrappedLines = WordWrap(content, maxWidth - 2);

			foreach (var line in wrappedLines)
			{
				var contentLabel = new Label
				{
					X = 1,
					Y = yPos,
					Text = line,
					Width = Dim.Fill(),
					ColorScheme = _baseScheme
				};
				_contentView.Add(contentLabel);
				yPos++;
			}

			// Add spacing between messages
			yPos += 1;
		}

		// Update content view height
		_contentView.Height = Dim.Sized(yPos);

		// THIS IS REQUIRED
		_scrollView.ContentSize = new Size(
			_scrollView.Bounds.Width,
			yPos
		);


		// Scroll to bottom
		ScrollToBottom();

		// Force redraw
		SetNeedsDisplay();
	}

	public void ScrollToBottom()
	{
		if (_contentView.Bounds.Height > _scrollView.Bounds.Height)
		{
			_scrollView.ContentOffset = new Point(
				0,
				Math.Max(0, _scrollView.ContentSize.Height - _scrollView.Bounds.Height)
			);
		}
	}

	private ColorScheme GetColorSchemeForRole(AuthorRole role)
	{
		var foreground = Color.White;
		var background = Color.Cyan;
		if (role.Equals(AuthorRole.System)) foreground = Color.BrightYellow;
		if (role.Equals(AuthorRole.User)) foreground = Color.BrightCyan;
		if (role.Equals(AuthorRole.Assistant)) foreground = Color.BrightGreen;
		if (role.Equals(AuthorRole.Tool)) foreground = Color.Magenta;

		return new ColorScheme
		{
			Normal = new Attribute(foreground, background),
			Focus = new Attribute(foreground, background),
			HotNormal = new Attribute(foreground, background),
			HotFocus = new Attribute(foreground, background)
		};
	}

	private List<string> WordWrap(string text, int maxWidth)
	{
		if (string.IsNullOrWhiteSpace(text) || maxWidth <= 0)
			return new List<string> { text ?? "" };

		var lines = new List<string>();
		var paragraphs = text.Split('\n');

		foreach (var paragraph in paragraphs)
		{
			if (string.IsNullOrWhiteSpace(paragraph))
			{
				lines.Add("");
				continue;
			}

			var words = paragraph.Split(' ');
			var currentLine = new StringBuilder();

			foreach (var word in words)
			{
				if (currentLine.Length > 0 && currentLine.Length + word.Length + 1 > maxWidth)
				{
					lines.Add(currentLine.ToString().Trim());
					currentLine.Clear();
				}

				if (currentLine.Length > 0)
					currentLine.Append(' ');
				currentLine.Append(word);
			}

			if (currentLine.Length > 0)
				lines.Add(currentLine.ToString().Trim());
		}

		return lines.Any() ? lines : new List<string> { "" };
	}

	public new void Clear()
	{
		_contentView.RemoveAll();
		_messageViews.Clear();
		_contentView.Height = Dim.Sized(0);
		SetNeedsDisplay();
		base.Clear();
	}
}
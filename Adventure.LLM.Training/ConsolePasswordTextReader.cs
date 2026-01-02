namespace Adventure.LLM.Training;

/// <summary>
/// Read a password securely (without echoing to console).
/// </summary>
internal class ConsolePasswordTextReader : ITextReader
{
	public string Read()
	{
		var password = new System.Text.StringBuilder();
		ConsoleKeyInfo key;

		do
		{
			key = Console.ReadKey(intercept: true);

			if (key.Key != ConsoleKey.Backspace && key.Key != ConsoleKey.Enter)
			{
				password.Append(key.KeyChar);
				Console.Write("*");
			}
			else if (key.Key == ConsoleKey.Backspace && password.Length > 0)
			{
				password.Remove(password.Length - 1, 1);
				Console.Write("\b \b");
			}
		}
		while (key.Key != ConsoleKey.Enter);

		return password.ToString();
	}
}
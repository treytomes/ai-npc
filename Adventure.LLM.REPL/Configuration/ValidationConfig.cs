namespace Adventure.LLM.REPL.Configuration;

public class ValidationConfig
{
	public int MaxAttempts { get; set; } = 2;
	public string MinSentences { get; set; } = "3";
	public string MaxSentences { get; set; } = "5";
}

namespace Adventure.LLM.Training;

internal sealed record ProgressChangedEventArgs(int Percentage, string Message);
internal sealed record OutputReceivedEventArgs(string OutputText);

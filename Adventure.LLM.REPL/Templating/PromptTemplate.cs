using Microsoft.SemanticKernel;

namespace Adventure.LLM.REPL.Templating;

public class PromptTemplate
{
	public string Template { get; set; } = string.Empty;
	public List<InputVariable> InputVariables { get; set; } = new();
	public ExecutionSettings ExecutionSettings { get; set; } = new();
}

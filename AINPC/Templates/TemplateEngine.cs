namespace AINPC.Templates;

class TemplateEngine
{
	public string Render(PromptTemplate template, Dictionary<string, string> values)
	{
		var result = template.TemplateText;

		foreach (var kv in values)
		{
			result = result.Replace("{" + kv.Key + "}", kv.Value);
		}

		return result;
	}
}

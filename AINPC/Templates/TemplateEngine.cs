namespace AINPC.Templates;

class TemplateEngine
{
	public string Render(PromptTemplate template, ICollection<KeyValuePair<string, string>>? values = null)
	{
		var result = template.TemplateText;

		if (values != null)
		{
			foreach (var kv in values)
			{
				result = result.Replace("{" + kv.Key + "}", kv.Value);
			}
		}

		return result;
	}
}

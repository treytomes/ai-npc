using Adventure.NLP.Services;

namespace Adventure.NLP.REPL.SystemIntent;

public sealed class SystemIntentEvaluator(IIntentSeedExtractor intentExtractor) : ISystemIntentEvaluator
{
	#region Constants

	private const string PATH_SYNONYMS = "assets/system_synonyms.json";

	#endregion

	#region Fields

	private readonly IIntentSeedExtractor _intentExtractor = intentExtractor ?? throw new ArgumentNullException(nameof(intentExtractor));
	private readonly List<IIntentPipelineStep> _pipelineSteps = [
		SynonymNormalizer.FromJsonFile(PATH_SYNONYMS),
	];
	private List<ISystemCommand> _commands = [];

	#endregion

	#region Methods

	public void AddCommand(ISystemCommand command) => _commands.Add(command);
	public void AddCommands(IEnumerable<ISystemCommand> commands) => _commands.AddRange(commands);

	public bool TryEvaluate(ParsedInput input)
	{
		if (input == null) throw new ArgumentNullException(nameof(input));

		// Use the same intent extraction pipeline.
		var seed = _intentExtractor.Extract(input);
		if (seed == null) return false;

		foreach (var step in _pipelineSteps)
		{
			seed = step.Process(seed);
		}

		foreach (var command in _commands)
		{
			if (command.CanExecute(input, seed))
			{
				var parameters = command.ParseParameters(input, seed);
				command.Execute(parameters);
				return true;
			}
		}

		return false;
	}

	#endregion
}

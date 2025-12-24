using Microsoft.Extensions.DependencyInjection;
using Language = Mosaik.Core.Language;

namespace LLM.NLP.Test;

/// <summary>
/// Verifies that the NLP runtime can be initialized through dependency injection,
/// including Catalyst model registration, storage configuration, and pipeline creation.
/// This test ensures the public DI entry point is sufficient for runtime use.
/// </summary>
public class NlpRuntimeInitializationTests
{
	[Fact]
	public void NlpRuntime_CanInitialize_ThroughDependencyInjection()
	{
		// ARRANGE
		var services = new ServiceCollection();

		services.AddNlpRuntime(options =>
		{
			options.DataPath = "catalyst-data";
			options.Language = Language.English;
		});

		Exception? exception = null;

		// ACT
		try
		{
			using var provider = services.BuildServiceProvider();

			var runtime = provider.GetRequiredService<INlpRuntime>();

			var doc = runtime.Process("Hello world!");
		}
		catch (Exception ex)
		{
			exception = ex;
		}

		// ASSERT
		Assert.Null(exception);
	}
}

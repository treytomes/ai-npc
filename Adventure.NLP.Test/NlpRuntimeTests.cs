using Adventure.NLP.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Adventure.NLP.Test;

/// <summary>
/// Verifies that the NLP runtime can be initialized through dependency injection,
/// including Catalyst model registration, storage configuration, and pipeline creation.
/// </summary>
public sealed class NlpRuntimeInitializationTests
{
	private readonly IServiceCollection _services;

	public NlpRuntimeInitializationTests()
	{
		_services = new ServiceCollection();
		_services.AddNlpRuntime();
	}

	[Fact]
	public void NlpRuntime_CanInitialize_ThroughDependencyInjection()
	{
		Exception? exception = null;

		try
		{
			using var provider = _services.BuildServiceProvider();

			var runtime = provider.GetRequiredService<INlpRuntime>();

			var document = runtime.Process("Hello world!");
		}
		catch (Exception ex)
		{
			exception = ex;
		}

		Assert.Null(exception);
	}
}

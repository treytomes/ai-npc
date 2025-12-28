using LLM.NLP.Services;
using Microsoft.Extensions.DependencyInjection;
using Mosaik.Core;
using Spectre.Console;

namespace LLM.NLP.Test;

/// <summary>
/// Verifies that the NLP runtime can be initialized through dependency injection,
/// including Catalyst model registration, storage configuration, and pipeline creation.
/// </summary>
public sealed class NlpRuntimeInitializationTests : IDisposable
{
	private readonly ServiceCollection _services;

	public NlpRuntimeInitializationTests()
	{
		_services = new ServiceCollection();
		_services.AddNlpRuntime();

		AnsiConsole.WriteLine();
		AnsiConsole.Write(
			new Rule("[bold green]NLP Runtime — DI Initialization[/]")
				.LeftJustified());
	}

	public void Dispose()
	{
		AnsiConsole.Write(
			new Rule("[dim]End Runtime Initialization Tests[/]")
				.LeftJustified());
		AnsiConsole.WriteLine();
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

			AnsiConsole.MarkupLine(
				"[green]✓[/] Runtime processed input successfully.");
		}
		catch (Exception ex)
		{
			exception = ex;

			AnsiConsole.MarkupLine(
				$"[red]✗ Runtime initialization failed:[/] {ex.Message}");
		}

		Assert.Null(exception);
	}
}

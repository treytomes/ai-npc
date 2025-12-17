using System.ComponentModel;
using System.Reflection;
using AINPC.ValueObjects;

namespace AINPC.Tools;

[DisplayName(NAME)]
[Description("Get the current weather conditions for a specific location.")]
internal sealed class GetWeatherTool : BaseOllamaTool
{
	#region Constants

	public const string NAME = "get_current_weather";
	public const string INTENT = "weather.query";

	#endregion

	#region Constructors

	public GetWeatherTool()
		: base(
			name: NAME,
			description: typeof(GetWeatherTool).GetCustomAttribute<DescriptionAttribute>()?.Description ?? string.Empty,
			intent: INTENT)
	{
		DefineParameter(
			name: "location",
			type: "string",
			description: "Name of the city or location",
			required: true);

		DefineParameter(
			name: "unit",
			type: "string",
			description: "Temperature unit",
			enumValues: ["celsius", "fahrenheit"],
			required: true);
	}

	#endregion

	#region Methods

	protected override async Task<object?> InvokeInternalAsync(
		IDictionary<string, object?> args)
	{
		await Task.CompletedTask;

		var location = (string?)args["location"] ?? "unknown location";
		var unitRaw = (string?)args["unit"] ?? "celsius";

		var unit = unitRaw.Equals("fahrenheit", StringComparison.OrdinalIgnoreCase)
			? TemperatureUnit.Fahrenheit
			: TemperatureUnit.Celsius;

		// Stubbed data for now — deterministic output matters more than realism.
		return unit switch
		{
			TemperatureUnit.Fahrenheit =>
				$"Current weather in {location}: 38°F, overcast.",

			TemperatureUnit.Celsius =>
				$"Current weather in {location}: 3°C, overcast.",

			_ =>
				$"Current weather in {location}: unavailable."
		};
	}

	#endregion
}

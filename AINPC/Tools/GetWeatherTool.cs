using AINPC.ValueObjects;

namespace AINPC.Tools;

public class GetWeatherTool : BaseOllamaTool
{
	public GetWeatherTool()
		: base("GetWeather", "Gets the current weather for a given location.")
	{
		DefineParameter(
			name: "location",
			type: "string",
			description: "The location or city to get the weather for",
			required: true);

		DefineParameter(
			name: "unit",
			type: "string",
			description: "The unit to measure the temperature in",
			enumValues: new[] { "Celsius", "Fahrenheit" },
			required: true);
	}

	protected override object? InvokeInternal(IDictionary<string, object?> args)
	{
		var location = (string?)args["location"] ?? "";
		var unitStr = (string?)args["unit"] ?? "Celsius";

		var unit = Enum.Parse<TemperatureUnit>(unitStr, ignoreCase: true);

		return unit switch
		{
			TemperatureUnit.Fahrenheit => $"It's cold at only 3.14159° {unit} in {location}.",
			TemperatureUnit.Celsius => $"It's warm at only 23 degrees {unit} in {location}, but is {location} a real place?",
			_ => "I don't really know.",
		};
	}
}

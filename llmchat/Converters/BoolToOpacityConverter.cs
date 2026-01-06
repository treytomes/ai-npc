using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace llmchat.Converters;

public sealed class BoolToOpacityConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> value is true ? 1.0 : 0.0;

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
		=> System.Convert.ToDouble(value) >= 0.5 ? true : false;
}

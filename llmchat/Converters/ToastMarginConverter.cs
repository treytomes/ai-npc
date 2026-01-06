using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace llmchat.Converters;

public sealed class ToastMarginConverter : IValueConverter
{
	public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
	{
		var visible = value is true;
		return visible
			? new Avalonia.Thickness(0, 0, 0, 16)
			: new Avalonia.Thickness(0, 0, 0, -20);
	}

	public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) => throw new NotSupportedException();
}

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace HowsMyMoney.Converters;

/// <summary>
/// Convierte un booleano a opacidad invertida (true = 0.3, false = 1.0)
/// </summary>
public class InverseBoolToOpacityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool boolValue)
        {
            return boolValue ? 0.3 : 1.0;
        }
        return 1.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace HowsMyMoney.Converters;

/// <summary>
/// Convierte un valor de progreso (0-100) a un ancho proporcional
/// </summary>
public class ProgressToWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            // Convertir porcentaje (0-100) a ancho de 300px máximo
            return (progress / 100.0) * 300.0;
        }
        return 0.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

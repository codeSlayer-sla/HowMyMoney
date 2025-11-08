using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace HowsMyMoney.ViewModels;

public class ProfitColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is decimal profitLoss)
        {
            if (profitLoss > 0)
                return new SolidColorBrush(Color.Parse("#27AE60")); // Verde
            else if (profitLoss < 0)
                return new SolidColorBrush(Color.Parse("#E74C3C")); // Rojo
            else
                return new SolidColorBrush(Color.Parse("#95A5A6")); // Gris
        }
        
        return new SolidColorBrush(Colors.Black);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

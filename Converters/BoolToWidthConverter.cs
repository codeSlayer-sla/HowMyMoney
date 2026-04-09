using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace HowsMyMoney.Converters;

public class BoolToWidthConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isCollapsed)
        {
            return isCollapsed ? 60.0 : 240.0; // Colapsado: 60px, Expandido: 240px
        }
        return 240.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToIconSizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isCollapsed)
        {
            return isCollapsed ? 45.0 : 70.0; // Colapsado: más pequeño
        }
        return 70.0;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class BoolToMenuIconConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool isCollapsed)
        {
            return isCollapsed ? "MenuOpen" : "Menu"; // Colapsado: MenuOpen (cerrar), Expandido: Menu (hamburguesa)
        }
        return "Menu";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

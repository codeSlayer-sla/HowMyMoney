using System;
using System.Globalization;
using Avalonia.Data.Converters;
using HowsMyMoney.Models;

namespace HowsMyMoney.Converters;

/// <summary>
/// Convierte valores del enum SkinCategory a nombres en español
/// </summary>
public class SkinCategoryConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is SkinCategory category)
        {
            return category switch
            {
                SkinCategory.Todos => "🔍 Todos",
                SkinCategory.Arma => "🔫 Armas",
                SkinCategory.Cuchillo => "🔪 Cuchillos",
                SkinCategory.Guantes => "🧤 Guantes",
                SkinCategory.Agente => "👤 Agentes",
                SkinCategory.Sticker => "🎨 Stickers",
                SkinCategory.Graffiti => "🖌️ Graffitis",
                SkinCategory.Musica => "🎵 Music Kits",
                SkinCategory.Parche => "🏅 Parches",
                SkinCategory.Caja => "📦 Cajas",
                SkinCategory.Llave => "🔑 Llaves",
                _ => category.ToString()
            };
        }
        return value?.ToString();
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;

namespace HowsMyMoney.Converters;

/// <summary>
/// Convierte una URL de imagen en un Bitmap para Avalonia
/// </summary>
public class UrlToBitmapConverter : IValueConverter
{
    private static readonly HttpClient _httpClient = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string url && !string.IsNullOrEmpty(url))
        {
            try
            {
                // Si es una URL HTTP/HTTPS, descargarla de forma síncrona
                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                    url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Descargar la imagen de forma síncrona usando GetAwaiter().GetResult()
                        var data = _httpClient.GetByteArrayAsync(url).GetAwaiter().GetResult();
                        using var stream = new MemoryStream(data);
                        var bitmap = new Bitmap(stream);
                        return bitmap;
                    }
                    catch (HttpRequestException httpEx)
                    {
                        Console.WriteLine($"⚠ Error HTTP al cargar imagen: {url} - {httpEx.Message}");
                        return null;
                    }
                }
                
                // Si es una ruta local
                try
                {
                    if (AssetLoader.Exists(new Uri(url, UriKind.RelativeOrAbsolute)))
                    {
                        return new Bitmap(AssetLoader.Open(new Uri(url, UriKind.RelativeOrAbsolute)));
                    }
                }
                catch
                {
                    // Ignorar errores de assets locales
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⚠ Error general cargando imagen desde: {url} - {ex.Message}");
            }
        }
        return null;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

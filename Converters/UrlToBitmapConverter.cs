using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using HowsMyMoney.Services;

namespace HowsMyMoney.Converters;

/// <summary>
/// Convierte una URL de imagen en un Bitmap para Avalonia usando caché
/// </summary>
public class UrlToBitmapConverter : IValueConverter
{
    private static readonly HttpClient _httpClient = new();
    private static readonly ImageCacheService _imageCache = new();
    
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string url && !string.IsNullOrEmpty(url))
        {
            try
            {
                // Si es una URL HTTP/HTTPS, obtenerla del caché o descargarla
                if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                    url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        // Usar el servicio de caché (sincrónico usando GetAwaiter().GetResult())
                        var data = _imageCache.GetImageAsync(url).GetAwaiter().GetResult();
                        
                        if (data != null && data.Length > 0)
                        {
                            using var stream = new MemoryStream(data);
                            var bitmap = new Bitmap(stream);
                            return bitmap;
                        }
                        else
                        {
                            Console.WriteLine($"⚠ No se pudo obtener imagen desde caché o descarga: {url.Substring(0, Math.Min(60, url.Length))}...");
                            return null;
                        }
                    }
                    catch (HttpRequestException httpEx)
                    {
                        Console.WriteLine($"⚠ Error HTTP al cargar imagen: {url.Substring(0, Math.Min(60, url.Length))}... - {httpEx.Message}");
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

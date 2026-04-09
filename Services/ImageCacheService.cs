using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using HowsMyMoney.Data;
using HowsMyMoney.Models;
using Microsoft.EntityFrameworkCore;
using SkiaSharp;

namespace HowsMyMoney.Services;

/// <summary>
/// Servicio para cachear imágenes en la base de datos
/// </summary>
public class ImageCacheService
{
    private readonly HttpClient _httpClient;
    private const int CACHE_EXPIRATION_DAYS = 30; // Expirar caché después de 30 días
    
    // Lock para evitar descargas duplicadas simultáneas
    private static readonly ConcurrentDictionary<string, SemaphoreSlim> _urlLocks = new();

    public ImageCacheService()
    {
        // Configurar HttpClientHandler para TLS moderno y otras restricciones
        var handler = new HttpClientHandler
        {
            ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true,
            SslProtocols = SslProtocols.Tls12 | SslProtocols.Tls13,
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 5
        };

        _httpClient = new HttpClient(handler);
        
        // Headers más realistas para evitar bloqueos
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Macintosh; Intel Mac OS X 10_15_7) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
        _httpClient.DefaultRequestHeaders.Add("Accept", "image/avif,image/webp,image/apng,image/svg+xml,image/*,*/*;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Accept-Language", "en-US,en;q=0.9,es;q=0.8");
        _httpClient.DefaultRequestHeaders.Add("Cache-Control", "no-cache");
        _httpClient.DefaultRequestHeaders.Add("Pragma", "no-cache");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Site", "cross-site");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Mode", "no-cors");
        _httpClient.DefaultRequestHeaders.Add("Sec-Fetch-Dest", "image");
        _httpClient.DefaultRequestHeaders.Add("Referer", "https://www.coingecko.com/");
        
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
        
        Console.WriteLine("🔧 ImageCacheService inicializado con bypass de restricciones");
    }

    /// <summary>
    /// Obtiene una imagen desde el caché o la descarga si no existe
    /// </summary>
    public async Task<byte[]?> GetImageAsync(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
        {
            Console.WriteLine("⚠ URL de imagen vacía");
            return null;
        }

        // Obtener o crear un lock para esta URL específica
        var urlLock = _urlLocks.GetOrAdd(imageUrl, _ => new SemaphoreSlim(1, 1));
        
        await urlLock.WaitAsync();
        try
        {
            using var db = new InvestmentDbContext();
            
            // Buscar en caché primero (puede haber sido agregado por otro thread)
            var cachedImage = await db.ImageCaches
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Url == imageUrl);

            // Si existe en caché y no ha expirado, retornarla
            if (cachedImage != null)
            {
                var age = DateTime.UtcNow - cachedImage.LastUpdated;
                if (age.TotalDays < CACHE_EXPIRATION_DAYS)
                {
                    Console.WriteLine($"✓ Imagen cargada desde caché: {imageUrl.Substring(0, Math.Min(60, imageUrl.Length))}...");
                    return cachedImage.ImageData;
                }
                else
                {
                    Console.WriteLine($"🔄 Caché expirado para: {imageUrl.Substring(0, Math.Min(60, imageUrl.Length))}...");
                    // Eliminar caché expirado
                    db.ImageCaches.Remove(cachedImage);
                    await db.SaveChangesAsync();
                }
            }

            if (IsGeneratedPlaceholderUrl(imageUrl))
            {
                Console.WriteLine($"🧩 Generando placeholder local: {imageUrl}");
                var placeholderData = GeneratePlaceholderImage(ExtractPlaceholderLabel(imageUrl));
                await SaveImageToCacheAsync(db, imageUrl, placeholderData, "image/png");
                return placeholderData;
            }

            // Descargar imagen
            Console.WriteLine($"⬇ Descargando imagen: {imageUrl.Substring(0, Math.Min(60, imageUrl.Length))}...");
            var downloadResult = await DownloadImageAsync(imageUrl);
            var imageData = downloadResult.Data;

            if (imageData == null && downloadResult.SslFailure)
            {
                Console.WriteLine($"🧩 Fallback local por error SSL: {imageUrl.Substring(0, Math.Min(60, imageUrl.Length))}...");
                imageData = GeneratePlaceholderImage(ExtractPlaceholderLabel(imageUrl));
                await SaveImageToCacheAsync(db, imageUrl, imageData, "image/png");
                return imageData;
            }

            if (imageData != null && imageData.Length > 0)
            {
                try
                {
                    // Verificar nuevamente por si acaso (race condition)
                    var existing = await db.ImageCaches.FirstOrDefaultAsync(i => i.Url == imageUrl);
                    if (existing != null)
                    {
                        Console.WriteLine($"✓ Imagen ya existía en caché (otra tarea la guardó)");
                        return existing.ImageData;
                    }
                    
                    // Guardar en caché
                    await SaveImageToCacheAsync(db, imageUrl, imageData, GetContentTypeFromUrl(imageUrl));
                    Console.WriteLine($"✓ Imagen guardada en caché ({imageData.Length} bytes)");

                    return imageData;
                }
                catch (DbUpdateException)
                {
                    // Si falla por duplicado, intentar leer de la BD
                    Console.WriteLine($"⚠ Conflicto al guardar, reintentando lectura...");
                    var existing = await db.ImageCaches
                        .AsNoTracking()
                        .FirstOrDefaultAsync(i => i.Url == imageUrl);
                    
                    return existing?.ImageData ?? imageData;
                }
            }

            Console.WriteLine($"⚠ No se pudo descargar la imagen");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al obtener imagen: {ex.Message}");
            return null;
        }
        finally
        {
            urlLock.Release();
        }
    }

    /// <summary>
    /// Descarga una imagen desde una URL
    /// </summary>
    private async Task<(byte[]? Data, bool SslFailure)> DownloadImageAsync(string imageUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();
            return (await response.Content.ReadAsByteArrayAsync(), false);
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"⚠ Error HTTP al descargar imagen: {ex.Message}");
            var sslFailure = ex.Message.Contains("SSL connection could not be established", StringComparison.OrdinalIgnoreCase)
                             || ex.Message.Contains("authentication", StringComparison.OrdinalIgnoreCase)
                             || ex.InnerException is AuthenticationException
                             || ex.InnerException is System.Security.Cryptography.CryptographicException;
            return (null, sslFailure);
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine($"⚠ Timeout al descargar imagen");
            return (null, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Error inesperado al descargar imagen: {ex.Message}");
            return (null, false);
        }
    }

    /// <summary>
    /// Verifica si una imagen ya existe en caché sin descargarla.
    /// </summary>
    public async Task<bool> IsCachedAsync(string imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl))
            return false;

        try
        {
            using var db = new InvestmentDbContext();
            var cachedImage = await db.ImageCaches
                .AsNoTracking()
                .FirstOrDefaultAsync(i => i.Url == imageUrl);

            if (cachedImage == null)
                return false;

            var age = DateTime.UtcNow - cachedImage.LastUpdated;
            return age.TotalDays < CACHE_EXPIRATION_DAYS;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Obtiene el tipo de contenido basado en la extensión de la URL
    /// </summary>
    private string GetContentTypeFromUrl(string url)
    {
        if (url.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            return "image/png";
        if (url.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) || 
            url.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            return "image/jpeg";
        if (url.EndsWith(".gif", StringComparison.OrdinalIgnoreCase))
            return "image/gif";
        if (url.EndsWith(".webp", StringComparison.OrdinalIgnoreCase))
            return "image/webp";

        return "image/jpeg"; // Default
    }

    private static bool IsGeneratedPlaceholderUrl(string url)
    {
        return url.StartsWith("placeholder://", StringComparison.OrdinalIgnoreCase)
               || url.Contains("via.placeholder.com", StringComparison.OrdinalIgnoreCase)
               || url.Contains("placeholder.com", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractPlaceholderLabel(string url)
    {
        if (url.StartsWith("placeholder://", StringComparison.OrdinalIgnoreCase))
        {
            var label = url[("placeholder://".Length)..];
            var lastSlash = label.LastIndexOf('/');
            if (lastSlash >= 0 && lastSlash < label.Length - 1)
            {
                label = label[(lastSlash + 1)..];
            }

            return string.IsNullOrWhiteSpace(label) ? "?" : label;
        }

        if (Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            var query = uri.Query;
            const string textKey = "text=";
            var textIndex = query.IndexOf(textKey, StringComparison.OrdinalIgnoreCase);
            if (textIndex >= 0)
            {
                var textValue = query[(textIndex + textKey.Length)..];
                var ampIndex = textValue.IndexOf('&');
                if (ampIndex >= 0)
                {
                    textValue = textValue[..ampIndex];
                }

                try
                {
                    return Uri.UnescapeDataString(textValue);
                }
                catch
                {
                    return textValue;
                }
            }

            var host = uri.Host;
            if (!string.IsNullOrWhiteSpace(host))
            {
                return host.Length <= 4 ? host : host[..4];
            }
        }

        return string.IsNullOrWhiteSpace(url) ? "?" : url[..Math.Min(3, url.Length)];
    }

    private static byte[] GeneratePlaceholderImage(string label)
    {
        const int size = 128;
        var text = GetPlaceholderText(label);
        var background = ColorFromSeed(label);
        var foreground = new SKColor(255, 255, 255);

        using var bitmap = new SKBitmap(size, size);
        using var canvas = new SKCanvas(bitmap);
        canvas.Clear(background);

        using var textPaint = new SKPaint
        {
            Color = foreground,
            IsAntialias = true,
            TextAlign = SKTextAlign.Center,
            TextSize = 42,
            FakeBoldText = true,
            Typeface = SKTypeface.Default
        };

        var bounds = new SKRect();
        textPaint.MeasureText(text, ref bounds);
        var x = size / 2f;
        var y = (size / 2f) - bounds.MidY;
        canvas.DrawText(text, x, y, textPaint);

        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100);
        return data.ToArray();
    }

    private static string GetPlaceholderText(string label)
    {
        var normalized = new string(label
            .Where(char.IsLetterOrDigit)
            .Take(6)
            .ToArray());

        if (string.IsNullOrWhiteSpace(normalized))
        {
            return "?";
        }

        return normalized.Length <= 3
            ? normalized.ToUpperInvariant()
            : normalized[..3].ToUpperInvariant();
    }

    private static SKColor ColorFromSeed(string seed)
    {
        var hash = seed.GetHashCode();
        var r = (byte)(80 + Math.Abs(hash % 120));
        var g = (byte)(80 + Math.Abs((hash / 3) % 120));
        var b = (byte)(80 + Math.Abs((hash / 7) % 120));
        return new SKColor(r, g, b);
    }

    private static async Task SaveImageToCacheAsync(InvestmentDbContext db, string url, byte[] imageData, string contentType)
    {
        var existing = await db.ImageCaches.FirstOrDefaultAsync(i => i.Url == url);
        if (existing != null)
        {
            existing.ImageData = imageData;
            existing.LastUpdated = DateTime.UtcNow;
            existing.ContentType = contentType;
        }
        else
        {
            db.ImageCaches.Add(new ImageCache
            {
                Url = url,
                ImageData = imageData,
                LastUpdated = DateTime.UtcNow,
                ContentType = contentType
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Limpia el caché antiguo (más de 30 días)
    /// </summary>
    public async Task CleanExpiredCacheAsync()
    {
        try
        {
            using var db = new InvestmentDbContext();
            var expirationDate = DateTime.UtcNow.AddDays(-CACHE_EXPIRATION_DAYS);

            var expiredImages = await db.ImageCaches
                .Where(i => i.LastUpdated < expirationDate)
                .ToListAsync();

            if (expiredImages.Any())
            {
                db.ImageCaches.RemoveRange(expiredImages);
                await db.SaveChangesAsync();
                Console.WriteLine($"🧹 Se eliminaron {expiredImages.Count} imágenes expiradas del caché");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error al limpiar caché: {ex.Message}");
        }
    }

    /// <summary>
    /// Obtiene el tamaño total del caché en bytes
    /// </summary>
    public async Task<long> GetCacheSizeAsync()
    {
        try
        {
            using var db = new InvestmentDbContext();
            return await db.ImageCaches.SumAsync(i => (long)i.ImageData.Length);
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Obtiene la cantidad de imágenes en caché
    /// </summary>
    public async Task<int> GetCacheCountAsync()
    {
        try
        {
            using var db = new InvestmentDbContext();
            return await db.ImageCaches.CountAsync();
        }
        catch
        {
            return 0;
        }
    }
}

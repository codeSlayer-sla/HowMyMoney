using System.Threading.Tasks;

namespace HowsMyMoney.Services;

/// <summary>
/// Servicio para cachear imágenes en la base de datos.
/// </summary>
public interface IImageCacheService
{
    Task<byte[]?> GetImageAsync(string imageUrl);
    Task<bool> IsCachedAsync(string imageUrl);
    Task CleanExpiredCacheAsync();
    Task<long> GetCacheSizeAsync();
    Task<int> GetCacheCountAsync();
}

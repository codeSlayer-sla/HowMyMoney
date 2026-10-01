using System.Threading.Tasks;

namespace HowsMyMoney.Services;

/// <summary>
/// Resuelve la URL del ícono de un item de Steam Community Market.
/// Usado SOLO para conseguir imágenes de skins de CS:GO (LIS-Skins no las provee) —
/// nunca para precios, así evitamos la fragilidad/rate-limiting que tenía el scraping
/// de Steam Market cuando lo usábamos para precios (ver LisSkinsService).
/// </summary>
public interface ISteamIconService
{
    /// <summary>
    /// Devuelve la URL del ícono (CDN de Steam) para el market hash name dado,
    /// o null si no se encontró o falló la consulta.
    /// </summary>
    Task<string?> GetIconUrlAsync(string marketHashName);
}

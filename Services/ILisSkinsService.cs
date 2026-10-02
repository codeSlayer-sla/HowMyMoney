using System.Collections.Generic;
using System.Threading.Tasks;
using HowsMyMoney.Models;

namespace HowsMyMoney.Services;

/// <summary>
/// Servicio para obtener precios de skins de CS:GO/CS2 desde LIS-Skins.
/// </summary>
public interface ILisSkinsService
{
    Task<List<SkinItem>> SearchSkinsAsync(string query, SkinCategory category = SkinCategory.Todos);
    Task<decimal?> GetSkinPriceAsync(string marketHashName);

    /// <summary>
    /// Devuelve las <paramref name="count"/> skins más caras del price list (para un
    /// panel tipo "top skins" en el monitor de mercados, sin tener que buscar por nombre).
    /// </summary>
    Task<List<SkinItem>> GetTopSkinsAsync(int count = 10);
}

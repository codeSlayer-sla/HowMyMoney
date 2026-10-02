using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HowsMyMoney.Models;

namespace HowsMyMoney.Services;

/// <summary>
/// Servicio para obtener precios de skins de CS:GO/CS2 desde LIS-Skins.
/// Usa el price list público (sin API key) que LIS-Skins publica para su market:
/// https://lis-skins.stoplight.io/docs/lis-skins/l6th4ko9av64c-json-price-lists
/// El listado completo se descarga una vez y se cachea en memoria para evitar
/// golpear la red en cada búsqueda o al refrescar precios de todo el portfolio.
/// </summary>
public class LisSkinsService : ILisSkinsService
{
    private const string PriceListUrl = "https://lis-skins.com/market_export_json/csgo.json";
    private const int CSGO_APP_ID = 730;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(10);

    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _cacheLock = new(1, 1);
    private List<LisSkinPriceEntry>? _cachedItems;
    private DateTime _cachedAt;

    /// <summary>
    /// Permite inyectar un HttpClient (ej. con un handler falso en tests).
    /// Si no se provee, crea uno propio con timeout de 20s.
    /// </summary>
    public LisSkinsService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(20) };
    }

    /// <summary>
    /// Busca skins por nombre (coincidencia parcial) en el price list de LIS-Skins.
    /// </summary>
    public async Task<List<SkinItem>> SearchSkinsAsync(string query, SkinCategory category = SkinCategory.Todos)
    {
        if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
        {
            Console.WriteLine($"⚠ Query muy corta: '{query}'");
            return new List<SkinItem>();
        }

        try
        {
            var priceList = await GetPriceListAsync();

            var matches = priceList
                .Where(i => i.Name.Contains(query, StringComparison.OrdinalIgnoreCase))
                .Select(ToSkinItem)
                .ToList();

            Console.WriteLine($"🔍 LIS-Skins: '{query}' encontró {matches.Count} skins de CS:GO (App ID: {CSGO_APP_ID})");

            if (category != SkinCategory.Todos && matches.Any())
            {
                var beforeFilter = matches.Count;
                matches = FilterByCategory(matches, category);
                Console.WriteLine($"🔍 Filtrado por categoría '{category}': {beforeFilter} → {matches.Count} items");
            }

            if (!matches.Any())
            {
                Console.WriteLine($"⚠ LIS-Skins: No se encontraron resultados para '{query}'");
            }

            return matches
                .OrderBy(s => s.MinPrice)
                .Take(50)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ ERROR buscando en LIS-Skins: {ex.Message}");
            return new List<SkinItem>();
        }
    }

    /// <summary>
    /// Obtiene el precio actual de una skin específica por su market hash name.
    /// </summary>
    public async Task<decimal?> GetSkinPriceAsync(string marketHashName)
    {
        try
        {
            var priceList = await GetPriceListAsync();

            var match = priceList.FirstOrDefault(i =>
                i.Name.Equals(marketHashName, StringComparison.OrdinalIgnoreCase));

            if (match != null)
            {
                Console.WriteLine($"✓ LIS-Skins: {marketHashName} = ${match.Price:F2}");
                return match.Price;
            }

            Console.WriteLine($"⚠ LIS-Skins: No se encontró precio para '{marketHashName}'");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ ERROR obteniendo precio de LIS-Skins: {ex.Message}");
            return null;
        }
    }

    public async Task<List<SkinItem>> GetTopSkinsAsync(int count = 10)
    {
        try
        {
            var priceList = await GetPriceListAsync();

            return priceList
                .OrderByDescending(i => i.Price)
                .Take(count)
                .Select(ToSkinItem)
                .ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ ERROR obteniendo top skins de LIS-Skins: {ex.Message}");
            return new List<SkinItem>();
        }
    }

    /// <summary>
    /// Descarga (o reutiliza de caché) el price list completo de CS:GO/CS2.
    /// </summary>
    private async Task<List<LisSkinPriceEntry>> GetPriceListAsync()
    {
        await _cacheLock.WaitAsync();
        try
        {
            if (_cachedItems != null && DateTime.UtcNow - _cachedAt < CacheDuration)
            {
                return _cachedItems;
            }

            Console.WriteLine("📡 Descargando price list de LIS-Skins (CS:GO)...");
            var json = await _httpClient.GetStringAsync(PriceListUrl);
            var items = JsonSerializer.Deserialize<List<LisSkinPriceEntry>>(json) ?? new List<LisSkinPriceEntry>();

            _cachedItems = items;
            _cachedAt = DateTime.UtcNow;

            Console.WriteLine($"✓ Price list de LIS-Skins descargado: {items.Count} skins");
            return items;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ ERROR descargando price list de LIS-Skins: {ex.Message}");
            // Si falla la descarga pero hay una caché vieja, mejor usarla que quedar sin datos
            return _cachedItems ?? new List<LisSkinPriceEntry>();
        }
        finally
        {
            _cacheLock.Release();
        }
    }

    private static SkinItem ToSkinItem(LisSkinPriceEntry entry) => new()
    {
        MarketHashName = entry.Name,
        MinPrice = entry.Price,
        MaxPrice = entry.Price,
        SuggestedPrice = entry.Price,
        AppId = CSGO_APP_ID,
        GameName = "CS:GO",
        ImageUrl = null,
        ListedCount = entry.Count
    };

    /// <summary>
    /// Filtra los resultados por categoría basándose en el nombre del item.
    /// </summary>
    private List<SkinItem> FilterByCategory(List<SkinItem> items, SkinCategory category)
    {
        return category switch
        {
            SkinCategory.Arma => items.Where(i =>
                !i.MarketHashName.Contains("★") && // No items especiales (cuchillos/guantes tienen ★)
                !i.MarketHashName.Contains("Sticker") &&
                !i.MarketHashName.Contains("Sealed Graffiti") &&
                !i.MarketHashName.Contains("Music Kit") &&
                !i.MarketHashName.Contains("Patch") &&
                !i.MarketHashName.Contains("Case") &&
                !i.MarketHashName.Contains("Key") &&
                !i.MarketHashName.Contains("Pin") &&
                !i.MarketHashName.Contains("Capsule") &&
                !i.MarketHashName.Contains("Package") &&
                !i.MarketHashName.Contains("Autograph") &&
                !i.MarketHashName.Contains("Souvenir") &&
                (i.MarketHashName.Contains("AK-47") ||
                 i.MarketHashName.Contains("M4A4") ||
                 i.MarketHashName.Contains("M4A1-S") ||
                 i.MarketHashName.Contains("AWP") ||
                 i.MarketHashName.Contains("Desert Eagle") ||
                 i.MarketHashName.Contains("USP-S") ||
                 i.MarketHashName.Contains("Glock-18") ||
                 i.MarketHashName.Contains("P250") ||
                 i.MarketHashName.Contains("Five-SeveN") ||
                 i.MarketHashName.Contains("Tec-9") ||
                 i.MarketHashName.Contains("CZ75-Auto") ||
                 i.MarketHashName.Contains("P2000") ||
                 i.MarketHashName.Contains("Dual Berettas") ||
                 i.MarketHashName.Contains("R8 Revolver") ||
                 i.MarketHashName.Contains("MP9") ||
                 i.MarketHashName.Contains("MAC-10") ||
                 i.MarketHashName.Contains("MP7") ||
                 i.MarketHashName.Contains("MP5-SD") ||
                 i.MarketHashName.Contains("UMP-45") ||
                 i.MarketHashName.Contains("P90") ||
                 i.MarketHashName.Contains("PP-Bizon") ||
                 i.MarketHashName.Contains("Galil AR") ||
                 i.MarketHashName.Contains("FAMAS") ||
                 i.MarketHashName.Contains("AUG") ||
                 i.MarketHashName.Contains("SG 553") ||
                 i.MarketHashName.Contains("SSG 08") ||
                 i.MarketHashName.Contains("SCAR-20") ||
                 i.MarketHashName.Contains("G3SG1") ||
                 i.MarketHashName.Contains("Nova") ||
                 i.MarketHashName.Contains("XM1014") ||
                 i.MarketHashName.Contains("MAG-7") ||
                 i.MarketHashName.Contains("Sawed-Off") ||
                 i.MarketHashName.Contains("M249") ||
                 i.MarketHashName.Contains("Negev"))
            ).ToList(),

            SkinCategory.Cuchillo => items.Where(i =>
                i.MarketHashName.Contains("★") &&
                (i.MarketHashName.Contains("Knife") ||
                 i.MarketHashName.Contains("Karambit") ||
                 i.MarketHashName.Contains("Bayonet") ||
                 i.MarketHashName.Contains("Butterfly") ||
                 i.MarketHashName.Contains("Daggers") ||
                 i.MarketHashName.Contains("Bowie"))
            ).ToList(),

            SkinCategory.Guantes => items.Where(i =>
                i.MarketHashName.Contains("★") && i.MarketHashName.Contains("Gloves")
            ).ToList(),

            SkinCategory.Agente => items.Where(i =>
                i.MarketHashName.Contains("Agent") || i.MarketHashName.Contains("The ")
            ).ToList(),

            SkinCategory.Sticker => items.Where(i =>
                i.MarketHashName.Contains("Sticker") && !i.MarketHashName.Contains("Capsule")
            ).ToList(),

            SkinCategory.Graffiti => items.Where(i =>
                i.MarketHashName.Contains("Sealed Graffiti")
            ).ToList(),

            SkinCategory.Musica => items.Where(i =>
                i.MarketHashName.Contains("Music Kit")
            ).ToList(),

            SkinCategory.Parche => items.Where(i =>
                i.MarketHashName.Contains("Patch")
            ).ToList(),

            SkinCategory.Caja => items.Where(i =>
                i.MarketHashName.Contains("Case") || i.MarketHashName.Contains("Package")
            ).ToList(),

            SkinCategory.Llave => items.Where(i =>
                i.MarketHashName.Contains("Key") && !i.MarketHashName.Contains("Capsule Key")
            ).ToList(),

            SkinCategory.Todos => items,

            _ => items
        };
    }
}

/// <summary>
/// Entrada del price list público de LIS-Skins para CS:GO/CS2.
/// https://lis-skins.com/market_export_json/csgo.json
/// </summary>
public class LisSkinPriceEntry
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("price")]
    public decimal Price { get; set; }

    [JsonPropertyName("unlocked_price")]
    public decimal? UnlockedPrice { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

/// <summary>
/// Representa un item de CS:GO con precio.
/// </summary>
public class SkinItem
{
    [JsonPropertyName("market_hash_name")]
    public string MarketHashName { get; set; } = string.Empty;

    [JsonPropertyName("min_price")]
    public decimal MinPrice { get; set; }

    [JsonPropertyName("max_price")]
    public decimal MaxPrice { get; set; }

    [JsonPropertyName("suggested_price")]
    public decimal SuggestedPrice { get; set; }

    [JsonPropertyName("app_id")]
    public int AppId { get; set; }

    [JsonPropertyName("game_name")]
    public string GameName { get; set; } = string.Empty;

    /// <summary>
    /// URL de la imagen del item (no disponible en el price list de LIS-Skins).
    /// </summary>
    public string? ImageUrl { get; set; }

    /// <summary>Cantidad de unidades listadas en LIS-Skins (campo "count" del price list).</summary>
    public int ListedCount { get; set; }
}

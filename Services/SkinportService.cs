using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HowsMyMoney.Models;

namespace HowsMyMoney.Services;

/// <summary>
/// Servicio para obtener precios REALES de skins de CS:GO desde Steam Community Market
/// App ID de Steam: 730 (CS:GO)
/// NO usa base de datos local - SOLO APIs reales
/// </summary>
public class SkinportService
{
    private readonly HttpClient _httpClient;
    private const int CSGO_APP_ID = 730;
    
    public SkinportService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
        _httpClient.Timeout = TimeSpan.FromSeconds(15);
    }
    
    /// <summary>
    /// Busca skins usando Steam Community Market API con filtro de categoría
    /// Retorna precios REALES actualizados
    /// </summary>
    public async Task<List<SkinItem>> SearchSkinsAsync(string query, SkinCategory category = SkinCategory.Todos)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                Console.WriteLine($"⚠ Query muy corta: '{query}'");
                return new List<SkinItem>();
            }
            
            Console.WriteLine($"\n========== BUSCANDO EN STEAM MARKET ==========");
            Console.WriteLine($"🔍 Búsqueda: '{query}' | Categoría: {category}");
            
            var results = new List<SkinItem>();
            
            // Lista de búsquedas comunes para probar
            var searchTerms = GenerateSearchTerms(query);
            
            foreach (var searchTerm in searchTerms.Take(3)) // Limitar a 3 búsquedas
            {
                try
                {
                    Console.WriteLine($"🔄 Probando término: '{searchTerm}'");
                    
                    // PASO 1: Hacer consulta inicial para saber cuántos items hay disponibles
                    var initialUrl = $"https://steamcommunity.com/market/search/render/?appid={CSGO_APP_ID}&search_descriptions=0&query={Uri.EscapeDataString(searchTerm)}&start=0&count=10&norender=1";
                    
                    Console.WriteLine($"📡 Consultando Steam Market...");
                    var initialResponse = await _httpClient.GetStringAsync(initialUrl);
                    var initialResult = JsonSerializer.Deserialize<SteamMarketSearchResult>(initialResponse);
                    
                    if (initialResult?.Results == null || !initialResult.Results.Any())
                    {
                        Console.WriteLine($"  ⚠ No se encontraron items para '{searchTerm}'");
                        continue;
                    }
                    
                    // PASO 2: Calcular cuántas páginas necesitamos traer
                    var totalCount = initialResult.TotalCount;
                    var itemsPerPage = 10; // Límite de Steam
                    var maxItemsToGet = 50; // Límite que queremos traer
                    var itemsToFetch = Math.Min(totalCount, maxItemsToGet);
                    var totalPages = (int)Math.Ceiling((double)itemsToFetch / itemsPerPage);
                    
                    Console.WriteLine($"✓ Total disponible: {totalCount} items");
                    Console.WriteLine($"📥 Descargando {itemsToFetch} items en {totalPages} páginas...");
                    
                    // PASO 3: Agregar los resultados de la primera página
                    var allItems = new List<SteamMarketItem>();
                    allItems.AddRange(initialResult.Results);
                    Console.WriteLine($"  ✓ Página 1/{totalPages} - {initialResult.Results.Count} items");
                    
                    // PASO 4: Traer las páginas restantes
                    for (int page = 1; page < totalPages; page++)
                    {
                        var start = page * itemsPerPage;
                        var url = $"https://steamcommunity.com/market/search/render/?appid={CSGO_APP_ID}&search_descriptions=0&query={Uri.EscapeDataString(searchTerm)}&start={start}&count={itemsPerPage}&norender=1";
                        
                        await Task.Delay(300); // Pausa entre requests para evitar rate limiting
                        
                        Console.WriteLine($"  📡 Página {page + 1}/{totalPages}...");
                        
                        var response = await _httpClient.GetStringAsync(url);
                        var searchResult = JsonSerializer.Deserialize<SteamMarketSearchResult>(response);
                        
                        if (searchResult?.Results != null && searchResult.Results.Any())
                        {
                            allItems.AddRange(searchResult.Results);
                            Console.WriteLine($"  ✓ Página {page + 1}/{totalPages} - {searchResult.Results.Count} items");
                        }
                        else
                        {
                            Console.WriteLine($"  ⚠ Página {page + 1} sin resultados");
                            break;
                        }
                    }
                    
                    if (allItems.Any())
                    {
                        Console.WriteLine($"✓ Total descargado: {allItems.Count} items de Steam Market");
                        Console.WriteLine($"⏳ Obteniendo precios (esto puede tomar unos segundos)...");
                        
                        // OBTENER PRECIOS CON RATE LIMITING (evitar bloqueos de Steam)
                        var itemsToProcess = allItems.Take(50).ToList();
                        var processedCount = 0;
                        
                        foreach (var item in itemsToProcess)
                        {
                            processedCount++;
                            
                            // Obtener URL de imagen primero
                            string? imageUrl = null;
                            if (item.AssetDescription?.IconUrlLarge != null)
                            {
                                imageUrl = $"https://community.cloudflare.steamstatic.com/economy/image/{item.AssetDescription.IconUrlLarge}";
                            }
                            else if (item.AssetDescription?.IconUrl != null)
                            {
                                imageUrl = $"https://community.cloudflare.steamstatic.com/economy/image/{item.AssetDescription.IconUrl}";
                            }
                            
                            // Intentar obtener precio
                            var price = await GetRealPriceFromSteam(item.HashName);
                            
                            if (price.HasValue && price.Value > 0)
                            {
                                Console.WriteLine($"  [{processedCount}/{itemsToProcess.Count}] ✓ {item.HashName} - ${price.Value:F2}");
                                
                                results.Add(new SkinItem
                                {
                                    MarketHashName = item.HashName,
                                    MinPrice = price.Value,
                                    MaxPrice = price.Value,
                                    SuggestedPrice = price.Value,
                                    AppId = CSGO_APP_ID,
                                    GameName = "CS:GO",
                                    ImageUrl = imageUrl
                                });
                            }
                            else
                            {
                                // AGREGAR ITEM AUNQUE NO TENGA PRECIO (el usuario puede seleccionarlo y el precio se obtendrá al guardar)
                                Console.WriteLine($"  [{processedCount}/{itemsToProcess.Count}] ⚠ {item.HashName} - Sin precio (se obtendrá al guardar)");
                                
                                results.Add(new SkinItem
                                {
                                    MarketHashName = item.HashName,
                                    MinPrice = 0, // Precio temporal, se actualizará al guardar
                                    MaxPrice = 0,
                                    SuggestedPrice = 0,
                                    AppId = CSGO_APP_ID,
                                    GameName = "CS:GO",
                                    ImageUrl = imageUrl
                                });
                            }
                            
                            // Delay entre peticiones para evitar rate limiting (solo cada 5 items)
                            if (processedCount % 5 == 0 && processedCount < itemsToProcess.Count)
                            {
                                await Task.Delay(1000); // 1 segundo cada 5 items
                            }
                            else
                            {
                                await Task.Delay(200); // 200ms entre items
                            }
                        }
                        
                        if (results.Any())
                        {
                            break; // Si encontramos resultados, no seguir buscando
                        }
                    }
                    else
                    {
                        Console.WriteLine($"  ⚠ No se encontraron items para '{searchTerm}'");
                    }
                    
                    await Task.Delay(500); // Delay entre búsquedas
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  ✗ Error buscando '{searchTerm}': {ex.Message}");
                }
            }
            
            // FILTRAR POR CATEGORÍA después de obtener todos los resultados
            if (category != SkinCategory.Todos && results.Any())
            {
                var beforeFilter = results.Count;
                results = FilterByCategory(results, category);
                Console.WriteLine($"🔍 Filtrado por categoría '{category}': {beforeFilter} → {results.Count} items");
            }
            
            if (!results.Any())
            {
                Console.WriteLine($"\n⚠ NO SE ENCONTRARON RESULTADOS REALES en Steam Market para '{query}'");
                Console.WriteLine($"💡 Sugerencias:");
                Console.WriteLine($"   - Usa nombres completos: 'AK-47 Redline'");
                Console.WriteLine($"   - Intenta sin calidad: 'AWP Asiimov' en vez de 'AWP Asiimov Field-Tested'");
                Console.WriteLine($"   - Verifica el nombre exacto en Steam Market");
            }
            else
            {
                Console.WriteLine($"\n✓ Total: {results.Count} items encontrados con precios reales de Steam");
            }
            
            return results.OrderBy(r => r.MinPrice).ToList();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"\n✗ ERROR en búsqueda: {ex.Message}");
            Console.WriteLine($"✗ StackTrace: {ex.StackTrace}");
            return new List<SkinItem>();
        }
    }
    
    /// <summary>
    /// Filtra los resultados por categoría basándose en el nombre del item
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
    
    /// <summary>
    /// Genera términos de búsqueda alternativos
    /// </summary>
    private List<string> GenerateSearchTerms(string query)
    {
        var terms = new List<string> { query };
        
        // Agregar variaciones sin calidades
        var withoutQuality = query
            .Replace("Factory New", "")
            .Replace("Minimal Wear", "")
            .Replace("Field-Tested", "")
            .Replace("Well-Worn", "")
            .Replace("Battle-Scarred", "")
            .Replace("(", "")
            .Replace(")", "")
            .Trim();
        
        if (withoutQuality != query && !string.IsNullOrWhiteSpace(withoutQuality))
        {
            terms.Add(withoutQuality);
        }
        
        // Buscar solo el arma/cuchillo
        var parts = query.Split('|', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length > 0)
        {
            var weapon = parts[0].Trim();
            if (!terms.Contains(weapon))
            {
                terms.Add(weapon);
            }
        }
        
        return terms;
    }
    
    /// <summary>
    /// Obtiene el precio REAL actual de Steam Community Market
    /// </summary>
    private async Task<decimal?> GetRealPriceFromSteam(string marketHashName)
    {
        try
        {
            var url = $"https://steamcommunity.com/market/priceoverview/?appid={CSGO_APP_ID}&currency=1&market_hash_name={Uri.EscapeDataString(marketHashName)}";
            
            var response = await _httpClient.GetStringAsync(url);
            
            // Debug: Ver respuesta de Steam
            if (string.IsNullOrEmpty(response) || response.Contains("null"))
            {
                // Item muy raro o sin ventas recientes
                return null;
            }
            
            var priceData = JsonSerializer.Deserialize<SteamPriceOverview>(response);
            
            if (priceData?.Success == true && !string.IsNullOrEmpty(priceData.LowestPrice))
            {
                // Parsear precio (formato: "$12.34" o "$1,234.56")
                var priceStr = priceData.LowestPrice
                    .Replace("$", "")
                    .Replace(",", "")
                    .Trim();
                
                if (decimal.TryParse(priceStr, System.Globalization.NumberStyles.Any, 
                    System.Globalization.CultureInfo.InvariantCulture, out decimal price))
                {
                    return price;
                }
            }
            else if (priceData?.Success == false)
            {
                // Steam devolvió success: false (item sin precio o rate limit)
                return null;
            }
            
            return null;
        }
        catch (HttpRequestException ex)
        {
            // Error de red o rate limiting
            if (ex.Message.Contains("429"))
            {
                Console.WriteLine($"    ⏸ Rate limit alcanzado, esperando...");
                await Task.Delay(2000); // Esperar 2 segundos
            }
            return null;
        }
        catch (Exception ex)
        {
            // Otro error
            Console.WriteLine($"    ✗ Error obteniendo precio: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Obtiene el precio de una skin específica desde Steam Market
    /// </summary>
    public async Task<decimal?> GetSkinPriceAsync(string marketHashName)
    {
        try
        {
            Console.WriteLine($"🔍 Obteniendo precio real de Steam para: '{marketHashName}'");
            
            var price = await GetRealPriceFromSteam(marketHashName);
            
            if (price.HasValue)
            {
                Console.WriteLine($"✓ Precio actual en Steam: ${price.Value:F2}");
                return price.Value;
            }
            
            Console.WriteLine($"⚠ No se pudo obtener precio de Steam para '{marketHashName}'");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error obteniendo precio: {ex.Message}");
            return null;
        }
    }
}

/// <summary>
/// Respuesta de Steam Market Search
/// </summary>
public class SteamMarketSearchResult
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("start")]
    public int Start { get; set; }
    
    [JsonPropertyName("pagesize")]
    public int PageSize { get; set; }
    
    [JsonPropertyName("total_count")]
    public int TotalCount { get; set; }
    
    [JsonPropertyName("results")]
    public List<SteamMarketItem>? Results { get; set; }
}

public class SteamMarketItem
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("hash_name")]
    public string HashName { get; set; } = string.Empty;
    
    [JsonPropertyName("sell_listings")]
    public int SellListings { get; set; }
    
    [JsonPropertyName("sell_price")]
    public int SellPrice { get; set; }
    
    [JsonPropertyName("sell_price_text")]
    public string SellPriceText { get; set; } = string.Empty;
    
    [JsonPropertyName("app_icon")]
    public string AppIcon { get; set; } = string.Empty;
    
    [JsonPropertyName("app_name")]
    public string AppName { get; set; } = string.Empty;
    
    [JsonPropertyName("asset_description")]
    public SteamAssetDescription? AssetDescription { get; set; }
}

public class SteamAssetDescription
{
    [JsonPropertyName("appid")]
    public int AppId { get; set; }
    
    [JsonPropertyName("classid")]
    public string ClassId { get; set; } = string.Empty;
    
    [JsonPropertyName("instanceid")]
    public string InstanceId { get; set; } = string.Empty;
    
    [JsonPropertyName("icon_url")]
    public string? IconUrl { get; set; }
    
    [JsonPropertyName("icon_url_large")]
    public string? IconUrlLarge { get; set; }
}

/// <summary>
/// Respuesta de Steam Price Overview
/// </summary>
public class SteamPriceOverview
{
    [JsonPropertyName("success")]
    public bool Success { get; set; }
    
    [JsonPropertyName("lowest_price")]
    public string? LowestPrice { get; set; }
    
    [JsonPropertyName("volume")]
    public string? Volume { get; set; }
    
    [JsonPropertyName("median_price")]
    public string? MedianPrice { get; set; }
}

/// <summary>
/// Representa un item de CS:GO con precio real
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
    /// URL de la imagen del item
    /// </summary>
    public string? ImageUrl { get; set; }
}

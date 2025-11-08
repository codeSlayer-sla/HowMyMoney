using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

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
    /// Busca skins usando Steam Community Market API
    /// Retorna precios REALES actualizados
    /// </summary>
    public async Task<List<SkinItem>> SearchSkinsAsync(string query)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(query) || query.Length < 2)
            {
                Console.WriteLine($"⚠ Query muy corta: '{query}'");
                return new List<SkinItem>();
            }
            
            Console.WriteLine($"\n========== BUSCANDO EN STEAM MARKET ==========");
            Console.WriteLine($"🔍 Búsqueda: '{query}'");
            
            var results = new List<SkinItem>();
            
            // Lista de búsquedas comunes para probar
            var searchTerms = GenerateSearchTerms(query);
            
            foreach (var searchTerm in searchTerms.Take(3)) // Limitar a 3 búsquedas
            {
                try
                {
                    Console.WriteLine($"🔄 Probando término: '{searchTerm}'");
                    
                    var url = $"https://steamcommunity.com/market/search/render/?appid={CSGO_APP_ID}&search_descriptions=0&query={Uri.EscapeDataString(searchTerm)}&count=10&norender=1";
                    Console.WriteLine($"📡 URL: {url}");
                    
                    var response = await _httpClient.GetStringAsync(url);
                    
                    // Debug: mostrar los primeros 200 caracteres de la respuesta
                    Console.WriteLine($"📄 Respuesta (primeros 200 chars): {response.Substring(0, Math.Min(200, response.Length))}");
                    
                    var searchResult = JsonSerializer.Deserialize<SteamMarketSearchResult>(response);
                    
                    if (searchResult?.Results != null && searchResult.Results.Any())
                    {
                        Console.WriteLine($"✓ Encontrados {searchResult.Results.Count} items en Steam Market");
                        
                        foreach (var item in searchResult.Results.Take(10))
                        {
                            // Obtener precio real del item
                            var price = await GetRealPriceFromSteam(item.HashName);
                            
                            if (price.HasValue && price.Value > 0)
                            {
                                Console.WriteLine($"  ✓ {item.HashName} - ${price.Value:F2}");
                                
                                // Obtener URL de imagen
                                string? imageUrl = null;
                                if (item.AssetDescription?.IconUrlLarge != null)
                                {
                                    imageUrl = $"https://community.cloudflare.steamstatic.com/economy/image/{item.AssetDescription.IconUrlLarge}";
                                }
                                else if (item.AssetDescription?.IconUrl != null)
                                {
                                    imageUrl = $"https://community.cloudflare.steamstatic.com/economy/image/{item.AssetDescription.IconUrl}";
                                }
                                
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
                                Console.WriteLine($"  ⚠ {item.HashName} - Sin precio disponible");
                            }
                            
                            // Pequeño delay para no saturar la API
                            await Task.Delay(300);
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
            
            return null;
        }
        catch
        {
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

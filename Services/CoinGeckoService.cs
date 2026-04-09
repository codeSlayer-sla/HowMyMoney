using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HowsMyMoney.Services;

/// <summary>
/// Servicio para obtener precios de criptomonedas usando CoinGecko API
/// </summary>
public class CoinGeckoService
{
    private readonly HttpClient _httpClient;
    private const string BaseUrl = "https://api.coingecko.com/api/v3";
    private static DateTime _lastApiCall = DateTime.MinValue;
    private static readonly TimeSpan MinTimeBetweenCalls = TimeSpan.FromSeconds(2);
    private static readonly Dictionary<string, (DateTime timestamp, List<CryptoFullInfo> data)> _priceCache = new();
    private static readonly TimeSpan CacheExpiration = TimeSpan.FromMinutes(1);
    
    public CoinGeckoService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "HowsMyMoney/1.0");
    }
    
    private async Task RateLimitAsync()
    {
        var timeSinceLastCall = DateTime.Now - _lastApiCall;
        if (timeSinceLastCall < MinTimeBetweenCalls)
        {
            var delay = MinTimeBetweenCalls - timeSinceLastCall;
            Console.WriteLine($"⏱ Rate limit: esperando {delay.TotalSeconds:F1}s...");
            await Task.Delay(delay);
        }
        _lastApiCall = DateTime.Now;
    }
    
    /// <summary>
    /// Obtiene el precio actual de una criptomoneda
    /// </summary>
    /// <param name="coinId">ID de la moneda en CoinGecko (ej: bitcoin, ethereum)</param>
    /// <returns>Precio en USD</returns>
    public async Task<decimal?> GetCryptoPriceAsync(string coinId)
    {
        try
        {
            // Rate limiting
            await RateLimitAsync();
            
            var url = $"{BaseUrl}/simple/price?ids={coinId.ToLower()}&vs_currencies=usd";
            var response = await _httpClient.GetStringAsync(url);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var result = JsonSerializer.Deserialize<Dictionary<string, CoinPrice>>(response, options);
            
            if (result != null && result.TryGetValue(coinId.ToLower(), out var coinPrice))
            {
                return coinPrice.Usd;
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo precio de {coinId}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Obtiene información completa de una criptomoneda (precio + imagen)
    /// </summary>
    /// <param name="coinId">ID de la moneda en CoinGecko (ej: bitcoin, ethereum)</param>
    /// <returns>Información completa de la cripto</returns>
    public async Task<CryptoFullInfo?> GetCryptoFullInfoAsync(string coinId)
    {
        try
        {
            // Rate limiting
            await RateLimitAsync();
            
            // Usar el endpoint /coins/{id} que retorna precio e imagen
            var url = $"{BaseUrl}/coins/{coinId.ToLower()}?localization=false&tickers=false&market_data=true&community_data=false&developer_data=false&sparkline=false";
            var response = await _httpClient.GetStringAsync(url);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var result = JsonSerializer.Deserialize<CoinFullData>(response, options);
            
            if (result != null && result.MarketData?.CurrentPrice?.TryGetValue("usd", out var usdPrice) == true)
            {
                return new CryptoFullInfo
                {
                    Id = result.Id,
                    Name = result.Name,
                    Symbol = result.Symbol,
                    ImageUrl = result.Image?.Large ?? result.Image?.Small ?? result.Image?.Thumb,
                    CurrentPrice = usdPrice,
                    PriceChange24h = result.MarketData?.PriceChange24h ?? 0,
                    PriceChangePercentage24h = result.MarketData?.PriceChangePercentage24h ?? 0
                };
            }
            
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo info completa de {coinId}: {ex.Message}");
            return null;
        }
    }

    /// <summary>
    /// Obtiene el top de criptomonedas por market cap en una sola petición
    /// </summary>
    /// <param name="limit">Número de cryptos a obtener (default: 10)</param>
    /// <returns>Lista de información de cryptos</returns>
    public async Task<List<CryptoFullInfo>> GetTopCryptosAsync(int limit = 10)
    {
        try
        {
            var cacheKey = $"top_{limit}";
            
            // Verificar caché
            if (_priceCache.TryGetValue(cacheKey, out var cached))
            {
                if (DateTime.Now - cached.timestamp < CacheExpiration)
                {
                    Console.WriteLine($"💾 Usando caché de precios (edad: {(DateTime.Now - cached.timestamp).TotalSeconds:F0}s)");
                    return cached.data;
                }
            }
            
            // Rate limiting
            await RateLimitAsync();
            
            // Endpoint markets trae múltiples cryptos en una sola petición
            var url = $"{BaseUrl}/coins/markets?vs_currency=usd&order=market_cap_desc&per_page={limit}&page=1&sparkline=false&price_change_percentage=24h";
            var response = await _httpClient.GetStringAsync(url);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var marketData = JsonSerializer.Deserialize<List<CoinMarketInfo>>(response, options);
            
            if (marketData != null)
            {
                var result = marketData.Select(coin => new CryptoFullInfo
                {
                    Id = coin.Id,
                    Name = coin.Name,
                    Symbol = coin.Symbol,
                    ImageUrl = coin.Image,
                    CurrentPrice = coin.CurrentPrice,
                    PriceChange24h = coin.PriceChange24h,
                    PriceChangePercentage24h = coin.PriceChangePercentage24h
                }).ToList();
                
                // Guardar en caché
                _priceCache[cacheKey] = (DateTime.Now, result);
                
                return result;
            }
            
            return new List<CryptoFullInfo>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo top cryptos: {ex.Message}");
            
            // Si hay error pero tenemos caché antiguo, usarlo
            var cacheKey = $"top_{limit}";
            if (_priceCache.TryGetValue(cacheKey, out var cached))
            {
                Console.WriteLine($"⚠️  Usando caché antiguo debido a error");
                return cached.data;
            }
            
            return new List<CryptoFullInfo>();
        }
    }
    
    /// <summary>
    /// Busca criptomonedas por nombre
    /// </summary>
    public async Task<List<CoinInfo>> SearchCoinsAsync(string query)
    {
        try
        {
            var url = $"{BaseUrl}/search?query={Uri.EscapeDataString(query)}";
            var response = await _httpClient.GetStringAsync(url);
            
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
            
            var result = JsonSerializer.Deserialize<SearchResult>(response, options);
            return result?.Coins ?? new List<CoinInfo>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error buscando criptomonedas: {ex.Message}");
            return new List<CoinInfo>();
        }
    }
    
    private class CoinPrice
    {
        [JsonPropertyName("usd")]
        public decimal Usd { get; set; }
    }
    
    private class SearchResult
    {
        [JsonPropertyName("coins")]
        public List<CoinInfo> Coins { get; set; } = new();
    }
}

public class CoinInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
    
    [JsonPropertyName("thumb")]
    public string? Thumb { get; set; }
    
    [JsonPropertyName("large")]
    public string? Large { get; set; }
}

/// <summary>
/// Información completa de una criptomoneda con precio e imagen
/// </summary>
public class CryptoFullInfo
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal PriceChange24h { get; set; }
    public decimal PriceChangePercentage24h { get; set; }
}

// Clase para deserializar el endpoint /coins/markets
public class CoinMarketInfo
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("image")]
    public string? Image { get; set; }
    
    [JsonPropertyName("current_price")]
    public decimal CurrentPrice { get; set; }
    
    [JsonPropertyName("price_change_24h")]
    public decimal PriceChange24h { get; set; }
    
    [JsonPropertyName("price_change_percentage_24h")]
    public decimal PriceChangePercentage24h { get; set; }
}

// Clases para deserializar respuesta completa de CoinGecko
public class CoinFullData
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;
    
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("symbol")]
    public string Symbol { get; set; } = string.Empty;
    
    [JsonPropertyName("image")]
    public CoinImageData? Image { get; set; }
    
    [JsonPropertyName("market_data")]
    public CoinMarketData? MarketData { get; set; }
}

public class CoinImageData
{
    [JsonPropertyName("thumb")]
    public string? Thumb { get; set; }
    
    [JsonPropertyName("small")]
    public string? Small { get; set; }
    
    [JsonPropertyName("large")]
    public string? Large { get; set; }
}

public class CoinMarketData
{
    [JsonPropertyName("current_price")]
    public Dictionary<string, decimal>? CurrentPrice { get; set; }
    
    [JsonPropertyName("price_change_24h")]
    public decimal? PriceChange24h { get; set; }
    
    [JsonPropertyName("price_change_percentage_24h")]
    public decimal? PriceChangePercentage24h { get; set; }
}

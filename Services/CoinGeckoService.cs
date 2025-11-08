using System;
using System.Collections.Generic;
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
    
    public CoinGeckoService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "HowsMyMoney/1.0");
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

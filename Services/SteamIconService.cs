using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace HowsMyMoney.Services;

/// <summary>
/// Resuelve íconos de skins de CS:GO consultando Steam Community Market UNA VEZ
/// por skin (nunca por precio). El resultado se guarda en Investment.ImageUrl, así
/// que en runs futuros ni siquiera se vuelve a llamar a este servicio para ese item
/// (ver InvestmentService.UpdateSkinAssetAsync -> needsImage).
/// </summary>
public class SteamIconService : ISteamIconService
{
    private const int CSGO_APP_ID = 730;
    private readonly HttpClient _httpClient;

    public SteamIconService(HttpClient? httpClient = null)
    {
        _httpClient = httpClient ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
    }

    public async Task<string?> GetIconUrlAsync(string marketHashName)
    {
        if (string.IsNullOrWhiteSpace(marketHashName))
        {
            return null;
        }

        try
        {
            // count=5 (no paginación) por si el nombre exacto no es el primer resultado
            // (ej. variantes StatTrak™ con nombre similar).
            var url = $"https://steamcommunity.com/market/search/render/?appid={CSGO_APP_ID}" +
                      $"&search_descriptions=0&query={Uri.EscapeDataString(marketHashName)}&start=0&count=5&norender=1";

            var json = await _httpClient.GetStringAsync(url);
            var result = JsonSerializer.Deserialize<SteamMarketSearchResult>(json);

            var match = result?.Results?.FirstOrDefault(r =>
                            r.HashName.Equals(marketHashName, StringComparison.OrdinalIgnoreCase))
                        ?? result?.Results?.FirstOrDefault();

            var iconPath = match?.AssetDescription?.IconUrlLarge ?? match?.AssetDescription?.IconUrl;
            if (string.IsNullOrEmpty(iconPath))
            {
                Console.WriteLine($"⚠ SteamIconService: sin ícono para '{marketHashName}'");
                return null;
            }

            return $"https://community.cloudflare.steamstatic.com/economy/image/{iconPath}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ SteamIconService: error resolviendo ícono para '{marketHashName}': {ex.Message}");
            return null;
        }
    }

    private class SteamMarketSearchResult
    {
        [JsonPropertyName("results")]
        public System.Collections.Generic.List<SteamMarketItem>? Results { get; set; }
    }

    private class SteamMarketItem
    {
        [JsonPropertyName("hash_name")]
        public string HashName { get; set; } = string.Empty;

        [JsonPropertyName("asset_description")]
        public SteamAssetDescription? AssetDescription { get; set; }
    }

    private class SteamAssetDescription
    {
        [JsonPropertyName("icon_url")]
        public string? IconUrl { get; set; }

        [JsonPropertyName("icon_url_large")]
        public string? IconUrlLarge { get; set; }
    }
}

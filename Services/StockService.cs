using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using HowsMyMoney.Models;

namespace HowsMyMoney.Services;

public class StockService
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl = "https://query1.finance.yahoo.com/v8/finance/chart/";
    
    public StockService()
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36");
    }
    
    /// <summary>
    /// Busca acciones por símbolo o nombre usando Yahoo Finance Search API
    /// </summary>
    public async Task<List<AssetSearchResult>> SearchStocksAsync(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
            return new List<AssetSearchResult>();
        
        var results = new List<AssetSearchResult>();
        
        try
        {
            // Usar Yahoo Finance Search API
            var searchUrl = $"https://query2.finance.yahoo.com/v1/finance/search?q={Uri.EscapeDataString(query)}&quotesCount=10&newsCount=0";
            
            Console.WriteLine($"🔍 Buscando en Yahoo Finance: {query}");
            
            var response = await _httpClient.GetAsync(searchUrl);
            
            if (response.IsSuccessStatusCode)
            {
                var json = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(json);
                
                if (doc.RootElement.TryGetProperty("quotes", out var quotes))
                {
                    foreach (var quote in quotes.EnumerateArray())
                    {
                        if (quote.TryGetProperty("symbol", out var symbolProp))
                        {
                            var symbol = symbolProp.GetString();
                            var name = quote.TryGetProperty("shortname", out var nameProp) 
                                ? nameProp.GetString() 
                                : quote.TryGetProperty("longname", out var longnameProp)
                                    ? longnameProp.GetString()
                                    : symbol;
                            
                            var quoteType = quote.TryGetProperty("quoteType", out var typeProp) 
                                ? typeProp.GetString() 
                                : "";
                            
                            // Solo incluir acciones y ETFs
                            if ((quoteType == "EQUITY" || quoteType == "ETF") && !string.IsNullOrEmpty(symbol))
                            {
                                results.Add(new AssetSearchResult
                                {
                                    Symbol = symbol ?? "",
                                    Name = name ?? symbol ?? "",
                                    CurrentPrice = 0, // Se llenará después en paralelo
                                    ImageUrl = $"https://logo.clearbit.com/{GetDomainFromSymbol(symbol ?? "")}.com",
                                    AssetType = AssetType.Accion
                                });
                            }
                        }
                    }
                }
            }
            
            // OPTIMIZACIÓN: Obtener precios en PARALELO después de tener todos los símbolos
            if (results.Any())
            {
                Console.WriteLine($"📊 Obteniendo precios para {results.Count} acciones en paralelo...");
                
                var priceTasks = results.Select(async result =>
                {
                    var price = await GetStockPriceAsync(result.Symbol);
                    if (price.HasValue && price.Value > 0)
                    {
                        result.CurrentPrice = price.Value;
                        Console.WriteLine($"  ✓ {result.Symbol}: {result.Name} - ${price.Value}");
                        return result;
                    }
                    return null;
                }).ToList();
                
                var priceResults = await Task.WhenAll(priceTasks);
                
                // Filtrar solo los que tienen precio
                results = priceResults.Where(r => r != null && r.CurrentPrice > 0).ToList()!;
            }
            
            Console.WriteLine($"✓ Total: {results.Count} acciones/ETFs encontrados");
            return results;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error buscando acciones: {ex.Message}");
            return new List<AssetSearchResult>();
        }
    }
    
    /// <summary>
    /// Obtiene el precio actual de una acción por su símbolo
    /// </summary>
    public async Task<decimal?> GetStockPriceAsync(string symbol)
    {
        try
        {
            var url = $"{_baseUrl}{symbol}?interval=1d&range=1d";
            var response = await _httpClient.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"Error obteniendo precio de {symbol}: {response.StatusCode}");
                return null;
            }
            
            var jsonString = await response.Content.ReadAsStringAsync();
            
            using var document = JsonDocument.Parse(jsonString);
            var root = document.RootElement;
            
            // Navegar por el JSON de Yahoo Finance
            if (root.TryGetProperty("chart", out var chart) &&
                chart.TryGetProperty("result", out var result) &&
                result.GetArrayLength() > 0)
            {
                var firstResult = result[0];
                
                if (firstResult.TryGetProperty("meta", out var meta) &&
                    meta.TryGetProperty("regularMarketPrice", out var priceElement))
                {
                    var price = priceElement.GetDecimal();
                    Console.WriteLine($"Precio de {symbol}: ${price}");
                    return price;
                }
            }
            
            Console.WriteLine($"No se encontró precio para {symbol}");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo precio de {symbol}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Obtiene información completa de una acción (nombre, precio, etc.)
    /// </summary>
    public async Task<(string Name, decimal Price)?> GetStockInfoAsync(string symbol)
    {
        try
        {
            var price = await GetStockPriceAsync(symbol);
            
            if (price == null)
                return null;
            
            // Aquí podrías agregar lógica para obtener el nombre real
            // Por ahora retornamos solo el símbolo
            return (symbol, price.Value);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error obteniendo info de {symbol}: {ex.Message}");
            return null;
        }
    }
    
    /// <summary>
    /// Obtiene el dominio web de una empresa basado en su símbolo
    /// Para usar con el servicio de logos de Clearbit
    /// </summary>
    private string GetDomainFromSymbol(string symbol)
    {
        return symbol.ToUpper() switch
        {
            // Tech
            "AAPL" => "apple",
            "MSFT" => "microsoft",
            "GOOGL" or "GOOG" => "google",
            "AMZN" => "amazon",
            "META" or "FB" => "meta",
            "TSLA" => "tesla",
            "NVDA" => "nvidia",
            "NFLX" => "netflix",
            "AMD" => "amd",
            "INTC" => "intel",
            
            // ETFs - Fondos más populares
            "SPY" => "spdr",
            "QQQ" => "invesco",
            "VOO" => "vanguard",
            "VTI" => "vanguard",
            "IWM" => "ishares",
            "DIA" => "spdr",
            "VEA" => "vanguard",
            "VWO" => "vanguard",
            "AGG" => "ishares",
            "BND" => "vanguard",
            
            // Finance
            "JPM" => "jpmorganchase",
            "BAC" => "bankofamerica",
            "WFC" => "wellsfargo",
            "GS" => "goldmansachs",
            "V" => "visa",
            "MA" => "mastercard",
            
            // Consumer
            "KO" => "coca-cola",
            "PEP" => "pepsi",
            "WMT" => "walmart",
            "DIS" => "disney",
            "NKE" => "nike",
            "MCD" => "mcdonalds",
            
            _ => symbol.ToLower()
        };
    }
}

using System.Collections.Generic;
using System.Threading.Tasks;
using HowsMyMoney.Models;

namespace HowsMyMoney.Services;

/// <summary>
/// Servicio para obtener precios de acciones/ETFs usando Yahoo Finance.
/// </summary>
public interface IStockService
{
    Task<List<AssetSearchResult>> SearchStocksAsync(string query);
    Task<decimal?> GetStockPriceAsync(string symbol);
    Task<(string Name, decimal Price)?> GetStockInfoAsync(string symbol);

    /// <summary>
    /// Precio + % de cambio respecto al cierre anterior (para el monitor de mercados).
    /// Usa el mismo endpoint que GetStockPriceAsync, solo que también lee
    /// "chartPreviousClose" de la respuesta en vez de descartarlo.
    /// </summary>
    Task<StockQuote?> GetStockQuoteAsync(string symbol);
}

public class StockQuote
{
    public decimal Price { get; set; }
    public decimal Change { get; set; }
    public decimal ChangePercent { get; set; }
}

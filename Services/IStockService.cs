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
}

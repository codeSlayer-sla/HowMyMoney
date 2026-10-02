namespace HowsMyMoney.Models;

public class MarketData
{
    public string Symbol { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Change24h { get; set; }
    public decimal ChangePercent24h { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public MarketType MarketType { get; set; }

    /// <summary>
    /// Capitalización de mercado (cripto: dato real de CoinGecko). Para skins es un
    /// proxy (precio × cantidad listada), no una cap real. Null cuando no hay dato
    /// (acciones, por ahora).
    /// </summary>
    public decimal? MarketCap { get; set; }
}

public enum MarketType
{
    Crypto,
    Stocks,
    Skins,
    Forex,
    Commodities
}

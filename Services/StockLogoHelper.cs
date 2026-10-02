namespace HowsMyMoney.Services;

/// <summary>
/// Mapea símbolos de acciones/ETFs a su dominio web para pedir el logo de la empresa.
/// Antes esta misma tabla estaba copiada en InvestmentService, StockService y
/// MarketMonitorViewModel. También antes se usaba logo.clearbit.com, que dejó de
/// resolver (el servicio gratuito de Clearbit ya no existe) — ahora se usa el
/// servicio de favicons de Google, que sigue funcionando.
/// </summary>
public static class StockLogoHelper
{
    /// <summary>URL del logo (favicon) de la empresa dueña del símbolo dado.</summary>
    public static string GetLogoUrl(string symbol) =>
        $"https://www.google.com/s2/favicons?domain={GetDomainFromSymbol(symbol)}.com&sz=64";

    public static string GetDomainFromSymbol(string symbol)
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

            // Otras grandes empresas sin categoría arriba
            "BRK-B" => "berkshirehathaway",

            _ => symbol.ToLower()
        };
    }
}

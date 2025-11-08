namespace HowsMyMoney.Models;

/// <summary>
/// Resultado de búsqueda de un activo con toda su información
/// </summary>
public class AssetSearchResult
{
    public string Name { get; set; } = string.Empty;
    public string Symbol { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public decimal CurrentPrice { get; set; }
    public AssetType AssetType { get; set; }
    
    /// <summary>
    /// Texto para mostrar en la UI
    /// </summary>
    public string DisplayText => $"{Name} ({Symbol}) - ${CurrentPrice:N2}";
}

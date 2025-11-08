using System;
using System.ComponentModel.DataAnnotations;

namespace HowsMyMoney.Models;

/// <summary>
/// Representa una inversión individual
/// </summary>
public class Investment
{
    [Key]
    public int Id { get; set; }
    
    /// <summary>
    /// Nombre del activo (ej: Bitcoin, AK-47 | Redline, Reloj Rolex)
    /// </summary>
    [Required]
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Tipo de activo
    /// </summary>
    public AssetType AssetType { get; set; }
    
    /// <summary>
    /// Símbolo o identificador para API (ej: bitcoin, btc, skin_id)
    /// </summary>
    public string? Symbol { get; set; }
    
    /// <summary>
    /// Cantidad de unidades
    /// </summary>
    public decimal Quantity { get; set; }
    
    /// <summary>
    /// Precio de compra por unidad (en USD)
    /// </summary>
    public decimal PurchasePrice { get; set; }
    
    /// <summary>
    /// Precio actual por unidad (en USD)
    /// </summary>
    public decimal CurrentPrice { get; set; }
    
    /// <summary>
    /// Fecha de compra
    /// </summary>
    public DateTime PurchaseDate { get; set; }
    
    /// <summary>
    /// Última actualización del precio
    /// </summary>
    public DateTime LastPriceUpdate { get; set; }
    
    /// <summary>
    /// Notas adicionales
    /// </summary>
    public string? Notes { get; set; }
    
    /// <summary>
    /// URL de la imagen del activo
    /// </summary>
    public string? ImageUrl { get; set; }
    
    // Propiedades calculadas
    
    /// <summary>
    /// Valor total de compra
    /// </summary>
    public decimal TotalPurchaseValue => Quantity * PurchasePrice;
    
    /// <summary>
    /// Valor actual total
    /// </summary>
    public decimal CurrentValue => Quantity * CurrentPrice;
    
    /// <summary>
    /// Ganancia/Pérdida en USD
    /// </summary>
    public decimal ProfitLoss => CurrentValue - TotalPurchaseValue;
    
    /// <summary>
    /// Rentabilidad en porcentaje
    /// Para earnings (PurchasePrice = 0), retorna infinito teóricamente, pero usamos 0 para evitar errores
    /// </summary>
    public decimal ProfitLossPercentage => TotalPurchaseValue > 0 
        ? (ProfitLoss / TotalPurchaseValue) * 100 
        : 0; // Para earnings es ganancia infinita (100% profit con $0 invertido)
}

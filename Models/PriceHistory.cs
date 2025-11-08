using System;
using System.ComponentModel.DataAnnotations;

namespace HowsMyMoney.Models;

/// <summary>
/// Historial de precios para tracking mensual
/// </summary>
public class PriceHistory
{
    [Key]
    public int Id { get; set; }
    
    public int InvestmentId { get; set; }
    
    public Investment? Investment { get; set; }
    
    public decimal Price { get; set; }
    
    public DateTime RecordedAt { get; set; }
    
    /// <summary>
    /// Mes en formato YYYY-MM para agrupación
    /// </summary>
    public string Month { get; set; } = string.Empty;
}

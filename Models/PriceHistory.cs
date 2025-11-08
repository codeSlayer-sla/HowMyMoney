using System;
using System.ComponentModel.DataAnnotations;

namespace HowsMyMoney.Models;

/// <summary>
/// Historial de precios para tracking diario/semanal/mensual
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
    /// Día en formato YYYY-MM-DD para agrupación diaria
    /// </summary>
    public string Day { get; set; } = string.Empty;
    
    /// <summary>
    /// Semana en formato YYYY-Www (ej: 2024-W45) para agrupación semanal
    /// </summary>
    public string Week { get; set; } = string.Empty;
    
    /// <summary>
    /// Mes en formato YYYY-MM para agrupación mensual
    /// </summary>
    public string Month { get; set; } = string.Empty;
}

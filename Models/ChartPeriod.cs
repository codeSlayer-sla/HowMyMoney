namespace HowsMyMoney.Models;

/// <summary>
/// Período de tiempo para visualización de gráficos
/// </summary>
public enum ChartPeriod
{
    /// <summary>
    /// Vista diaria (últimos 30 días)
    /// </summary>
    Daily,
    
    /// <summary>
    /// Vista semanal (últimas 12 semanas)
    /// </summary>
    Weekly,
    
    /// <summary>
    /// Vista mensual (últimos 12 meses)
    /// </summary>
    Monthly
}

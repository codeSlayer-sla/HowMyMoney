using System.Collections.Generic;
using System.Threading.Tasks;
using HowsMyMoney.Models;

namespace HowsMyMoney.Services;

/// <summary>
/// Servicio para gestionar inversiones: CRUD, refresco de precios y métricas de portfolio.
/// </summary>
public interface IInvestmentService
{
    Task<List<Investment>> GetAllInvestmentsAsync();
    Task<Investment> AddInvestmentAsync(Investment investment);
    Task<Investment?> UpdateInvestmentAsync(Investment investment);
    Task<bool> DeleteInvestmentAsync(int id);
    Task UpdateAllPricesAsync();
    Task<List<PriceHistory>> GetPriceHistoryAsync(int investmentId);
    Task<PortfolioSummary> GetPortfolioSummaryAsync();
    Task<List<PerformanceData>> GetPerformanceByPeriodAsync(ChartPeriod period);
    Task<List<MonthlyPerformance>> GetMonthlyPerformanceAsync();
}

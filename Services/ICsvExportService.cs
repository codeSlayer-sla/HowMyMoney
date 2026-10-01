using System.Collections.Generic;
using System.Threading.Tasks;
using HowsMyMoney.Models;

namespace HowsMyMoney.Services;

/// <summary>
/// Servicio para exportar inversiones a CSV.
/// </summary>
public interface ICsvExportService
{
    Task ExportInvestmentsAsync(List<Investment> investments, string filePath);
    string GetDefaultExportPath();
}

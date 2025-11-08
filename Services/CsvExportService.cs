using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CsvHelper;
using CsvHelper.Configuration;
using HowsMyMoney.Models;

namespace HowsMyMoney.Services;

/// <summary>
/// Servicio para exportar datos a CSV
/// </summary>
public class CsvExportService
{
    /// <summary>
    /// Exporta inversiones a un archivo CSV
    /// </summary>
    public async Task ExportInvestmentsAsync(List<Investment> investments, string filePath)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            HasHeaderRecord = true,
            Delimiter = ","
        };
        
        await using var writer = new StreamWriter(filePath);
        await using var csv = new CsvWriter(writer, config);
        
        // Escribir encabezados personalizados
        csv.WriteField("Nombre");
        csv.WriteField("Tipo");
        csv.WriteField("Símbolo");
        csv.WriteField("Cantidad");
        csv.WriteField("Precio Compra");
        csv.WriteField("Precio Actual");
        csv.WriteField("Valor Compra");
        csv.WriteField("Valor Actual");
        csv.WriteField("Ganancia/Pérdida");
        csv.WriteField("Rentabilidad %");
        csv.WriteField("Fecha Compra");
        csv.WriteField("Última Actualización");
        csv.WriteField("Notas");
        await csv.NextRecordAsync();
        
        // Escribir datos
        foreach (var investment in investments)
        {
            csv.WriteField(investment.Name);
            csv.WriteField(investment.AssetType.ToString());
            csv.WriteField(investment.Symbol ?? "");
            csv.WriteField(investment.Quantity);
            csv.WriteField(investment.PurchasePrice);
            csv.WriteField(investment.CurrentPrice);
            csv.WriteField(investment.TotalPurchaseValue);
            csv.WriteField(investment.CurrentValue);
            csv.WriteField(investment.ProfitLoss);
            csv.WriteField($"{investment.ProfitLossPercentage:F2}%");
            csv.WriteField(investment.PurchaseDate.ToString("yyyy-MM-dd"));
            csv.WriteField(investment.LastPriceUpdate.ToString("yyyy-MM-dd HH:mm"));
            csv.WriteField(investment.Notes ?? "");
            await csv.NextRecordAsync();
        }
    }
    
    /// <summary>
    /// Obtiene la ruta predeterminada para exportar
    /// </summary>
    public string GetDefaultExportPath()
    {
        var documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var fileName = $"HowsMyMoney_Export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
        return Path.Combine(documentsPath, fileName);
    }
}

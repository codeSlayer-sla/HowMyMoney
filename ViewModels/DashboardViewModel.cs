using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HowsMyMoney.Models;
using HowsMyMoney.Services;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace HowsMyMoney.ViewModels;

public partial class DashboardViewModel : ViewModelBase
{
    private readonly InvestmentService _investmentService;
    private readonly CsvExportService _csvExportService;
    
    [ObservableProperty]
    private ObservableCollection<Investment> _investments = new();
    
    [ObservableProperty]
    private ObservableCollection<Investment> _cryptoInvestments = new();
    
    [ObservableProperty]
    private ObservableCollection<Investment> _skinInvestments = new();
    
    [ObservableProperty]
    private ObservableCollection<Investment> _manualInvestments = new();
    
    [ObservableProperty]
    private ObservableCollection<CryptoSummary> _cryptoSummaries = new();
    
    [ObservableProperty]
    private CryptoSummary? _selectedCryptoForAdjustment;
    
    [ObservableProperty]
    private decimal _newCryptoQuantity;
    
    [ObservableProperty]
    private bool _isAdjustmentDialogOpen;
    
    [ObservableProperty]
    private int _totalInvestments;
    
    [ObservableProperty]
    private decimal _totalInvested;
    
    [ObservableProperty]
    private decimal _currentValue;
    
    [ObservableProperty]
    private decimal _totalProfitLoss;
    
    [ObservableProperty]
    private decimal _profitLossPercentage;
    
    [ObservableProperty]
    private string _statusMessage = "Listo";
    
    [ObservableProperty]
    private bool _isLoading;
    
    [ObservableProperty]
    private Investment? _selectedInvestment;
    
    // Gráficos
    [ObservableProperty]
    private ISeries[] _monthlyPerformanceSeries = Array.Empty<ISeries>();
    
    [ObservableProperty]
    private ISeries[] _assetDistributionSeries = Array.Empty<ISeries>();
    
    public DashboardViewModel()
    {
        _investmentService = new InvestmentService();
        _csvExportService = new CsvExportService();
        
        _ = LoadDataAsync();
    }
    
    [RelayCommand]
    private async Task LoadDataAsync()
    {
        IsLoading = true;
        StatusMessage = "Cargando inversiones...";
        
        try
        {
            Console.WriteLine("🔄 DashboardViewModel: Cargando inversiones...");
            var investments = await _investmentService.GetAllInvestmentsAsync();
            Console.WriteLine($"✓ DashboardViewModel: Obtenidas {investments.Count} inversiones de la base de datos");
            
            Investments.Clear();
            CryptoInvestments.Clear();
            SkinInvestments.Clear();
            ManualInvestments.Clear();
            
            foreach (var inv in investments)
            {
                Console.WriteLine($"   • Agregando: {inv.Name} - ${inv.CurrentPrice}");
                Investments.Add(inv);
                
                // Agrupar por categoría
                switch (inv.AssetType)
                {
                    case AssetType.Criptomoneda:
                        CryptoInvestments.Add(inv);
                        break;
                    case AssetType.SkinCSGO:
                        SkinInvestments.Add(inv);
                        break;
                    case AssetType.Manual:
                        ManualInvestments.Add(inv);
                        break;
                }
            }
            Console.WriteLine($"✓ DashboardViewModel: Investments.Count = {Investments.Count}");
            Console.WriteLine($"   📊 Crypto: {CryptoInvestments.Count}, Skins: {SkinInvestments.Count}, Manual: {ManualInvestments.Count}");
            
            // Crear resumen de criptomonedas por tipo
            CreateCryptoSummary();
            
            // Forzar notificación de cambio
            OnPropertyChanged(nameof(Investments));
            OnPropertyChanged(nameof(CryptoInvestments));
            OnPropertyChanged(nameof(SkinInvestments));
            OnPropertyChanged(nameof(ManualInvestments));
            
            var summary = await _investmentService.GetPortfolioSummaryAsync();
            TotalInvestments = summary.TotalInvestments;
            TotalInvested = summary.TotalInvested;
            CurrentValue = summary.CurrentValue;
            TotalProfitLoss = summary.TotalProfitLoss;
            ProfitLossPercentage = summary.TotalProfitLossPercentage;
            
            Console.WriteLine($"📊 Resumen:");
            Console.WriteLine($"   Total Inversiones: {TotalInvestments}");
            Console.WriteLine($"   Total Invertido: ${TotalInvested:N2}");
            Console.WriteLine($"   Valor Actual: ${CurrentValue:N2}");
            
            await LoadChartsAsync();
            
            StatusMessage = $"Última actualización: {DateTime.Now:HH:mm:ss}";
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ ERROR en LoadDataAsync: {ex.Message}");
            Console.WriteLine($"✗ StackTrace: {ex.StackTrace}");
            StatusMessage = $"Error: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task UpdatePricesAsync()
    {
        IsLoading = true;
        StatusMessage = "Actualizando precios...";
        
        try
        {
            await _investmentService.UpdateAllPricesAsync();
            await LoadDataAsync();
            StatusMessage = "Precios actualizados correctamente";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error actualizando precios: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task DeleteInvestmentAsync(Investment? investment)
    {
        if (investment == null) return;
        
        try
        {
            await _investmentService.DeleteInvestmentAsync(investment.Id);
            await LoadDataAsync();
            StatusMessage = $"Inversión '{investment.Name}' eliminada";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error eliminando inversión: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private void OpenAdjustCryptoDialog(CryptoSummary? crypto)
    {
        if (crypto == null) return;
        
        SelectedCryptoForAdjustment = crypto;
        NewCryptoQuantity = crypto.TotalQuantity;
        IsAdjustmentDialogOpen = true;
        StatusMessage = $"Ajustando {crypto.Symbol}...";
    }
    
    [RelayCommand]
    private void CloseAdjustCryptoDialog()
    {
        IsAdjustmentDialogOpen = false;
        SelectedCryptoForAdjustment = null;
        NewCryptoQuantity = 0;
    }
    
    [RelayCommand]
    private async Task AdjustCryptoQuantityAsync()
    {
        if (SelectedCryptoForAdjustment == null) return;
        
        try
        {
            IsLoading = true;
            StatusMessage = $"Ajustando cantidad de {SelectedCryptoForAdjustment.Symbol}...";
            
            // Obtener todas las inversiones de esta cripto
            var cryptoInvestmentsToAdjust = CryptoInvestments
                .Where(i => i.Name.Equals(SelectedCryptoForAdjustment.Symbol, StringComparison.OrdinalIgnoreCase))
                .ToList();
            
            if (!cryptoInvestmentsToAdjust.Any())
            {
                StatusMessage = "No se encontraron inversiones para ajustar";
                return;
            }
            
            decimal oldTotalQuantity = cryptoInvestmentsToAdjust.Sum(i => i.Quantity);
            decimal difference = NewCryptoQuantity - oldTotalQuantity;
            
            Console.WriteLine($"📊 Ajuste de {SelectedCryptoForAdjustment.Symbol}:");
            Console.WriteLine($"   Cantidad actual: {oldTotalQuantity:N8}");
            Console.WriteLine($"   Nueva cantidad: {NewCryptoQuantity:N8}");
            Console.WriteLine($"   Diferencia (earnings): {difference:N8}");
            
            if (difference == 0)
            {
                StatusMessage = "No hay cambios en la cantidad";
                IsAdjustmentDialogOpen = false;
                return;
            }
            
            if (difference < 0)
            {
                StatusMessage = "⚠️ La nueva cantidad no puede ser menor a la actual. Para reducir, elimina inversiones.";
                IsAdjustmentDialogOpen = false;
                return;
            }
            
            // NUEVA ESTRATEGIA: Crear un nuevo registro de "compra" con tag "EARNING"
            // Esto mantiene la trazabilidad completa sin modificar registros existentes
            // IMPORTANTE: PurchasePrice = 0 porque NO es una inversión, es GANANCIA PURA
            
            var firstInvestment = cryptoInvestmentsToAdjust.First();
            decimal currentPrice = SelectedCryptoForAdjustment.CurrentPrice;
            
            var earningInvestment = new Investment
            {
                Name = $"{SelectedCryptoForAdjustment.Symbol} [EARNING]",
                AssetType = AssetType.Criptomoneda,
                Quantity = difference,
                PurchasePrice = 0, // ⚠️ CERO porque NO invertiste nada, es ganancia
                CurrentPrice = currentPrice,
                PurchaseDate = DateTime.Now,
                ImageUrl = firstInvestment.ImageUrl
            };
            
            Console.WriteLine($"   ✨ Creando registro de earning:");
            Console.WriteLine($"      Nombre: {earningInvestment.Name}");
            Console.WriteLine($"      Cantidad: +{difference:N8}");
            Console.WriteLine($"      Precio compra: $0.00 (NO es inversión)");
            Console.WriteLine($"      Precio actual: ${currentPrice:N2}");
            Console.WriteLine($"      Valor ganado: ${difference * currentPrice:N2}");
            
            // Guardar el nuevo registro
            await _investmentService.AddInvestmentAsync(earningInvestment);
            
            // Recargar datos
            await LoadDataAsync();
            
            StatusMessage = $"✓ {SelectedCryptoForAdjustment.Symbol} ajustado: +{difference:N8} earnings registrados como nueva inversión";
            IsAdjustmentDialogOpen = false;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error ajustando cantidad: {ex.Message}";
            Console.WriteLine($"✗ Error en AdjustCryptoQuantityAsync: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }
    
    [RelayCommand]
    private async Task ExportToCsvAsync()
    {
        try
        {
            var filePath = _csvExportService.GetDefaultExportPath();
            await _csvExportService.ExportInvestmentsAsync(Investments.ToList(), filePath);
            StatusMessage = $"Exportado a: {filePath}";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error exportando: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private void ShowAddInvestment()
    {
        // Implementar navegación o diálogo
        StatusMessage = "Abriendo formulario de nueva inversión...";
    }
    
    private async Task LoadChartsAsync()
    {
        try
        {
            // Gráfico de rendimiento mensual
            var monthlyData = await _investmentService.GetMonthlyPerformanceAsync();
            if (monthlyData.Any())
            {
                MonthlyPerformanceSeries = new ISeries[]
                {
                    new LineSeries<decimal>
                    {
                        Values = monthlyData.Select(m => m.TotalValue).ToArray(),
                        Name = "Valor del Portfolio",
                        Fill = null,
                        Stroke = new SolidColorPaint(SKColors.DodgerBlue) { StrokeThickness = 3 }
                    }
                };
            }
            
            // Gráfico de distribución por tipo
            var cryptoValue = Investments
                .Where(i => i.AssetType == AssetType.Criptomoneda)
                .Sum(i => i.CurrentValue);
            
            var skinsValue = Investments
                .Where(i => i.AssetType == AssetType.SkinCSGO)
                .Sum(i => i.CurrentValue);
            
            var manualValue = Investments
                .Where(i => i.AssetType == AssetType.Manual)
                .Sum(i => i.CurrentValue);
            
            AssetDistributionSeries = new ISeries[]
            {
                new PieSeries<decimal> { Values = new[] { cryptoValue }, Name = "Criptomonedas" },
                new PieSeries<decimal> { Values = new[] { skinsValue }, Name = "Skins CS:GO" },
                new PieSeries<decimal> { Values = new[] { manualValue }, Name = "Manual" }
            };
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error cargando gráficos: {ex.Message}");
        }
    }
    
    private void CreateCryptoSummary()
    {
        CryptoSummaries.Clear();
        
        // Agrupar criptomonedas por símbolo base (eliminando el tag [EARNING])
        var cryptoGroups = CryptoInvestments
            .GroupBy(i => {
                // Extraer símbolo base: "BITCOIN [EARNING]" → "BITCOIN"
                var name = i.Name.ToUpper();
                var earningIndex = name.IndexOf(" [EARNING]");
                return earningIndex >= 0 ? name.Substring(0, earningIndex) : name;
            })
            .Select(g => new CryptoSummary
            {
                Symbol = g.Key,
                TotalQuantity = g.Sum(i => i.Quantity),
                TotalInvested = g.Sum(i => i.TotalPurchaseValue), // Los earnings tienen PurchasePrice = 0
                CurrentValue = g.Sum(i => i.CurrentValue),
                CurrentPrice = g.First().CurrentPrice,
                ProfitLoss = g.Sum(i => i.ProfitLoss),
                ProfitLossPercentage = g.Sum(i => i.TotalPurchaseValue) > 0 
                    ? (g.Sum(i => i.CurrentValue) - g.Sum(i => i.TotalPurchaseValue)) / g.Sum(i => i.TotalPurchaseValue) * 100 
                    : 0,
                ImageUrl = g.First().ImageUrl
            })
            .OrderByDescending(c => c.CurrentValue)
            .ToList();
        
        foreach (var crypto in cryptoGroups)
        {
            CryptoSummaries.Add(crypto);
            Console.WriteLine($"   💰 {crypto.Symbol}: {crypto.TotalQuantity:N8} unidades = ${crypto.CurrentValue:N2}");
        }
    }
}

/// <summary>
/// Resumen de criptomonedas agrupadas por símbolo
/// </summary>
public class CryptoSummary
{
    public string Symbol { get; set; } = string.Empty;
    public decimal TotalQuantity { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal CurrentPrice { get; set; }
    public decimal ProfitLoss { get; set; }
    public decimal ProfitLossPercentage { get; set; }
    public string? ImageUrl { get; set; }
}

/// <summary>
/// Convertidor para colorear ganancias (verde) y pérdidas (rojo)
/// </summary>
public class ProfitLossColorConverter : Avalonia.Data.Converters.IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is decimal profitLoss)
        {
            if (profitLoss > 0)
                return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(76, 175, 80)); // Verde #4CAF50
            else if (profitLoss < 0)
                return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(244, 67, 54)); // Rojo #F44336
            else
                return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromRgb(158, 158, 158)); // Gris #9E9E9E
        }
        return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Gray);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

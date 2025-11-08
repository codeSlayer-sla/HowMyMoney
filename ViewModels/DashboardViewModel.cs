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
    
    [ObservableProperty]
    private ChartPeriod _selectedPeriod = ChartPeriod.Monthly;
    
    [ObservableProperty]
    private string _chartTitle = "Rendimiento Mensual";
    
    [ObservableProperty]
    private string[] _chartLabels = Array.Empty<string>();
    
    [ObservableProperty]
    private bool _isLoadingChart = false;
    
    // Propiedades para indicar qué botón está activo
    public bool IsDailyActive => SelectedPeriod == ChartPeriod.Daily;
    public bool IsWeeklyActive => SelectedPeriod == ChartPeriod.Weekly;
    public bool IsMonthlyActive => SelectedPeriod == ChartPeriod.Monthly;
    
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
    
    [RelayCommand]
    private async Task ChangePeriodAsync(string period)
    {
        if (IsLoadingChart) return; // Evitar múltiples clics
        
        var newPeriod = period switch
        {
            "Daily" => ChartPeriod.Daily,
            "Weekly" => ChartPeriod.Weekly,
            "Monthly" => ChartPeriod.Monthly,
            _ => ChartPeriod.Monthly
        };
        
        // Si ya está seleccionado, no hacer nada
        if (newPeriod == SelectedPeriod) return;
        
        Console.WriteLine($"📊 Cambiando período a: {newPeriod}");
        IsLoadingChart = true;
        
        SelectedPeriod = newPeriod;
        
        // Notificar cambios en los estados activos
        OnPropertyChanged(nameof(IsDailyActive));
        OnPropertyChanged(nameof(IsWeeklyActive));
        OnPropertyChanged(nameof(IsMonthlyActive));
        
        ChartTitle = SelectedPeriod switch
        {
            ChartPeriod.Daily => "Rendimiento Diario (últimos 30 días)",
            ChartPeriod.Weekly => "Rendimiento Semanal (últimas 12 semanas)",
            ChartPeriod.Monthly => "Rendimiento Mensual (últimos 12 meses)",
            _ => "Rendimiento Mensual"
        };
        
        await LoadChartsAsync();
        IsLoadingChart = false;
    }
    
    private async Task LoadChartsAsync()
    {
        try
        {
            // Gráfico de rendimiento por período seleccionado
            var performanceData = await _investmentService.GetPerformanceByPeriodAsync(SelectedPeriod);
            
            if (performanceData.Any())
            {
                // Preparar etiquetas según el período
                ChartLabels = performanceData.Select(p => FormatLabel(p.Period, SelectedPeriod)).ToArray();
                
                // Calcular cambio porcentual
                var firstValue = performanceData.First().TotalValue;
                var lastValue = performanceData.Last().TotalValue;
                var percentChange = firstValue > 0 ? ((lastValue - firstValue) / firstValue * 100) : 0;
                var changeSymbol = percentChange >= 0 ? "▲" : "▼";
                
                MonthlyPerformanceSeries = new ISeries[]
                {
                    new LineSeries<decimal>
                    {
                        Values = performanceData.Select(p => p.TotalValue).ToArray(),
                        Name = $"Portfolio {changeSymbol} {Math.Abs(percentChange):F2}%",
                        Fill = new SolidColorPaint(new SKColor(59, 130, 246, 50)),
                        Stroke = new SolidColorPaint(new SKColor(59, 130, 246)) { StrokeThickness = 3 },
                        GeometrySize = 10,
                        GeometryStroke = new SolidColorPaint(new SKColor(59, 130, 246)) { StrokeThickness = 3 },
                        GeometryFill = new SolidColorPaint(SKColors.White),
                        LineSmoothness = 0.65
                    }
                };
                
                Console.WriteLine($"📊 Gráfico cargado: {performanceData.Count} puntos | Cambio: {changeSymbol}{Math.Abs(percentChange):F2}%");
            }
            else
            {
                // Sin datos, mostrar mensaje
                MonthlyPerformanceSeries = new ISeries[]
                {
                    new LineSeries<decimal>
                    {
                        Values = new decimal[] { 0 },
                        Name = "Sin datos disponibles",
                        Fill = null,
                        Stroke = new SolidColorPaint(new SKColor(128, 128, 128)) { StrokeThickness = 2 }
                    }
                };
                Console.WriteLine($"⚠ No hay datos de historial para el período {SelectedPeriod}");
                StatusMessage = "Actualiza los precios para comenzar a ver el historial";
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
    
    private string FormatLabel(string period, ChartPeriod chartPeriod)
    {
        try
        {
            return chartPeriod switch
            {
                ChartPeriod.Daily => DateTime.Parse(period).ToString("dd MMM"),
                ChartPeriod.Weekly => period.Replace("-W", " S"),
                ChartPeriod.Monthly => DateTime.Parse(period + "-01").ToString("MMM yyyy"),
                _ => period
            };
        }
        catch
        {
            return period;
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
            .Select(g => {
                // Calcular una vez para evitar múltiples iteraciones
                var totalInvested = g.Sum(i => i.TotalPurchaseValue);
                var currentValue = g.Sum(i => i.CurrentValue);
                
                return new CryptoSummary
                {
                    Symbol = g.Key,
                    TotalQuantity = g.Sum(i => i.Quantity),
                    TotalInvested = totalInvested,
                    CurrentValue = currentValue,
                    CurrentPrice = g.First().CurrentPrice,
                    ProfitLoss = g.Sum(i => i.ProfitLoss),
                    ProfitLossPercentage = totalInvested > 0 
                        ? (currentValue - totalInvested) / totalInvested * 100 
                        : 0,
                    ImageUrl = g.First().ImageUrl
                };
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

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using HowsMyMoney.Data;
using HowsMyMoney.Models;

namespace HowsMyMoney.Services;

/// <summary>
/// Servicio para gestionar inversiones
/// </summary>
public class InvestmentService
{
    private readonly InvestmentDbContext _context;
    private readonly CoinGeckoService _coinGeckoService;
    private readonly SkinportService _skinportService;
    private readonly StockService _stockService;
    private readonly ImageCacheService _imageCacheService;
    
    public InvestmentService()
    {
        _context = new InvestmentDbContext();
        _coinGeckoService = new CoinGeckoService();
        _skinportService = new SkinportService();
        _stockService = new StockService();
        _imageCacheService = new ImageCacheService();
        
        // Asegurar que la base de datos existe
        _context.Database.EnsureCreated();
        
        // Migrar esquema para agregar campos Day y Week
        _context.MigrateDatabaseSchema();
    }
    
    /// <summary>
    /// Obtiene todas las inversiones
    /// </summary>
    public async Task<List<Investment>> GetAllInvestmentsAsync()
    {
        return await _context.Investments
            .OrderByDescending(i => i.PurchaseDate)
            .ToListAsync();
    }
    
    /// <summary>
    /// Agrega una nueva inversión
    /// </summary>
    public async Task<Investment> AddInvestmentAsync(Investment investment)
    {
        investment.LastPriceUpdate = DateTime.Now;
        
        // Actualizar precio e imagen según tipo de activo
        await UpdateAssetPriceAndImageAsync(investment, forceImageUpdate: false);
        
        _context.Investments.Add(investment);
        await _context.SaveChangesAsync();
        
        // Registrar precio inicial en historial
        await AddPriceHistoryAsync(investment);
        
        return investment;
    }
    
    /// <summary>
    /// Actualiza una inversión existente
    /// </summary>
    public async Task<Investment?> UpdateInvestmentAsync(Investment investment)
    {
        var existing = await _context.Investments.FindAsync(investment.Id);
        if (existing == null) return null;
        
        existing.Name = investment.Name;
        existing.AssetType = investment.AssetType;
        existing.Symbol = investment.Symbol;
        existing.Quantity = investment.Quantity;
        existing.PurchasePrice = investment.PurchasePrice;
        existing.CurrentPrice = investment.CurrentPrice;
        existing.PurchaseDate = investment.PurchaseDate;
        existing.Notes = investment.Notes;
        
        await _context.SaveChangesAsync();
        return existing;
    }
    
    /// <summary>
    /// Elimina una inversión
    /// </summary>
    public async Task<bool> DeleteInvestmentAsync(int id)
    {
        var investment = await _context.Investments.FindAsync(id);
        if (investment == null) return false;
        
        _context.Investments.Remove(investment);
        await _context.SaveChangesAsync();
        return true;
    }
    
    /// <summary>
    /// Actualiza los precios de todas las inversiones
    /// </summary>
    public async Task UpdateAllPricesAsync()
    {
        var investments = await _context.Investments.ToListAsync();
        
        foreach (var investment in investments)
        {
            try
            {
                await UpdateAssetPriceAndImageAsync(investment, forceImageUpdate: true);
                
                if (investment.CurrentPrice > 0)
                {
                    investment.LastPriceUpdate = DateTime.Now;
                    await AddPriceHistoryAsync(investment);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Error actualizando {investment.Name}: {ex.Message}");
            }
        }
        
        await _context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Actualiza el precio e imagen de un activo según su tipo
    /// </summary>
    private async Task UpdateAssetPriceAndImageAsync(Investment investment, bool forceImageUpdate)
    {
        if (investment.AssetType == AssetType.Criptomoneda && !string.IsNullOrEmpty(investment.Symbol))
        {
            await UpdateCryptoAssetAsync(investment, forceImageUpdate);
        }
        else if (investment.AssetType == AssetType.SkinCSGO && !string.IsNullOrEmpty(investment.Name))
        {
            await UpdateSkinAssetAsync(investment, forceImageUpdate);
        }
        else if (investment.AssetType == AssetType.Accion && !string.IsNullOrEmpty(investment.Symbol))
        {
            await UpdateStockAssetAsync(investment, forceImageUpdate);
        }
    }
    
    /// <summary>
    /// Actualiza precio e imagen de criptomoneda
    /// </summary>
    private async Task UpdateCryptoAssetAsync(Investment investment, bool forceImageUpdate)
    {
        if (string.IsNullOrEmpty(investment.Symbol))
        {
            Console.WriteLine($"⚠ Cripto sin símbolo: {investment.Name}");
            return;
        }
        
        Console.WriteLine($"Actualizando cripto: {investment.Name} ({investment.Symbol})");
        
        bool needsImage = string.IsNullOrEmpty(investment.ImageUrl);
        
        if (needsImage || forceImageUpdate)
        {
            var cryptoInfo = await _coinGeckoService.GetCryptoFullInfoAsync(investment.Symbol);
            if (cryptoInfo != null)
            {
                investment.CurrentPrice = cryptoInfo.CurrentPrice;
                investment.ImageUrl = cryptoInfo.ImageUrl;
                Console.WriteLine($"✓ Precio: ${cryptoInfo.CurrentPrice}, Imagen: {cryptoInfo.ImageUrl}");
            }
            else
            {
                Console.WriteLine($"⚠ No se pudo obtener info completa para {investment.Symbol}");
            }
        }
        else
        {
            var price = await _coinGeckoService.GetCryptoPriceAsync(investment.Symbol);
            if (price.HasValue)
            {
                investment.CurrentPrice = price.Value;
                Console.WriteLine($"✓ Precio actualizado: ${price.Value}");
            }
        }
    }
    
    /// <summary>
    /// Actualiza precio e imagen de skin CS:GO
    /// </summary>
    private async Task UpdateSkinAssetAsync(Investment investment, bool forceImageUpdate)
    {
        Console.WriteLine($"Actualizando skin CS:GO: {investment.Name}");
        
        bool needsImage = string.IsNullOrEmpty(investment.ImageUrl) || HasInvalidImageUrl(investment);
        
        if (needsImage)
        {
            if (HasInvalidImageUrl(investment))
            {
                Console.WriteLine($"  ⚠ Imagen inválida detectada, buscando nueva...");
            }
            else
            {
                Console.WriteLine($"  🔍 Skin sin imagen, buscando info completa...");
            }
            
            var skins = await _skinportService.SearchSkinsAsync(investment.Name);
            var skin = skins.FirstOrDefault(s => s.MarketHashName == investment.Name);
            
            if (skin != null)
            {
                investment.CurrentPrice = skin.MinPrice;
                investment.ImageUrl = skin.ImageUrl;
                Console.WriteLine($"✓ Precio: ${skin.MinPrice}, Imagen: {skin.ImageUrl}");
            }
            else
            {
                var price = await _skinportService.GetSkinPriceAsync(investment.Name);
                if (price.HasValue)
                {
                    investment.CurrentPrice = price.Value;
                    Console.WriteLine($"✓ Precio: ${price.Value}, ⚠ Sin imagen");
                }
            }
        }
        else
        {
            var price = await _skinportService.GetSkinPriceAsync(investment.Name);
            if (price.HasValue)
            {
                investment.CurrentPrice = price.Value;
                Console.WriteLine($"✓ Precio actualizado: ${price.Value}");
            }
        }
    }
    
    /// <summary>
    /// Actualiza precio e imagen de acción
    /// </summary>
    private async Task UpdateStockAssetAsync(Investment investment, bool forceImageUpdate)
    {
        if (string.IsNullOrEmpty(investment.Symbol))
        {
            Console.WriteLine($"⚠ Acción sin símbolo: {investment.Name}");
            return;
        }
        
        Console.WriteLine($"Actualizando acción: {investment.Name} ({investment.Symbol})");
        
        var price = await _stockService.GetStockPriceAsync(investment.Symbol);
        if (price.HasValue)
        {
            investment.CurrentPrice = price.Value;
            Console.WriteLine($"✓ Precio actualizado: ${price.Value}");
        }
        else
        {
            Console.WriteLine($"⚠ No se pudo obtener precio para {investment.Symbol}");
        }
        
        // Verificar si necesita descargar imagen
        bool needsImageDownload = string.IsNullOrEmpty(investment.ImageUrl) || 
                                   forceImageUpdate || 
                                   await NeedsImageRedownload(investment.ImageUrl);
        
        if (needsImageDownload)
        {
            var domain = GetDomainFromSymbol(investment.Symbol);
            var imageUrl = $"https://logo.clearbit.com/{domain}.com";
            
            Console.WriteLine($"📥 Descargando logo de acción: {imageUrl}");
            
            // Descargar y guardar la imagen en la base de datos usando ImageCacheService
            var imageData = await _imageCacheService.GetImageAsync(imageUrl);
            
            if (imageData != null && imageData.Length > 0)
            {
                investment.ImageUrl = imageUrl;
                Console.WriteLine($"✓ Logo descargado y guardado en DB: {imageUrl}");
            }
            else
            {
                // Si falla Clearbit, intentar con un logo genérico
                Console.WriteLine($"⚠ No se pudo descargar logo de {domain}, usando icono genérico");
                investment.ImageUrl = $"placeholder://stock/{investment.Symbol}";
            }
        }
    }
    
    /// <summary>
    /// Verifica si una imagen necesita ser re-descargada
    /// (por ejemplo, si la URL existe pero no está en caché)
    /// </summary>
    private async Task<bool> NeedsImageRedownload(string? imageUrl)
    {
        if (string.IsNullOrEmpty(imageUrl)) return true;

        if (IsPlaceholderImageUrl(imageUrl)) return false;

        // Solo consultar la caché, sin descargar ni tocar red
        return !await _imageCacheService.IsCachedAsync(imageUrl);
    }

    private static bool IsPlaceholderImageUrl(string imageUrl)
    {
        return imageUrl.StartsWith("placeholder://", StringComparison.OrdinalIgnoreCase)
               || imageUrl.Contains("via.placeholder.com", StringComparison.OrdinalIgnoreCase)
               || imageUrl.Contains("placeholder.com", StringComparison.OrdinalIgnoreCase);
    }
    
    /// <summary>
    /// Obtiene el dominio web de una empresa basado en su símbolo
    /// </summary>
    private string GetDomainFromSymbol(string symbol)
    {
        return symbol.ToUpper() switch
        {
            // Tech
            "AAPL" => "apple",
            "MSFT" => "microsoft",
            "GOOGL" or "GOOG" => "google",
            "AMZN" => "amazon",
            "META" or "FB" => "meta",
            "TSLA" => "tesla",
            "NVDA" => "nvidia",
            "NFLX" => "netflix",
            "AMD" => "amd",
            "INTC" => "intel",
            
            // ETFs - Fondos más populares
            "SPY" => "spdr",
            "QQQ" => "invesco",
            "VOO" => "vanguard",
            "VTI" => "vanguard",
            "IWM" => "ishares",
            "DIA" => "spdr",
            "VEA" => "vanguard",
            "VWO" => "vanguard",
            "AGG" => "ishares",
            "BND" => "vanguard",
            
            // Finance
            "JPM" => "jpmorganchase",
            "BAC" => "bankofamerica",
            "WFC" => "wellsfargo",
            "GS" => "goldmansachs",
            "V" => "visa",
            "MA" => "mastercard",
            
            // Consumer
            "KO" => "coca-cola",
            "PEP" => "pepsi",
            "WMT" => "walmart",
            "DIS" => "disney",
            "NKE" => "nike",
            "MCD" => "mcdonalds",
            
            _ => symbol.ToLower()
        };
    }
    
    /// <summary>
    /// Verifica si una URL de imagen es inválida
    /// </summary>
    private bool HasInvalidImageUrl(Investment investment)
    {
        if (string.IsNullOrEmpty(investment.ImageUrl)) return false;
        
        return investment.ImageUrl.Contains(investment.Name) || 
               investment.ImageUrl.Contains(" ") || 
               investment.ImageUrl.Contains("|");
    }
    
    /// <summary>
    /// Agrega un registro al historial de precios (diario, semanal y mensual)
    /// </summary>
    private async Task AddPriceHistoryAsync(Investment investment)
    {
        var now = DateTime.Now;
        var currentDay = now.ToString("yyyy-MM-dd");
        var currentWeek = GetIso8601WeekOfYear(now);
        var currentMonth = now.ToString("yyyy-MM");
        
        // Verificar si ya existe un registro para hoy
        var existingHistory = await _context.PriceHistories
            .FirstOrDefaultAsync(h => h.InvestmentId == investment.Id && h.Day == currentDay);
        
        if (existingHistory == null)
        {
            var history = new PriceHistory
            {
                InvestmentId = investment.Id,
                Price = investment.CurrentPrice,
                RecordedAt = now,
                Day = currentDay,
                Week = currentWeek,
                Month = currentMonth
            };
            
            _context.PriceHistories.Add(history);
            await _context.SaveChangesAsync();
        }
    }
    
    /// <summary>
    /// Calcula el número de semana ISO 8601
    /// </summary>
    private string GetIso8601WeekOfYear(DateTime date)
    {
        var day = (int)System.Globalization.CultureInfo.CurrentCulture.Calendar.GetDayOfWeek(date);
        var weekNum = System.Globalization.CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(
            date, 
            System.Globalization.CalendarWeekRule.FirstFourDayWeek, 
            DayOfWeek.Monday);
        return $"{date.Year}-W{weekNum:D2}";
    }
    
    /// <summary>
    /// Obtiene el historial de precios de una inversión
    /// </summary>
    public async Task<List<PriceHistory>> GetPriceHistoryAsync(int investmentId)
    {
        return await _context.PriceHistories
            .Where(h => h.InvestmentId == investmentId)
            .OrderBy(h => h.RecordedAt)
            .ToListAsync();
    }
    
    /// <summary>
    /// Obtiene el rendimiento total del portfolio
    /// </summary>
    public async Task<PortfolioSummary> GetPortfolioSummaryAsync()
    {
        var investments = await GetAllInvestmentsAsync();
        
        return new PortfolioSummary
        {
            TotalInvestments = investments.Count,
            TotalInvested = investments.Sum(i => i.TotalPurchaseValue),
            CurrentValue = investments.Sum(i => i.CurrentValue),
            TotalProfitLoss = investments.Sum(i => i.ProfitLoss),
            TotalProfitLossPercentage = investments.Sum(i => i.TotalPurchaseValue) > 0
                ? (investments.Sum(i => i.ProfitLoss) / investments.Sum(i => i.TotalPurchaseValue)) * 100
                : 0
        };
    }
    
    /// <summary>
    /// Obtiene el rendimiento por período (diario, semanal o mensual)
    /// </summary>
    public async Task<List<PerformanceData>> GetPerformanceByPeriodAsync(ChartPeriod period)
    {
        var now = DateTime.Now;
        DateTime startDate;
        
        // Determinar fecha de inicio según el período
        startDate = period switch
        {
            ChartPeriod.Daily => now.AddDays(-30),    // Últimos 30 días
            ChartPeriod.Weekly => now.AddDays(-84),   // Últimas 12 semanas
            ChartPeriod.Monthly => now.AddMonths(-12), // Últimos 12 meses
            _ => now.AddMonths(-12)
        };
        
        var histories = await _context.PriceHistories
            .Include(h => h.Investment)
            .Where(h => h.RecordedAt >= startDate)
            .OrderBy(h => h.RecordedAt)
            .ToListAsync();
        
        List<PerformanceData> data;
        
        switch (period)
        {
            case ChartPeriod.Daily:
                data = histories
                    .GroupBy(h => h.Day)
                    .Select(g => new PerformanceData
                    {
                        Period = g.Key,
                        Date = DateTime.Parse(g.Key),
                        TotalValue = g.Sum(h => h.Price * (h.Investment?.Quantity ?? 0))
                    })
                    .OrderBy(p => p.Date)
                    .ToList();
                break;
                
            case ChartPeriod.Weekly:
                data = histories
                    .GroupBy(h => h.Week)
                    .Select(g => new PerformanceData
                    {
                        Period = g.Key,
                        Date = g.Min(h => h.RecordedAt),
                        TotalValue = g.Sum(h => h.Price * (h.Investment?.Quantity ?? 0))
                    })
                    .OrderBy(p => p.Date)
                    .ToList();
                break;
                
            case ChartPeriod.Monthly:
            default:
                data = histories
                    .GroupBy(h => h.Month)
                    .Select(g => new PerformanceData
                    {
                        Period = g.Key,
                        Date = DateTime.Parse(g.Key + "-01"),
                        TotalValue = g.Sum(h => h.Price * (h.Investment?.Quantity ?? 0))
                    })
                    .OrderBy(p => p.Date)
                    .ToList();
                break;
        }
        
        return data;
    }
    
    /// <summary>
    /// Obtiene el rendimiento por mes (método legacy mantenido para compatibilidad)
    /// </summary>
    public async Task<List<MonthlyPerformance>> GetMonthlyPerformanceAsync()
    {
        var histories = await _context.PriceHistories
            .Include(h => h.Investment)
            .OrderBy(h => h.Month)
            .ToListAsync();
        
        var monthlyData = histories
            .GroupBy(h => h.Month)
            .Select(g => new MonthlyPerformance
            {
                Month = g.Key,
                TotalValue = g.Sum(h => h.Price * (h.Investment?.Quantity ?? 0))
            })
            .ToList();
        
        return monthlyData;
    }
}

public class PortfolioSummary
{
    public int TotalInvestments { get; set; }
    public decimal TotalInvested { get; set; }
    public decimal CurrentValue { get; set; }
    public decimal TotalProfitLoss { get; set; }
    public decimal TotalProfitLossPercentage { get; set; }
}

public class MonthlyPerformance
{
    public string Month { get; set; } = string.Empty;
    public decimal TotalValue { get; set; }
}

public class PerformanceData
{
    public string Period { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public decimal TotalValue { get; set; }
}

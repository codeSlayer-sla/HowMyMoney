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
    
    public InvestmentService()
    {
        _context = new InvestmentDbContext();
        _coinGeckoService = new CoinGeckoService();
        _skinportService = new SkinportService();
        
        // Asegurar que la base de datos existe
        _context.Database.EnsureCreated();
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
        
        // Si es criptomoneda o skin, obtener precio actual
        if (investment.AssetType == AssetType.Criptomoneda && !string.IsNullOrEmpty(investment.Symbol))
        {
            Console.WriteLine($"Agregando cripto: {investment.Name} ({investment.Symbol})");
            var price = await _coinGeckoService.GetCryptoPriceAsync(investment.Symbol);
            if (price.HasValue)
            {
                investment.CurrentPrice = price.Value;
                Console.WriteLine($"✓ Precio cripto obtenido: ${price.Value}");
            }
        }
        else if (investment.AssetType == AssetType.SkinCSGO && !string.IsNullOrEmpty(investment.Name))
        {
            Console.WriteLine($"Agregando skin CS:GO: {investment.Name}");
            var price = await _skinportService.GetSkinPriceAsync(investment.Name);
            if (price.HasValue)
            {
                investment.CurrentPrice = price.Value;
                Console.WriteLine($"✓ Precio CS:GO obtenido: ${price.Value}");
            }
            else
            {
                Console.WriteLine($"⚠ No se pudo obtener precio para skin CS:GO: {investment.Name}");
                Console.WriteLine($"⚠ Usando CurrentPrice del formulario: ${investment.CurrentPrice}");
            }
        }
        
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
                decimal? newPrice = null;
                
                if (investment.AssetType == AssetType.Criptomoneda && !string.IsNullOrEmpty(investment.Symbol))
                {
                    Console.WriteLine($"Actualizando cripto: {investment.Name} ({investment.Symbol})");
                    newPrice = await _coinGeckoService.GetCryptoPriceAsync(investment.Symbol);
                }
                else if (investment.AssetType == AssetType.SkinCSGO && !string.IsNullOrEmpty(investment.Name))
                {
                    Console.WriteLine($"Actualizando skin CS:GO: {investment.Name}");
                    newPrice = await _skinportService.GetSkinPriceAsync(investment.Name);
                    
                    if (newPrice.HasValue)
                    {
                        Console.WriteLine($"✓ Precio CS:GO actualizado: {investment.Name} = ${newPrice.Value}");
                    }
                    else
                    {
                        Console.WriteLine($"⚠ No se pudo obtener precio para skin CS:GO: {investment.Name}");
                    }
                }
                
                if (newPrice.HasValue && newPrice.Value > 0)
                {
                    investment.CurrentPrice = newPrice.Value;
                    investment.LastPriceUpdate = DateTime.Now;
                    
                    // Agregar al historial si es un nuevo mes
                    await AddPriceHistoryAsync(investment);
                }
                else if (investment.AssetType != AssetType.Manual)
                {
                    Console.WriteLine($"⚠ No se actualizó precio para {investment.Name} - newPrice = {newPrice}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error actualizando precio de {investment.Name}: {ex.Message}");
            }
        }
        
        await _context.SaveChangesAsync();
    }
    
    /// <summary>
    /// Agrega un registro al historial de precios
    /// </summary>
    private async Task AddPriceHistoryAsync(Investment investment)
    {
        var currentMonth = DateTime.Now.ToString("yyyy-MM");
        
        // Verificar si ya existe un registro para este mes
        var existingHistory = await _context.PriceHistories
            .FirstOrDefaultAsync(h => h.InvestmentId == investment.Id && h.Month == currentMonth);
        
        if (existingHistory == null)
        {
            var history = new PriceHistory
            {
                InvestmentId = investment.Id,
                Price = investment.CurrentPrice,
                RecordedAt = DateTime.Now,
                Month = currentMonth
            };
            
            _context.PriceHistories.Add(history);
            await _context.SaveChangesAsync();
        }
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
    /// Obtiene el rendimiento por mes
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

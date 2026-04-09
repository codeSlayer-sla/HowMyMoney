using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HowsMyMoney.Models;
using HowsMyMoney.Services;

namespace HowsMyMoney.ViewModels;

public partial class MarketMonitorViewModel : ViewModelBase
{
    private readonly CoinGeckoService _coinGeckoService;
    private readonly StockService _stockService;
    private CancellationTokenSource? _updateCancellationTokenSource;
    
    [ObservableProperty]
    private ObservableCollection<MarketData> _cryptoMarkets = new();
    
    [ObservableProperty]
    private ObservableCollection<MarketData> _stockMarkets = new();
    
    [ObservableProperty]
    private bool _isCryptoEnabled = true;
    
    [ObservableProperty]
    private bool _isStocksEnabled = false;
    
    [ObservableProperty]
    private bool _isSkinsEnabled = false;
    
    [ObservableProperty]
    private bool _isLoading = false;
    
    [ObservableProperty]
    private bool _isLiveUpdateActive = false;
    
    [ObservableProperty]
    private DateTime _lastUpdate = DateTime.Now;
    
    [ObservableProperty]
    private int _updateInterval = 30; // segundos
    
    [ObservableProperty]
    private bool _isFullscreenMode = false;
    
    [ObservableProperty]
    private MarketData? _selectedMarket;
    
    private DateTime _lastRefresh = DateTime.MinValue;
    private static readonly TimeSpan MinRefreshInterval = TimeSpan.FromSeconds(30);

    public MarketMonitorViewModel()
    {
        _coinGeckoService = new CoinGeckoService();
        _stockService = new StockService();
    }

    public async Task LoadMarkets()
    {
        IsLoading = true;
        
        try
        {
            var tasks = new List<Task>();
            
            if (IsCryptoEnabled)
            {
                tasks.Add(LoadCryptoMarketsAsync());
            }
            
            if (IsStocksEnabled)
            {
                tasks.Add(LoadStockMarketsAsync());
            }
            
            await Task.WhenAll(tasks);
            
            LastUpdate = DateTime.Now;
            Console.WriteLine($"✓ Mercados actualizados: {DateTime.Now:HH:mm:ss}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error cargando mercados: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RefreshMarkets()
    {
        var timeSinceLastRefresh = DateTime.Now - _lastRefresh;
        if (timeSinceLastRefresh < MinRefreshInterval)
        {
            var remaining = MinRefreshInterval - timeSinceLastRefresh;
            Console.WriteLine($"⏱ Espera {remaining.TotalSeconds:F0}s antes de refrescar nuevamente");
            return;
        }
        
        _lastRefresh = DateTime.Now;
        await LoadMarkets();
    }
    
    private async Task LoadCryptoMarketsAsync()
    {
        try
        {
            Console.WriteLine("📊 Cargando Top 10 Criptomonedas...");
            
            // Usar el endpoint batch para obtener todas las cryptos en una sola petición
            var topCryptos = await _coinGeckoService.GetTopCryptosAsync(10);
            
            Console.WriteLine($"📥 Recibidas {topCryptos.Count} cryptos del API");
            
            var cryptoData = topCryptos.Select(info => new MarketData
            {
                Symbol = info.Symbol.ToUpper(),
                Name = info.Name,
                Price = info.CurrentPrice,
                Change24h = info.PriceChange24h,
                ChangePercent24h = info.PriceChangePercentage24h,
                ImageUrl = info.ImageUrl ?? string.Empty,
                MarketType = MarketType.Crypto
            }).ToList();
            
            CryptoMarkets = new ObservableCollection<MarketData>(cryptoData);
            Console.WriteLine($"✓ {CryptoMarkets.Count} criptomonedas cargadas en CryptoMarkets");
            
            // Debug: imprimir las primeras 3
            foreach (var crypto in CryptoMarkets.Take(3))
            {
                Console.WriteLine($"  • {crypto.Name} ({crypto.Symbol}): ${crypto.Price:F2}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error cargando cryptos: {ex.Message}");
        }
    }
    
    private async Task LoadStockMarketsAsync()
    {
        try
        {
            Console.WriteLine("📈 Cargando Top 10 Acciones...");
            
            // Top 10 stocks más populares
            var topStocks = new[] 
            { 
                ("AAPL", "Apple Inc."),
                ("MSFT", "Microsoft Corporation"),
                ("GOOGL", "Alphabet Inc."),
                ("AMZN", "Amazon.com Inc."),
                ("NVDA", "NVIDIA Corporation"),
                ("TSLA", "Tesla Inc."),
                ("META", "Meta Platforms Inc."),
                ("BRK-B", "Berkshire Hathaway"),
                ("V", "Visa Inc."),
                ("JPM", "JPMorgan Chase")
            };
            
            var stockData = new List<MarketData>();
            
            foreach (var (symbol, name) in topStocks)
            {
                var price = await _stockService.GetStockPriceAsync(symbol);
                if (price.HasValue)
                {
                    stockData.Add(new MarketData
                    {
                        Symbol = symbol,
                        Name = name,
                        Price = price.Value,
                        Change24h = 0, // Yahoo Finance API básica no proporciona esto fácilmente
                        ChangePercent24h = 0,
                        ImageUrl = $"https://logo.clearbit.com/{GetDomain(symbol)}.com",
                        MarketType = MarketType.Stocks
                    });
                }
            }
            
            StockMarkets = new ObservableCollection<MarketData>(stockData);
            Console.WriteLine($"✓ {StockMarkets.Count} acciones cargadas");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error cargando stocks: {ex.Message}");
        }
    }
    
    [RelayCommand]
    private async Task ToggleLiveUpdate()
    {
        if (IsLiveUpdateActive)
        {
            StopLiveUpdate();
        }
        else
        {
            await StartLiveUpdate();
        }
    }
    
    private async Task StartLiveUpdate()
    {
        IsLiveUpdateActive = true;
        _updateCancellationTokenSource = new CancellationTokenSource();
        
        Console.WriteLine($"🔴 Monitoreo en vivo iniciado (actualiza cada {UpdateInterval}s)");
        
        try
        {
            while (!_updateCancellationTokenSource.Token.IsCancellationRequested)
            {
                await LoadMarkets();
                await Task.Delay(TimeSpan.FromSeconds(UpdateInterval), _updateCancellationTokenSource.Token);
            }
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("⏸ Monitoreo en vivo detenido");
        }
    }
    
    private void StopLiveUpdate()
    {
        IsLiveUpdateActive = false;
        _updateCancellationTokenSource?.Cancel();
        _updateCancellationTokenSource?.Dispose();
        _updateCancellationTokenSource = null;
        
        Console.WriteLine("⏸ Monitoreo en vivo detenido");
    }
    
    [RelayCommand]
    private void ToggleCrypto()
    {
        IsCryptoEnabled = !IsCryptoEnabled;
        _ = LoadMarkets();
    }
    
    [RelayCommand]
    private void ToggleStocks()
    {
        IsStocksEnabled = !IsStocksEnabled;
        _ = LoadMarkets();
    }
    
    [RelayCommand]
    private void ToggleSkins()
    {
        IsSkinsEnabled = !IsSkinsEnabled;
        Console.WriteLine($"🔫 CS:GO Skins panel: {(IsSkinsEnabled ? "Abierto" : "Cerrado")}");
    }
    
    [RelayCommand]
    private void OpenFullscreen(MarketData market)
    {
        SelectedMarket = market;
        IsFullscreenMode = true;
        Console.WriteLine($"🔍 Fullscreen: {market.Name}");
    }
    
    [RelayCommand]
    private void CloseFullscreen()
    {
        IsFullscreenMode = false;
        SelectedMarket = null;
    }
    
    private string GetDomain(string symbol)
    {
        return symbol.ToUpper() switch
        {
            "AAPL" => "apple",
            "MSFT" => "microsoft",
            "GOOGL" or "GOOG" => "google",
            "AMZN" => "amazon",
            "META" => "meta",
            "TSLA" => "tesla",
            "NVDA" => "nvidia",
            "BRK-B" => "berkshirehathaway",
            "V" => "visa",
            "JPM" => "jpmorganchase",
            _ => symbol.ToLower()
        };
    }
    
    public void Cleanup()
    {
        StopLiveUpdate();
    }
}

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
    private readonly ICoinGeckoService _coinGeckoService;
    private readonly IStockService _stockService;
    private readonly ILisSkinsService _lisSkinsService;
    private readonly ISteamIconService _steamIconService;
    private readonly IImageCacheService _imageCacheService;
    private CancellationTokenSource? _updateCancellationTokenSource;

    [ObservableProperty]
    private ObservableCollection<MarketDataViewModel> _cryptoMarkets = new();

    [ObservableProperty]
    private ObservableCollection<MarketDataViewModel> _stockMarkets = new();

    [ObservableProperty]
    private ObservableCollection<MarketDataViewModel> _skinMarkets = new();

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
    private MarketDataViewModel? _selectedMarket;

    private DateTime _lastRefresh = DateTime.MinValue;
    private static readonly TimeSpan MinRefreshInterval = TimeSpan.FromSeconds(30);

    public MarketMonitorViewModel(
        ICoinGeckoService coinGeckoService,
        IStockService stockService,
        ILisSkinsService lisSkinsService,
        ISteamIconService steamIconService,
        IImageCacheService imageCacheService)
    {
        _coinGeckoService = coinGeckoService;
        _stockService = stockService;
        _lisSkinsService = lisSkinsService;
        _steamIconService = steamIconService;
        _imageCacheService = imageCacheService;
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

            if (IsSkinsEnabled)
            {
                tasks.Add(LoadSkinMarketsAsync());
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
            
            var cryptoData = topCryptos.Select(info => new MarketDataViewModel(new MarketData
            {
                Symbol = info.Symbol.ToUpper(),
                Name = info.Name,
                Price = info.CurrentPrice,
                Change24h = info.PriceChange24h,
                ChangePercent24h = info.PriceChangePercentage24h,
                ImageUrl = info.ImageUrl ?? string.Empty,
                MarketType = MarketType.Crypto,
                MarketCap = info.MarketCap
            }, _imageCacheService)).ToList();

            CryptoMarkets = new ObservableCollection<MarketDataViewModel>(cryptoData);
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
            
            // Pedir las 10 cotizaciones en paralelo en vez de una por una
            var stockTasks = topStocks.Select(async stock =>
            {
                var (symbol, name) = stock;
                var quote = await _stockService.GetStockQuoteAsync(symbol);
                return quote != null
                    ? new MarketDataViewModel(new MarketData
                    {
                        Symbol = symbol,
                        Name = name,
                        Price = quote.Price,
                        Change24h = quote.Change,
                        ChangePercent24h = quote.ChangePercent,
                        ImageUrl = StockLogoHelper.GetLogoUrl(symbol),
                        MarketType = MarketType.Stocks
                        // MarketCap: no disponible en el endpoint básico de Yahoo que usamos.
                    }, _imageCacheService)
                    : null;
            });

            var stockResults = await Task.WhenAll(stockTasks);
            var stockData = stockResults.Where(s => s != null).Cast<MarketDataViewModel>().ToList();

            StockMarkets = new ObservableCollection<MarketDataViewModel>(stockData);
            Console.WriteLine($"✓ {StockMarkets.Count} acciones cargadas");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error cargando stocks: {ex.Message}");
        }
    }

    private async Task LoadSkinMarketsAsync()
    {
        try
        {
            Console.WriteLine("🔫 Cargando Top Skins CS:GO...");

            var topSkins = await _lisSkinsService.GetTopSkinsAsync(10);

            // Solo 10 íconos, una vez por refresh, en paralelo: volumen bajo, no es
            // lo mismo que resolver íconos por cada resultado de una búsqueda (eso sí
            // podía ser 50 a la vez, por eso ahí se evita).
            var skinIconTasks = topSkins.Select(skin => _steamIconService.GetIconUrlAsync(skin.MarketHashName));
            var skinIcons = await Task.WhenAll(skinIconTasks);

            var skinData = topSkins.Zip(skinIcons, (skin, iconUrl) => new MarketDataViewModel(new MarketData
            {
                Symbol = "CS:GO",
                Name = skin.MarketHashName,
                Price = skin.MinPrice,
                Change24h = 0,
                ChangePercent24h = 0,
                ImageUrl = iconUrl ?? string.Empty,
                MarketType = MarketType.Skins,
                // Proxy de "tamaño de mercado" (no hay market cap real para skins):
                // precio × unidades listadas en LIS-Skins.
                MarketCap = skin.MinPrice * skin.ListedCount
            }, _imageCacheService)).ToList();

            SkinMarkets = new ObservableCollection<MarketDataViewModel>(skinData);
            Console.WriteLine($"✓ {SkinMarkets.Count} skins cargadas");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error cargando skins: {ex.Message}");
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
        _ = LoadMarkets();
    }
    
    [RelayCommand]
    private void OpenFullscreen(MarketDataViewModel market)
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
    
    public void Cleanup()
    {
        StopLiveUpdate();
    }
}

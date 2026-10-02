using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using CommunityToolkit.Mvvm.ComponentModel;
using HowsMyMoney.Models;
using HowsMyMoney.Services;

namespace HowsMyMoney.ViewModels;

/// <summary>
/// ViewModel wrapper para MarketData con imagen cargada asíncronamente.
/// Antes los paneles de Monitoreo bindeaban el ícono directo contra
/// UrlToBitmapConverter (que descarga de forma SÍNCRONA, bloqueando la UI
/// por cada fila) — con 10-30 íconos abriéndose de golpe eso congelaba la app.
/// Este wrapper replica el mismo patrón ya usado en InvestmentViewModel:
/// la imagen se resuelve en background y la UI solo espera el Bitmap final.
/// </summary>
public partial class MarketDataViewModel : ObservableObject
{
    private static readonly SemaphoreSlim _imageSemaphore = new(5, 5); // Máximo 5 descargas concurrentes

    private readonly IImageCacheService _imageCache;

    [ObservableProperty]
    private MarketData _market;

    [ObservableProperty]
    private Bitmap? _imageBitmap;

    public string Symbol => Market.Symbol;
    public string Name => Market.Name;
    public decimal Price => Market.Price;
    public decimal ChangePercent24h => Market.ChangePercent24h;

    /// <summary>
    /// Market cap (o el proxy de tamaño de mercado, para skins) formateado con
    /// sufijo K/M/B/T, ej. "$1.71T". Null si no hay dato (acciones, por ahora).
    /// </summary>
    public string? MarketCapDisplay
    {
        get
        {
            if (Market.MarketCap is not decimal cap)
            {
                return null;
            }

            return cap switch
            {
                >= 1_000_000_000_000m => $"${cap / 1_000_000_000_000m:F2}T",
                >= 1_000_000_000m => $"${cap / 1_000_000_000m:F2}B",
                >= 1_000_000m => $"${cap / 1_000_000m:F2}M",
                >= 1_000m => $"${cap / 1_000m:F1}K",
                _ => $"${cap:F0}"
            };
        }
    }

    public MarketDataViewModel(MarketData market, IImageCacheService imageCache)
    {
        _market = market;
        _imageCache = imageCache;

        _ = LoadImageAsync();
    }

    private async Task LoadImageAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(Market.ImageUrl))
            {
                return;
            }

            await _imageSemaphore.WaitAsync();

            try
            {
                var imageData = await _imageCache.GetImageAsync(Market.ImageUrl);

                if (imageData != null && imageData.Length > 0)
                {
                    using var stream = new MemoryStream(imageData);
                    ImageBitmap = new Bitmap(stream);
                }
            }
            finally
            {
                _imageSemaphore.Release();
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"⚠ Error cargando imagen para {Market.Name}: {ex.Message}");
        }
    }
}

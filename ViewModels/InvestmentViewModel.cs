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
/// ViewModel wrapper para Investment con imagen cargada asíncronamente
/// </summary>
public partial class InvestmentViewModel : ObservableObject
{
    private static readonly ImageCacheService _imageCache = new();
    private static readonly SemaphoreSlim _imageSemaphore = new(5, 5); // Máximo 5 descargas concurrentes

    [ObservableProperty]
    private Investment _investment;

    [ObservableProperty]
    private Bitmap? _imageBitmap;

    [ObservableProperty]
    private bool _isLoadingImage = true;

    public InvestmentViewModel(Investment investment)
    {
        _investment = investment;
        
        // Cargar imagen asíncronamente sin bloquear
        _ = LoadImageAsync();
    }

    private async Task LoadImageAsync()
    {
        try
        {
            if (string.IsNullOrEmpty(Investment.ImageUrl))
            {
                IsLoadingImage = false;
                return;
            }

            // Limitar descargas concurrentes para evitar sobrecarga
            await _imageSemaphore.WaitAsync();
            
            try
            {
                // Cargar imagen desde caché o descargar en background
                var imageData = await _imageCache.GetImageAsync(Investment.ImageUrl);

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
            Console.WriteLine($"⚠ Error cargando imagen para {Investment.Name}: {ex.Message}");
        }
        finally
        {
            IsLoadingImage = false;
        }
    }
}

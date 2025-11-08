using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using HowsMyMoney.Models;
using HowsMyMoney.Services;

namespace HowsMyMoney.ViewModels;

public partial class AddInvestmentViewModel : ViewModelBase
{
    private readonly InvestmentService _investmentService;
    private readonly CoinGeckoService _coinGeckoService;
    private readonly SkinportService _skinportService;
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    private AssetType _selectedAssetType = AssetType.Criptomoneda;
    
    [ObservableProperty]
    private string _symbol = string.Empty;
    
    // Se ejecuta cuando cambia el tipo de activo
    partial void OnSelectedAssetTypeChanged(AssetType value)
    {
        // Limpiar resultados de búsqueda cuando cambia el tipo
        SearchResults.Clear();
        StatusMessage = $"Tipo de activo cambiado a: {value}";
        Console.WriteLine($"DEBUG: Tipo de activo cambiado a {value}");
    }
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPurchaseValue))]
    private decimal _quantity = 1;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(TotalPurchaseValue))]
    private decimal _purchasePrice;
    
    [ObservableProperty]
    private decimal _currentPrice;
    
    [ObservableProperty]
    private DateTime _purchaseDate = DateTime.Today;
    
    [ObservableProperty]
    private string _notes = string.Empty;
    
    [ObservableProperty]
    private string? _imageUrl;
    
    [ObservableProperty]
    private string _statusMessage = string.Empty;
    
    public decimal TotalPurchaseValue => Quantity * PurchasePrice;
    
    [ObservableProperty]
    private bool _isSearching;
    
    [ObservableProperty]
    private List<AssetSearchResult> _searchResults = new();
    
    [ObservableProperty]
    private AssetSearchResult? _selectedSearchResult;
    
    public List<AssetType> AssetTypes { get; } = Enum.GetValues<AssetType>().ToList();
    
    public AddInvestmentViewModel()
    {
        _investmentService = new InvestmentService();
        _coinGeckoService = new CoinGeckoService();
        _skinportService = new SkinportService();
    }
    
    [RelayCommand]
    private async Task SearchAssetAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "Ingrese un nombre para buscar";
            return;
        }
        
        IsSearching = true;
        SearchResults.Clear();
        
        try
        {
            // Debug: Mostrar qué tipo se está buscando
            StatusMessage = $"Buscando {SelectedAssetType}: {Name}...";
            Console.WriteLine($"DEBUG: Tipo seleccionado = {SelectedAssetType}");
            Console.WriteLine($"DEBUG: Nombre a buscar = {Name}");
            
            if (SelectedAssetType == AssetType.Criptomoneda)
            {
                Console.WriteLine("DEBUG: Entrando a búsqueda de criptomonedas");
                var coins = await _coinGeckoService.SearchCoinsAsync(Name);
                
                // Obtener precios para cada moneda
                var searchResults = new List<AssetSearchResult>();
                foreach (var coin in coins.Take(10)) // Limitar a 10 resultados
                {
                    var price = await _coinGeckoService.GetCryptoPriceAsync(coin.Id);
                    searchResults.Add(new AssetSearchResult
                    {
                        Name = coin.Name,
                        Symbol = coin.Id, // Usamos el ID para después obtener el precio
                        ImageUrl = coin.Large ?? coin.Thumb,
                        CurrentPrice = price ?? 0,
                        AssetType = AssetType.Criptomoneda
                    });
                }
                
                SearchResults = searchResults;
                
                if (searchResults.Any())
                {
                    StatusMessage = $"✓ Se encontraron {searchResults.Count} criptomonedas. Selecciona una de la lista.";
                }
                else
                {
                    StatusMessage = "No se encontraron criptomonedas con ese nombre";
                }
            }
            else if (SelectedAssetType == AssetType.SkinCSGO)
            {
                Console.WriteLine("DEBUG: Entrando a búsqueda de skins CS:GO");
                var skins = await _skinportService.SearchSkinsAsync(Name);
                Console.WriteLine($"DEBUG: Se encontraron {skins.Count} skins");
                
                // Convertir a AssetSearchResult con imágenes de Steam
                SearchResults = skins.Select(s => new AssetSearchResult
                {
                    Name = s.MarketHashName,
                    Symbol = s.MarketHashName,
                    ImageUrl = s.ImageUrl,
                    CurrentPrice = s.MinPrice,
                    AssetType = AssetType.SkinCSGO
                }).ToList();
                
                if (skins.Any())
                {
                    StatusMessage = $"✓ Se encontraron {skins.Count} skins CS:GO ordenadas por precio más bajo. Selecciona una.";
                }
                else
                {
                    StatusMessage = "No se encontraron skins con ese nombre";
                }
            }
            else if (SelectedAssetType == AssetType.Manual)
            {
                Console.WriteLine("DEBUG: Activo manual seleccionado");
                StatusMessage = "Los activos manuales no requieren búsqueda. Ingresa los datos directamente.";
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"ERROR en búsqueda: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            StatusMessage = $"Error en búsqueda: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }
    
    [RelayCommand]
    private void SelectSearchResult(AssetSearchResult? selectedResult)
    {
        if (selectedResult == null)
        {
            return;
        }
        
        IsSearching = true;
        StatusMessage = "Cargando información...";
        
        try
        {
            Name = selectedResult.Name;
            Symbol = selectedResult.Symbol;
            CurrentPrice = selectedResult.CurrentPrice;
            ImageUrl = selectedResult.ImageUrl; // Guardar la URL de imagen
            
            if (PurchasePrice == 0)
            {
                PurchasePrice = selectedResult.CurrentPrice;
            }
            
            StatusMessage = $"✓ {selectedResult.Name} seleccionado - Precio: ${selectedResult.CurrentPrice:N2}";
            
            // Limpiar búsqueda
            Console.WriteLine($"🧹 LIMPIANDO búsqueda...");
            SearchResults.Clear();
            SelectedSearchResult = null;
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error al seleccionar: {ex.Message}";
        }
        finally
        {
            IsSearching = false;
        }
    }
    
    [RelayCommand]
    private async Task SaveInvestmentAsync()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            StatusMessage = "El nombre es requerido";
            return;
        }
        
        if (Quantity <= 0)
        {
            StatusMessage = "La cantidad debe ser mayor a 0";
            return;
        }
        
        if (PurchasePrice <= 0)
        {
            StatusMessage = "El precio de compra debe ser mayor a 0";
            return;
        }
        
        try
        {
            var investment = new Investment
            {
                Name = Name,
                AssetType = SelectedAssetType,
                Symbol = Symbol,
                Quantity = Quantity,
                PurchasePrice = PurchasePrice,
                CurrentPrice = CurrentPrice > 0 ? CurrentPrice : PurchasePrice,
                PurchaseDate = PurchaseDate,
                Notes = Notes,
                ImageUrl = ImageUrl  // Guardar la URL de la imagen
            };
            
            await _investmentService.AddInvestmentAsync(investment);
            StatusMessage = "Inversión guardada correctamente";
            
            // Limpiar formulario
            ClearForm();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error guardando inversión: {ex.Message}";
        }
    }
    
    [RelayCommand]
    private void ClearForm()
    {
        Name = string.Empty;
        Symbol = string.Empty;
        Quantity = 1;
        PurchasePrice = 0;
        CurrentPrice = 0;
        PurchaseDate = DateTime.Today;
        Notes = string.Empty;
        ImageUrl = null;
        SearchResults.Clear();
        StatusMessage = string.Empty;
    }
}

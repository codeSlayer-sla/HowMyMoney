using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    private readonly StockService _stockService;
    
    [ObservableProperty]
    private string _name = string.Empty;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSkinAsset), nameof(IsNotSkinAsset))]
    private AssetType _selectedAssetType = AssetType.Criptomoneda;
    
    [ObservableProperty]
    private string _symbol = string.Empty;
    
    [ObservableProperty]
    private SkinCategory _selectedSkinCategory = SkinCategory.Todos;
    
    // Propiedades computadas para visibilidad de controles
    public bool IsSkinAsset => SelectedAssetType == AssetType.SkinCSGO;
    public bool IsNotSkinAsset => SelectedAssetType != AssetType.SkinCSGO;
    
    public List<SkinCategory> SkinCategories { get; } = Enum.GetValues<SkinCategory>().ToList();
    
    // Se ejecuta cuando cambia el tipo de activo
    partial void OnSelectedAssetTypeChanged(AssetType value)
    {
        // Limpiar resultados de búsqueda cuando cambia el tipo
        SearchResults.Clear();
        StatusMessage = $"Tipo de activo cambiado a: {value}";
        Console.WriteLine($"DEBUG: Tipo de activo cambiado a {value}");
        
        // Ajustar cantidad a entero si es skin
        if (value == AssetType.SkinCSGO && Quantity != Math.Floor(Quantity))
        {
            Quantity = Math.Floor(Quantity);
        }
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
    
    public ObservableCollection<AssetSearchResult> SearchResults { get; } = new();
    
    [ObservableProperty]
    private AssetSearchResult? _selectedSearchResult;
    
    public List<AssetType> AssetTypes { get; } = Enum.GetValues<AssetType>().ToList();
    
    public AddInvestmentViewModel()
    {
        _investmentService = new InvestmentService();
        _coinGeckoService = new CoinGeckoService();
        _skinportService = new SkinportService();
        _stockService = new StockService();
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
                
                // Limpiar resultados anteriores
                SearchResults.Clear();
                
                // OPTIMIZACIÓN: Obtener precios en lotes pequeños para evitar rate limiting
                var coinsToProcess = coins.Take(15).ToList(); // Reducido a 15 para evitar rate limiting
                var results = new List<AssetSearchResult>();
                
                // Procesar en lotes de 5 para no saturar la API
                for (int i = 0; i < coinsToProcess.Count; i += 5)
                {
                    var batch = coinsToProcess.Skip(i).Take(5);
                    var batchTasks = batch.Select(async coin =>
                    {
                        var price = await _coinGeckoService.GetCryptoPriceAsync(coin.Id);
                        return new AssetSearchResult
                        {
                            Name = coin.Name,
                            Symbol = coin.Id,
                            ImageUrl = coin.Large ?? coin.Thumb,
                            CurrentPrice = price ?? 0,
                            AssetType = AssetType.Criptomoneda
                        };
                    });
                    
                    var batchResults = await Task.WhenAll(batchTasks);
                    results.AddRange(batchResults);
                    
                    // Pequeña pausa entre lotes
                    if (i + 5 < coinsToProcess.Count)
                        await Task.Delay(200);
                }
                
                foreach (var result in results)
                {
                    SearchResults.Add(result);
                }
                
                if (SearchResults.Any())
                {
                    StatusMessage = $"✓ Se encontraron {SearchResults.Count} criptomonedas. Selecciona una de la lista.";
                }
                else
                {
                    StatusMessage = "No se encontraron criptomonedas con ese nombre";
                }
            }
            else if (SelectedAssetType == AssetType.SkinCSGO)
            {
                Console.WriteLine($"DEBUG: Entrando a búsqueda de skins CS:GO - Categoría: {SelectedSkinCategory}");
                var skins = await _skinportService.SearchSkinsAsync(Name, SelectedSkinCategory);
                Console.WriteLine($"DEBUG: Se encontraron {skins.Count} skins");
                
                // Limpiar resultados anteriores
                SearchResults.Clear();
                
                // Convertir a AssetSearchResult con imágenes de Steam
                foreach (var skin in skins)
                {
                    SearchResults.Add(new AssetSearchResult
                    {
                        Name = skin.MarketHashName,
                        Symbol = skin.MarketHashName,
                        ImageUrl = skin.ImageUrl,
                        CurrentPrice = skin.MinPrice,
                        AssetType = AssetType.SkinCSGO
                    });
                }
                
                if (skins.Any())
                {
                    StatusMessage = $"✓ Se encontraron {skins.Count} skins CS:GO ordenadas por precio más bajo. Selecciona una.";
                }
                else
                {
                    StatusMessage = "No se encontraron skins con ese nombre";
                }
            }
            else if (SelectedAssetType == AssetType.Accion)
            {
                Console.WriteLine("DEBUG: Entrando a búsqueda de acciones");
                var stocks = await _stockService.SearchStocksAsync(Name);
                Console.WriteLine($"DEBUG: Se encontraron {stocks.Count} acciones");
                
                // Limpiar resultados anteriores
                SearchResults.Clear();
                
                // Agregar los resultados de acciones
                foreach (var stock in stocks)
                {
                    SearchResults.Add(stock);
                }
                
                if (stocks.Any())
                {
                    StatusMessage = $"✓ Se encontraron {stocks.Count} acciones. Selecciona una de la lista.";
                }
                else
                {
                    StatusMessage = "No se encontraron acciones con ese nombre o símbolo";
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

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HowsMyMoney.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentView;
    
    [ObservableProperty]
    private bool _showingSplash = true;
    
    public SplashViewModel SplashViewModel { get; }
    public DashboardViewModel DashboardViewModel { get; }
    public AddInvestmentViewModel AddInvestmentViewModel { get; }
    
    public MainWindowViewModel()
    {
        SplashViewModel = new SplashViewModel();
        DashboardViewModel = new DashboardViewModel();
        AddInvestmentViewModel = new AddInvestmentViewModel();
        
        // Vista inicial: Splash Screen
        _currentView = SplashViewModel;
        
        // Iniciar animación de splash
        _ = InitializeAsync();
    }
    
    /// <summary>
    /// Inicializa la aplicación con splash screen
    /// </summary>
    private async Task InitializeAsync()
    {
        try
        {
            Console.WriteLine("🚀 MainWindowViewModel: Iniciando aplicación...");
            
            // Ejecutar animación de splash
            await SplashViewModel.AnimateAsync();
            
            // Transición al dashboard
            Console.WriteLine("🔄 Transicionando al dashboard...");
            ShowingSplash = false;
            CurrentView = DashboardViewModel;
            
            Console.WriteLine("✓ Aplicación inicializada correctamente");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error inicializando aplicación: {ex.Message}");
            // En caso de error, mostrar dashboard de todos modos
            ShowingSplash = false;
            CurrentView = DashboardViewModel;
        }
    }
    
    [RelayCommand]
    private void ShowDashboard()
    {
        CurrentView = DashboardViewModel;
    }
    
    [RelayCommand]
    private void ShowAddInvestment()
    {
        CurrentView = AddInvestmentViewModel;
    }
}

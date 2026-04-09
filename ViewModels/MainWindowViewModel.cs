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
    
    [ObservableProperty]
    private bool _showingLogin = true;
    
    [ObservableProperty]
    private bool _isAuthenticated = false;
    
    [ObservableProperty]
    private bool _isSidebarCollapsed = false;
    
    [ObservableProperty]
    private double _loginOpacity = 1.0;
    
    [ObservableProperty]
    private double _dashboardOpacity = 0.0;
    
    public LoginViewModel LoginViewModel { get; }
    public SplashViewModel SplashViewModel { get; }
    public DashboardViewModel DashboardViewModel { get; }
    public AddInvestmentViewModel AddInvestmentViewModel { get; }
    public MarketMonitorViewModel MarketMonitorViewModel { get; }
    
    public MainWindowViewModel()
    {
        LoginViewModel = new LoginViewModel(OnLoginSuccess);
        SplashViewModel = new SplashViewModel();
        DashboardViewModel = new DashboardViewModel();
        AddInvestmentViewModel = new AddInvestmentViewModel();
        MarketMonitorViewModel = new MarketMonitorViewModel();
        
        // Vista inicial: Login
        _currentView = LoginViewModel;
        Console.WriteLine("🔐 Mostrando pantalla de login");
    }
    
    private async void OnLoginSuccess(bool success)
    {
        if (!success) return;
        
        Console.WriteLine("✓ Autenticación exitosa, iniciando transición...");
        IsAuthenticated = true;
        
        // Fade out login
        await FadeOut();
        
        // Cambiar a splash
        ShowingLogin = false;
        ShowingSplash = true;
        CurrentView = SplashViewModel;
        
        // Fade in splash y continuar con inicialización
        await FadeIn();
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
            
            // Iniciar ambas tareas en paralelo
            var splashTask = SplashViewModel.AnimateAsync();
            var loadTask = DashboardViewModel.InitializeAsync();
            
            // Esperar a que ambas terminen
            await Task.WhenAll(splashTask, loadTask);
            
            // Asegurar un mínimo de tiempo del splash (2.5 segundos total)
            await Task.Delay(500);
            
            // Fade out splash
            await FadeOut();
            
            // Transición al dashboard
            Console.WriteLine("🔄 Transicionando al dashboard...");
            ShowingSplash = false;
            CurrentView = DashboardViewModel;
            
            // Fade in dashboard
            await FadeIn();
            
            Console.WriteLine("✓ Aplicación inicializada correctamente");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error inicializando aplicación: {ex.Message}");
            // En caso de error, mostrar dashboard de todos modos
            ShowingSplash = false;
            CurrentView = DashboardViewModel;
            DashboardOpacity = 1.0;
        }
    }
    
    private async Task FadeOut()
    {
        for (int i = 10; i >= 0; i--)
        {
            if (ShowingLogin)
                LoginOpacity = i / 10.0;
            else
                DashboardOpacity = i / 10.0;
            await Task.Delay(30);
        }
    }
    
    private async Task FadeIn()
    {
        for (int i = 0; i <= 10; i++)
        {
            if (ShowingLogin)
                LoginOpacity = i / 10.0;
            else
                DashboardOpacity = i / 10.0;
            await Task.Delay(30);
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
    
    [RelayCommand]
    private void ShowMarketMonitor()
    {
        Console.WriteLine("🎯 ShowMarketMonitor comando ejecutado");
        Console.WriteLine($"   CurrentView antes: {CurrentView?.GetType().Name}");
        Console.WriteLine($"   DashboardOpacity: {DashboardOpacity}");
        Console.WriteLine($"   IsAuthenticated: {IsAuthenticated}");
        
        // Asegurar que la opacidad esté en 1.0
        DashboardOpacity = 1.0;
        
        CurrentView = MarketMonitorViewModel;
        Console.WriteLine($"   CurrentView después: {CurrentView?.GetType().Name}");
        Console.WriteLine($"   MarketMonitorViewModel es null? {MarketMonitorViewModel == null}");
        // Cargar mercados al abrir la vista
        if (MarketMonitorViewModel != null)
        {
            _ = MarketMonitorViewModel.LoadMarkets();
        }
    }
    
    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarCollapsed = !IsSidebarCollapsed;
        Console.WriteLine($"🎯 Sidebar: {(IsSidebarCollapsed ? "Colapsado" : "Expandido")}");
    }
}

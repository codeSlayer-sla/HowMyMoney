using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace HowsMyMoney.ViewModels;

/// <summary>
/// ViewModel para la pantalla de inicio (splash screen)
/// </summary>
public partial class SplashViewModel : ViewModelBase
{
    [ObservableProperty]
    private double _logoOpacity = 0.0;
    
    [ObservableProperty]
    private double _logoScale = 0.8;
    
    [ObservableProperty]
    private double _textOpacity = 0.0;
    
    [ObservableProperty]
    private string _statusText = "Cargando...";
    
    [ObservableProperty]
    private double _progressValue = 0.0;
    
    public SplashViewModel()
    {
        Console.WriteLine("🎬 SplashViewModel: Inicializado");
    }
    
    /// <summary>
    /// Ejecuta la animación de carga
    /// </summary>
    public async Task AnimateAsync()
    {
        try
        {
            Console.WriteLine("🎬 Iniciando animación de splash...");
            
            // Fase 1: Aparecer logo (0-800ms)
            await Task.Delay(100);
            LogoOpacity = 1.0;
            LogoScale = 1.0;
            
            // Fase 2: Aparecer texto (800-1200ms)
            await Task.Delay(400);
            TextOpacity = 1.0;
            StatusText = "Inicializando...";
            
            // Fase 3: Simular carga de datos (1200-2500ms)
            await Task.Delay(300);
            ProgressValue = 30;
            StatusText = "Conectando con APIs...";
            
            await Task.Delay(300);
            ProgressValue = 60;
            StatusText = "Cargando inversiones...";
            
            await Task.Delay(300);
            ProgressValue = 90;
            StatusText = "Preparando dashboard...";
            
            await Task.Delay(300);
            ProgressValue = 100;
            StatusText = "¡Listo!";
            
            await Task.Delay(400);
            
            Console.WriteLine("✓ Animación de splash completada");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error en animación splash: {ex.Message}");
        }
    }
}

using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HowsMyMoney.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly Action<bool> _onLoginSuccess;
    
    [ObservableProperty]
    private string _username = string.Empty;
    
    [ObservableProperty]
    private string _password = string.Empty;
    
    [ObservableProperty]
    private string _errorMessage = string.Empty;
    
    [ObservableProperty]
    private bool _isLoading = false;
    
    [ObservableProperty]
    private bool _showError = false;

    // Default credentials
    private const string DEFAULT_USERNAME = "admin";
    private const string DEFAULT_PASSWORD = "admin123";

    public LoginViewModel(Action<bool> onLoginSuccess)
    {
        _onLoginSuccess = onLoginSuccess;
    }

    [RelayCommand]
    private async Task Login()
    {
        ShowError = false;
        ErrorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(Username))
        {
            ErrorMessage = "Por favor ingresa tu usuario";
            ShowError = true;
            return;
        }

        if (string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "Por favor ingresa tu contraseña";
            ShowError = true;
            return;
        }

        IsLoading = true;

        // Simulate authentication delay
        await Task.Delay(800);

        if (Username.Trim().ToLower() == DEFAULT_USERNAME && Password == DEFAULT_PASSWORD)
        {
            Console.WriteLine("✓ Login exitoso");
            _onLoginSuccess?.Invoke(true);
        }
        else
        {
            ErrorMessage = "Usuario o contraseña incorrectos";
            ShowError = true;
            IsLoading = false;
            
            // Clear password on failed login
            Password = string.Empty;
        }
    }
}

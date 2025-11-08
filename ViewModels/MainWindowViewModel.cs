using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace HowsMyMoney.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    [ObservableProperty]
    private ViewModelBase _currentView;
    
    public DashboardViewModel DashboardViewModel { get; }
    public AddInvestmentViewModel AddInvestmentViewModel { get; }
    
    public MainWindowViewModel()
    {
        DashboardViewModel = new DashboardViewModel();
        AddInvestmentViewModel = new AddInvestmentViewModel();
        
        // Vista inicial
        _currentView = DashboardViewModel;
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

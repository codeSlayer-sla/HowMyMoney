using Avalonia.Controls;
using System;

namespace HowsMyMoney.Views;

public partial class DashboardView : UserControl
{
    public DashboardView()
    {
        InitializeComponent();
        
        // Debug: verificar DataContext
        this.DataContextChanged += (sender, args) =>
        {
            Console.WriteLine($"🔍 DashboardView: DataContext changed to: {DataContext?.GetType().Name ?? "null"}");
            if (DataContext != null)
            {
                var vm = DataContext as ViewModels.DashboardViewModel;
                if (vm != null)
                {
                    Console.WriteLine($"   ✓ DashboardViewModel encontrado con {vm.Investments?.Count ?? 0} inversiones");
                }
            }
        };
    }
}

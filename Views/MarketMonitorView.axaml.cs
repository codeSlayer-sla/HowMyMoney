using System;
using Avalonia.Controls;

namespace HowsMyMoney.Views;

public partial class MarketMonitorView : UserControl
{
    public MarketMonitorView()
    {
        InitializeComponent();
        
        DataContextChanged += (s, e) =>
        {
            Console.WriteLine($"🔍 MarketMonitorView: DataContext changed to: {DataContext?.GetType().Name ?? "null"}");
            if (DataContext != null)
            {
                var vm = DataContext as ViewModels.MarketMonitorViewModel;
                if (vm != null)
                {
                    Console.WriteLine($"   ✓ MarketMonitorViewModel encontrado");
                    Console.WriteLine($"   • IsCryptoEnabled: {vm.IsCryptoEnabled}");
                    Console.WriteLine($"   • CryptoMarkets.Count: {vm.CryptoMarkets.Count}");
                }
            }
        };
    }
}

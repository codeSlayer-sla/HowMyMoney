using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using HowsMyMoney.Services;
using HowsMyMoney.ViewModels;
using HowsMyMoney.Views;

namespace HowsMyMoney;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit.
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();
            desktop.MainWindow = new MainWindow
            {
                DataContext = CreateMainWindowViewModel(),
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    /// <summary>
    /// Composition root: arma el grafo de dependencias a mano. El grafo es chico
    /// (un puñado de servicios + 3 ViewModels de nivel superior), así que no hace
    /// falta un contenedor de DI; esto alcanza para poder mockear todo en tests.
    /// </summary>
    private static MainWindowViewModel CreateMainWindowViewModel()
    {
        var coinGeckoService = new CoinGeckoService();
        var lisSkinsService = new LisSkinsService();
        var stockService = new StockService();
        var imageCacheService = new ImageCacheService();
        var steamIconService = new SteamIconService();
        var csvExportService = new CsvExportService();

        var investmentService = new InvestmentService(
            coinGeckoService, lisSkinsService, stockService, imageCacheService, steamIconService);

        var dashboardViewModel = new DashboardViewModel(investmentService, csvExportService);
        var addInvestmentViewModel = new AddInvestmentViewModel(
            investmentService, coinGeckoService, lisSkinsService, stockService);
        var marketMonitorViewModel = new MarketMonitorViewModel(
            coinGeckoService, stockService, lisSkinsService, steamIconService, imageCacheService);

        return new MainWindowViewModel(dashboardViewModel, addInvestmentViewModel, marketMonitorViewModel);
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}
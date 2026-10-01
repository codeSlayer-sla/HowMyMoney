using HowsMyMoney.Data;
using HowsMyMoney.Models;
using HowsMyMoney.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace HowsMyMoney.Tests.Services;

/// <summary>
/// Usa un archivo SQLite real (no el InMemory provider de EF, ni ":memory:") porque
/// InvestmentDbContext.MigrateDatabaseSchema abre y CIERRA la conexión subyacente con
/// SQL crudo (PRAGMA, ALTER TABLE) — un ":memory:" pierde todo su contenido en cuanto
/// se cierra la única conexión que lo mantiene vivo. Un archivo temporal por test se
/// comporta igual que en producción y se borra al terminar.
/// </summary>
public class InvestmentServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly DbContextOptions<InvestmentDbContext> _options;

    public InvestmentServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"howsmymoney_test_{Guid.NewGuid():N}.db");

        _options = new DbContextOptionsBuilder<InvestmentDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        using var context = new InvestmentDbContext(_options);
        context.Database.EnsureCreated();
    }

    public void Dispose()
    {
        // Fuerza a cerrar las conexiones pooleadas de Sqlite antes de borrar el archivo,
        // si no, el archivo sigue "en uso" y File.Delete falla.
        SqliteConnection.ClearAllPools();

        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }
    }

    private InvestmentService CreateService(
        Mock<ICoinGeckoService>? coinGecko = null,
        Mock<ILisSkinsService>? lisSkins = null,
        Mock<IStockService>? stock = null,
        Mock<IImageCacheService>? imageCache = null,
        Mock<ISteamIconService>? steamIcon = null)
    {
        return new InvestmentService(
            (coinGecko ?? new Mock<ICoinGeckoService>()).Object,
            (lisSkins ?? new Mock<ILisSkinsService>()).Object,
            (stock ?? new Mock<IStockService>()).Object,
            (imageCache ?? new Mock<IImageCacheService>()).Object,
            (steamIcon ?? new Mock<ISteamIconService>()).Object,
            new InvestmentDbContext(_options));
    }

    [Fact]
    public async Task AddInvestmentAsync_PersistsInvestment_UsingCoinGeckoPrice()
    {
        var coinGecko = new Mock<ICoinGeckoService>();
        coinGecko.Setup(s => s.GetCryptoFullInfoAsync("bitcoin"))
            .ReturnsAsync(new CryptoFullInfo
            {
                Id = "bitcoin",
                Name = "Bitcoin",
                CurrentPrice = 50000m,
                ImageUrl = "https://img/bitcoin.png"
            });

        var service = CreateService(coinGecko: coinGecko);

        var saved = await service.AddInvestmentAsync(new Investment
        {
            Name = "Bitcoin",
            AssetType = AssetType.Criptomoneda,
            Symbol = "bitcoin",
            Quantity = 1,
            PurchasePrice = 40000m,
            PurchaseDate = DateTime.Today
        });

        Assert.True(saved.Id > 0);
        Assert.Equal(50000m, saved.CurrentPrice);
        Assert.Equal("https://img/bitcoin.png", saved.ImageUrl);

        var all = await service.GetAllInvestmentsAsync();
        Assert.Single(all);
    }

    [Fact]
    public async Task DeleteInvestmentAsync_RemovesInvestment()
    {
        var service = CreateService();
        var investment = await service.AddInvestmentAsync(new Investment
        {
            Name = "Reloj",
            AssetType = AssetType.Manual,
            Quantity = 1,
            PurchasePrice = 1000m,
            CurrentPrice = 1200m,
            PurchaseDate = DateTime.Today
        });

        var deleted = await service.DeleteInvestmentAsync(investment.Id);

        Assert.True(deleted);
        Assert.Empty(await service.GetAllInvestmentsAsync());
    }

    [Fact]
    public async Task DeleteInvestmentAsync_ReturnsFalse_WhenNotFound()
    {
        var service = CreateService();

        var deleted = await service.DeleteInvestmentAsync(999);

        Assert.False(deleted);
    }

    [Fact]
    public async Task GetPortfolioSummaryAsync_AggregatesAcrossInvestments()
    {
        var service = CreateService();
        await service.AddInvestmentAsync(new Investment
        {
            Name = "A",
            AssetType = AssetType.Manual,
            Quantity = 1,
            PurchasePrice = 100m,
            CurrentPrice = 150m,
            PurchaseDate = DateTime.Today
        });
        await service.AddInvestmentAsync(new Investment
        {
            Name = "B",
            AssetType = AssetType.Manual,
            Quantity = 2,
            PurchasePrice = 50m,
            CurrentPrice = 40m,
            PurchaseDate = DateTime.Today
        });

        var summary = await service.GetPortfolioSummaryAsync();

        Assert.Equal(2, summary.TotalInvestments);
        Assert.Equal(200m, summary.TotalInvested); // 100 + 2*50
        Assert.Equal(230m, summary.CurrentValue);  // 150 + 2*40
        Assert.Equal(30m, summary.TotalProfitLoss);
    }

    [Fact]
    public async Task UpdateAllPricesAsync_UpdatesSkinPrice_FromLisSkins_AndIcon_FromSteam()
    {
        const string skinName = "AK-47 | Redline (Field-Tested)";

        var lisSkins = new Mock<ILisSkinsService>();
        lisSkins.Setup(s => s.GetSkinPriceAsync(skinName)).ReturnsAsync(30.5m);

        var steamIcon = new Mock<ISteamIconService>();
        steamIcon.Setup(s => s.GetIconUrlAsync(skinName)).ReturnsAsync("https://img/ak47.png");

        var service = CreateService(lisSkins: lisSkins, steamIcon: steamIcon);
        await service.AddInvestmentAsync(new Investment
        {
            Name = skinName,
            AssetType = AssetType.SkinCSGO,
            Quantity = 1,
            PurchasePrice = 20m,
            PurchaseDate = DateTime.Today
        });

        var saved = (await service.GetAllInvestmentsAsync()).Single();
        Assert.Equal(30.5m, saved.CurrentPrice);
        Assert.Equal("https://img/ak47.png", saved.ImageUrl);
    }

    [Fact]
    public async Task UpdateAllPricesAsync_ResolvesSteamIcon_OnlyOnce_OncePersisted()
    {
        const string skinName = "AWP | Asiimov (Field-Tested)";

        var lisSkins = new Mock<ILisSkinsService>();
        lisSkins.Setup(s => s.GetSkinPriceAsync(skinName)).ReturnsAsync(75m);

        var steamIcon = new Mock<ISteamIconService>();
        steamIcon.Setup(s => s.GetIconUrlAsync(skinName)).ReturnsAsync("https://img/awp.png");

        var service = CreateService(lisSkins: lisSkins, steamIcon: steamIcon);
        await service.AddInvestmentAsync(new Investment
        {
            Name = skinName,
            AssetType = AssetType.SkinCSGO,
            Quantity = 1,
            PurchasePrice = 60m,
            PurchaseDate = DateTime.Today
        });

        // Refrescar precios de nuevo: como ImageUrl ya quedó guardado y es válido,
        // no debería volver a golpear a Steam para esta skin.
        await service.UpdateAllPricesAsync();

        steamIcon.Verify(s => s.GetIconUrlAsync(skinName), Times.Once);
    }

    [Fact]
    public async Task UpdateAllPricesAsync_KeepsManualAssetPrice_Unchanged()
    {
        var service = CreateService();
        await service.AddInvestmentAsync(new Investment
        {
            Name = "Reloj",
            AssetType = AssetType.Manual,
            Quantity = 1,
            PurchasePrice = 1000m,
            CurrentPrice = 1200m,
            PurchaseDate = DateTime.Today
        });

        await service.UpdateAllPricesAsync();

        var saved = (await service.GetAllInvestmentsAsync()).Single();
        Assert.Equal(1200m, saved.CurrentPrice);
    }
}

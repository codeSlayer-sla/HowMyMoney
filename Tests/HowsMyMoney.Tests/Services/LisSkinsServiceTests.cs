using HowsMyMoney.Models;
using HowsMyMoney.Services;
using HowsMyMoney.Tests.TestHelpers;

namespace HowsMyMoney.Tests.Services;

public class LisSkinsServiceTests
{
    // Payload reducido con la misma forma que https://lis-skins.com/market_export_json/csgo.json
    private const string SamplePriceList = """
    [
        {"name":"AK-47 | Redline (Field-Tested)","price":26.67,"unlocked_price":26.67,"url":"https://app.lis-skins.com/x","count":10},
        {"name":"AK-47 | Redline (Battle-Scarred)","price":20.79,"unlocked_price":20.79,"url":"https://app.lis-skins.com/y","count":5},
        {"name":"AWP | Asiimov (Field-Tested)","price":75.00,"unlocked_price":80.00,"url":"https://app.lis-skins.com/z","count":3},
        {"name":"★ Karambit | Fade (Factory New)","price":900.00,"unlocked_price":900.00,"url":"https://app.lis-skins.com/w","count":1}
    ]
    """;

    private static LisSkinsService CreateService(string json)
        => new(FakeHttpMessageHandler.CreateClient(json));

    [Fact]
    public async Task SearchSkinsAsync_ReturnsMatchesOrderedByPrice()
    {
        var service = CreateService(SamplePriceList);

        var results = await service.SearchSkinsAsync("AK-47 | Redline");

        Assert.Equal(2, results.Count);
        Assert.Equal("AK-47 | Redline (Battle-Scarred)", results[0].MarketHashName);
        Assert.Equal(20.79m, results[0].MinPrice);
        Assert.Equal("AK-47 | Redline (Field-Tested)", results[1].MarketHashName);
    }

    [Fact]
    public async Task SearchSkinsAsync_FiltersByWeaponCategory_ExcludingKnives()
    {
        var service = CreateService(SamplePriceList);

        var results = await service.SearchSkinsAsync("a", SkinCategory.Arma);

        Assert.All(results, s => Assert.DoesNotContain("★", s.MarketHashName));
        Assert.DoesNotContain(results, s => s.MarketHashName.Contains("Karambit"));
    }

    [Fact]
    public async Task SearchSkinsAsync_FiltersByKnifeCategory()
    {
        var service = CreateService(SamplePriceList);

        var results = await service.SearchSkinsAsync("Fade", SkinCategory.Cuchillo);

        var result = Assert.Single(results);
        Assert.Contains("Karambit", result.MarketHashName);
    }

    [Fact]
    public async Task SearchSkinsAsync_ReturnsEmpty_ForTooShortQuery()
    {
        var service = CreateService(SamplePriceList);

        var results = await service.SearchSkinsAsync("a");

        Assert.Empty(results);
    }

    [Fact]
    public async Task GetSkinPriceAsync_ReturnsPrice_ForExactMatch()
    {
        var service = CreateService(SamplePriceList);

        var price = await service.GetSkinPriceAsync("AWP | Asiimov (Field-Tested)");

        Assert.Equal(75.00m, price);
    }

    [Fact]
    public async Task GetSkinPriceAsync_ReturnsNull_WhenNotFound()
    {
        var service = CreateService(SamplePriceList);

        var price = await service.GetSkinPriceAsync("Nonexistent Skin (Field-Tested)");

        Assert.Null(price);
    }

    [Fact]
    public async Task GetTopSkinsAsync_ReturnsMostExpensiveFirst()
    {
        var service = CreateService(SamplePriceList);

        var top = await service.GetTopSkinsAsync(2);

        Assert.Equal(2, top.Count);
        Assert.Equal("★ Karambit | Fade (Factory New)", top[0].MarketHashName);
        Assert.Equal(900.00m, top[0].MinPrice);
        Assert.Equal("AWP | Asiimov (Field-Tested)", top[1].MarketHashName);
    }

    [Fact]
    public async Task SearchSkinsAsync_CachesPriceList_SoSecondCallDoesNotHitNetworkAgain()
    {
        var handler = new FakeHttpMessageHandler(SamplePriceList);
        var service = new LisSkinsService(new HttpClient(handler));

        await service.SearchSkinsAsync("AK-47");
        var firstRequestUri = handler.LastRequestUri;
        handler.LastRequestUri = null;

        await service.SearchSkinsAsync("AWP");

        // La segunda búsqueda debe resolverse contra la caché en memoria, sin nueva petición HTTP.
        Assert.NotNull(firstRequestUri);
        Assert.Null(handler.LastRequestUri);
    }
}

using HowsMyMoney.Models;

namespace HowsMyMoney.Tests.Models;

public class InvestmentTests
{
    [Fact]
    public void TotalPurchaseValue_MultipliesQuantityByPurchasePrice()
    {
        var investment = new Investment { Quantity = 3, PurchasePrice = 10m };

        Assert.Equal(30m, investment.TotalPurchaseValue);
    }

    [Fact]
    public void CurrentValue_MultipliesQuantityByCurrentPrice()
    {
        var investment = new Investment { Quantity = 3, CurrentPrice = 15m };

        Assert.Equal(45m, investment.CurrentValue);
    }

    [Fact]
    public void ProfitLoss_IsDifferenceBetweenCurrentAndPurchaseValue()
    {
        var investment = new Investment { Quantity = 2, PurchasePrice = 10m, CurrentPrice = 15m };

        Assert.Equal(10m, investment.ProfitLoss);
    }

    [Fact]
    public void ProfitLoss_IsNegative_WhenValueDropped()
    {
        var investment = new Investment { Quantity = 2, PurchasePrice = 20m, CurrentPrice = 15m };

        Assert.Equal(-10m, investment.ProfitLoss);
    }

    [Theory]
    [InlineData(1, 10, 15, 50)]   // ganó 50%
    [InlineData(1, 10, 5, -50)]   // perdió 50%
    [InlineData(2, 100, 100, 0)]  // sin cambio
    public void ProfitLossPercentage_ComputesExpectedPercentage(
        decimal quantity, decimal purchasePrice, decimal currentPrice, decimal expectedPercentage)
    {
        var investment = new Investment
        {
            Quantity = quantity,
            PurchasePrice = purchasePrice,
            CurrentPrice = currentPrice
        };

        Assert.Equal(expectedPercentage, investment.ProfitLossPercentage);
    }

    [Fact]
    public void ProfitLossPercentage_IsZero_WhenPurchasePriceIsZero()
    {
        // Caso "earning" (ver DashboardViewModel.AdjustCryptoQuantityAsync): PurchasePrice = 0
        // no debe explotar por división entre cero.
        var investment = new Investment { Quantity = 5, PurchasePrice = 0, CurrentPrice = 100m };

        Assert.Equal(0m, investment.ProfitLossPercentage);
    }
}

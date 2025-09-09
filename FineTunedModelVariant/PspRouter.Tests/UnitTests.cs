using PspRouter.Lib;

namespace PspRouter.Tests;

public class UnitTests
{
    [Fact]
    public void TestRouteInputStructure()
    {
        // Arrange
        var tx = new RouteInput("M001", "US", "IL", 1, 100.00m, 1, "411111", null, false, false, 10);
        
        // Assert - Verify RouteInput structure
        Assert.Equal("M001", tx.MerchantId);
        Assert.Equal("US", tx.BuyerCountry);
        Assert.Equal("IL", tx.MerchantCountry);
        Assert.Equal(1, tx.CurrencyId);
        Assert.Equal(100.00m, tx.Amount);
        Assert.Equal(1, tx.PaymentMethodId);
        Assert.Equal("411111", tx.PaymentCardBin);
    }
}

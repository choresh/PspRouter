using PspRouter.Lib;

namespace PspRouter.Tests;

public class UnitTests
{
    private readonly ICapabilityProvider capability = new API.DummyCapabilityProvider();

    [Fact]
    public void TestCapabilityProvider()
    {
        // Arrange
        var tx = new RouteInput("M001", "US", "IL", 1, 100.00m, 1, "411111", null, false, false, 10);
        
        // Act
        var supportsAdyen = capability.Supports("Adyen", tx);
        var supportsStripe = capability.Supports("Stripe", tx);
        
        // Assert
        Assert.True(supportsAdyen);
        Assert.True(supportsStripe);
    }
}

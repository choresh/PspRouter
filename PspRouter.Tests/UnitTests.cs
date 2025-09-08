using PspRouter.Lib;

namespace PspRouter.Tests;

public class UnitTests
{
    [Fact]
    public void TestCapabilityMatrix()
    {
        // Arrange
        var tx = new RouteInput("M001", "US", "IL", "USD", 100.00m, PaymentMethod.Card, CardScheme.Visa, false, 10, "411111");
        
        // Act
        var supportsAdyen = CapabilityMatrix.Supports("Adyen", tx);
        var supportsStripe = CapabilityMatrix.Supports("Stripe", tx);
        
        // Assert
        Assert.True(supportsAdyen);
        Assert.True(supportsStripe);
    }

    // Bandit tests removed in pre-trained model variant
}

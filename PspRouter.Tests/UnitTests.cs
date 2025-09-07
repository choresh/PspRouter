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

    [Fact]
    public void TestEpsilonGreedyBandit()
    {
        // Arrange
        var bandit = new EpsilonGreedyBandit(epsilon: 0.1);
        var arms = new[] { "Adyen", "Stripe", "Klarna" };
        
        // Act
        var selectedArm = bandit.Select("test_segment", arms);
        
        // Assert
        Assert.Contains(selectedArm, arms);
    }

    [Fact]
    public void TestContextualEpsilonGreedyBandit()
    {
        // Arrange
        var bandit = new ContextualEpsilonGreedyBandit(epsilon: 0.1);
        var arms = new[] { "Adyen", "Stripe", "Klarna" };
        var context = new Dictionary<string, object>
        {
            ["amount"] = 100.0,
            ["risk_score"] = 15,
            ["currency"] = "USD"
        };
        
        // Act
        var selectedArm = bandit.SelectWithContext("test_segment", arms, context);
        
        // Assert
        Assert.Contains(selectedArm, arms);
    }
}

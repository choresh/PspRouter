using System.Text.Json;
using System.Threading;
using System.Collections.Generic;

namespace PspRouter;

class Program
{
    static async Task Main()
    {
        // Example usage
        var tx = new RouteInput(
            MerchantId: "M123",
            BuyerCountry: "US",
            MerchantCountry: "IL",
            Currency: "USD",
            Amount: 120.00m,
            Method: PaymentMethod.Card,
            Scheme: CardScheme.Visa,
            SCARequired: false,
            RiskScore: 18,
            Bin: "411111"
        );

        var prefs = new Dictionary<string,string> { ["prefer_low_fees"] = "true" };
        var stats = new Dictionary<string,double> {
            ["Adyen_USD_Visa_auth"] = 0.89,
            ["Stripe_USD_Visa_auth"] = 0.87
        };

        var ctx = new RouteContext(tx, new List<PspSnapshot>(), prefs, stats);

        var health = new DummyHealthProvider();
        var fees = new DummyFeeProvider();
        var chat = new DummyChatClient(); // replace with real OpenAI .NET client wrapper

        var router = new PspRouter(chat, health, fees);
        var decision = await router.DecideAsync(ctx, CancellationToken.None);

        Console.WriteLine(JsonSerializer.Serialize(decision, new JsonSerializerOptions { WriteIndented = true }));
    }
}

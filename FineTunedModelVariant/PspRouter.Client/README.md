# PspRouter.Client

A .NET client library for PSP (Payment Service Provider) routing with real-time database access. This library allows external systems to query PSP performance data and make intelligent routing decisions.

## Features

- **Real-time PSP Data**: Query current PSP performance metrics from PaymentTransaction database
- **Intelligent Filtering**: Filter PSPs by currency, payment method, 3DS support, and health status
- **Deterministic Routing**: Make routing decisions based on auth rates and fees
- **Fine-tuned Model Integration**: Support for external AI model services
- **Performance Metrics**: Detailed PSP performance analytics
- **Easy Integration**: Simple API for external systems

## Installation

```bash
dotnet add package PspRouter.Client
```

## Quick Start

### Basic Usage

```csharp
using PspRouter.Client;

// Create client with database connection string
var client = PspRouterClientFactory.Create("Server=localhost;Database=Payments;Trusted_Connection=true;");

// Get all available PSPs
var psps = await client.GetAvailablePspsAsync();
Console.WriteLine($"Found {psps.Count} PSPs");

// Get PSPs for specific transaction
var cardPsps = await client.GetPspsForTransactionAsync(currencyId: 1, paymentMethodId: 1);
```

### Make Routing Decision

```csharp
// Create transaction
var transaction = new RouteInput(
    MerchantId: "M123",
    BuyerCountry: "US",
    MerchantCountry: "US",
    CurrencyId: 1, // USD
    Amount: 150.00m,
    PaymentMethodId: 1, // Card
    PaymentCardBin: "411111",
    SCARequired: false,
    RiskScore: 25
);

// Make deterministic routing decision
var decision = await client.MakeDeterministicDecisionAsync(transaction);

Console.WriteLine($"Selected PSP: {decision.Candidate}");
Console.WriteLine($"Reasoning: {decision.Reasoning}");
```

### With Fine-tuned Model

```csharp
// Make routing decision using AI model
var decision = await client.MakeRoutingDecisionAsync(
    transaction, 
    modelApiUrl: "https://your-model-api.com/route",
    apiKey: "your-api-key"
);
```

## API Reference

### PspRouterClient

Main client class for PSP routing operations.

#### Methods

- `GetAvailablePspsAsync()` - Get all available PSPs
- `GetPspsForTransactionAsync(currencyId, paymentMethodId)` - Get PSPs for specific transaction
- `Get3DSCapablePspsAsync()` - Get PSPs with 3DS support
- `GetPspDetailsAsync(pspName)` - Get detailed performance metrics
- `BuildRouteContextAsync(transaction)` - Build routing context
- `MakeDeterministicDecisionAsync(transaction)` - Make deterministic routing decision
- `MakeRoutingDecisionAsync(transaction, modelApiUrl, apiKey)` - Make AI-powered routing decision

### Models

#### RouteInput
Transaction details for routing decisions.

```csharp
public record RouteInput(
    string MerchantId,
    string BuyerCountry,
    string MerchantCountry,
    long CurrencyId,
    decimal Amount,
    long PaymentMethodId,
    string? PaymentCardBin = null,
    long? ThreeDSTypeId = null,
    bool IsTokenized = false,
    bool SCARequired = false,
    int RiskScore = 0
);
```

#### PspSnapshot
Current PSP performance snapshot.

```csharp
public record PspSnapshot(
    string Name,
    bool Supports,
    string Health,        // "green", "yellow", "red"
    double AuthRate30d,   // 0.0 to 1.0
    int FeeBps,          // Fee in basis points
    decimal FixedFee,    // Fixed fee amount
    bool Supports3DS,
    bool Tokenization
);
```

#### RouteDecision
Final routing decision result.

```csharp
public record RouteDecision(
    string Schema_Version,
    string Decision_Id,
    string Candidate,           // Selected PSP name
    IReadOnlyList<string> Alternates,
    string Reasoning,
    string Guardrail,
    RouteConstraints Constraints,
    IReadOnlyList<string> Features_Used
);
```

## Examples

### Get PSP Performance Metrics

```csharp
var metrics = await client.GetPspDetailsAsync("Adyen");
if (metrics != null)
{
    Console.WriteLine($"Auth Rate (30d): {metrics.AuthRate30d:F2}%");
    Console.WriteLine($"Health Status: {metrics.HealthStatus}");
    Console.WriteLine($"Total Transactions: {metrics.TotalTransactions30d}");
}
```

### Filter PSPs by Requirements

```csharp
// Get PSPs that support 3DS for high-risk transactions
var threeDSPsps = await client.Get3DSCapablePspsAsync();

// Get PSPs for specific currency and payment method
var usdCardPsps = await client.GetPspsForTransactionAsync(currencyId: 1, paymentMethodId: 1);
```

### Batch Processing

```csharp
var transactions = new[]
{
    new RouteInput("M001", "US", "US", 1, 25.00m, 1, "411111", SCARequired: false, RiskScore: 10),
    new RouteInput("M002", "GB", "GB", 2, 100.00m, 1, "555555", SCARequired: true, RiskScore: 60),
};

var decisions = new List<RouteDecision>();
foreach (var transaction in transactions)
{
    var decision = await client.MakeDeterministicDecisionAsync(transaction);
    decisions.Add(decision);
}
```

## Database Requirements

The client requires access to a SQL Server database with a `PaymentTransactions` table containing:

- `PspReference` - PSP name
- `PaymentTransactionStatusId` - Transaction status (5,7,9 = success)
- `CurrencyId` - Currency identifier
- `PaymentMethodId` - Payment method identifier
- `FeeBps` - Fee in basis points
- `FixedFee` - Fixed fee amount
- `ThreeDSTypeId` - 3DS support indicator
- `IsTokenized` - Tokenization support
- `CreatedDate` - Transaction date

## Connection String

```csharp
var connectionString = "Server=your-server;Database=your-db;User Id=your-user;Password=your-password;TrustServerCertificate=true;";
var client = PspRouterClientFactory.Create(connectionString);
```

## Logging

The client supports Microsoft.Extensions.Logging for detailed logging:

```csharp
using Microsoft.Extensions.Logging;

var logger = LoggerFactory.Create(builder => builder.AddConsole()).CreateLogger<PspRouterClient>();
var client = PspRouterClientFactory.Create(connectionString, logger);
```

## Error Handling

All methods throw exceptions for database connection issues, invalid parameters, or other errors:

```csharp
try
{
    var psps = await client.GetAvailablePspsAsync();
}
catch (SqlException ex)
{
    Console.WriteLine($"Database error: {ex.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}
```

## Performance Considerations

- Database queries are optimized for performance
- Results are cached for 30-day lookups
- Minimum transaction volume (10 transactions) required for reliability
- Connection pooling is handled automatically

## License

This library is part of the PspRouter project and follows the same licensing terms.

# 🚀 PSP Router - Intelligent Payment Routing System (ML-Based)

## 🎯 Purpose

Decide the optimal PSP (Adyen / Stripe / Klarna / PayPal) per transaction to maximize auth success, minimize fees, and maintain compliance & reliability using **ML.NET-based decision engine** that learns from historical transaction data.

## 🏗 Solution Overview
- **4-Project Architecture**: Clean separation with Library, API, Tests, and Trainer
- **ASP.NET Core Web API**: RESTful API with Swagger documentation
- **ML.NET-Based**: AI learns PSP patterns, fees, and capabilities from historical data
- **Production Ready**: Structured logging, health checks, and monitoring

## ✨ Key Features

### 🧠 **ML-Based Routing**
- **Intelligent Decision Making**: Uses ML.NET with LightGBM for structured predictions
- **Context-Aware**: Considers transaction context, merchant preferences, and historical data
- **Learned Patterns**: Models learn PSP health, fees, and capabilities from training data
- **No External Dependencies**: No need for real-time health/fee API calls

### 🏗️ **ASP.NET Core Web API Architecture**
- **Enterprise-Grade Web API**: Built on ASP.NET Core for production deployment
- **Dependency Injection**: Automatic service lifetime management and disposal
- **Configuration Management**: Hierarchical configuration with JSON and environment variables
- **Controller-Based**: RESTful API endpoints with proper HTTP semantics
- **Production Ready**: Structured logging, health checks, environment support

### 📊 **Comprehensive Monitoring**
- **Structured Logging**: Detailed logging with Microsoft.Extensions.Logging
- **Performance Metrics**: Processing time, success rates, fee optimization
- **Audit Trail**: Complete decision history with reasoning
- **Error Tracking**: Comprehensive error handling and reporting

## 📦 Project Layout

### 🏗️ **4-Project Architecture**

```
PspRouter/
├── PspRouter.Lib/           # Core business logic library
├── PspRouter.API/           # ASP.NET Core Web API
├── PspRouter.Tests/         # Unit tests
├── PspRouter.Trainer/       # ML model training service
├── PspRouter.sln           # Solution file
├── README.md               # Documentation
├── TODO.md                 # Implementation roadmap
├── AI-ML-CONCEPTS.md       # AI/ML concepts documentation
```

### 📚 **Core Components**
- **Library**: Core routing algorithms with ML.NET and deterministic fallback
- **API**: RESTful API endpoints for routing transactions
- **Tests**: Comprehensive test coverage for all components
- **Trainer**: ML model training and retraining service

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- SQL Server database with PaymentTransactions table
- .env file with database connection

### Build and Run
```bash
# Build all projects
dotnet build

# Run tests
dotnet test

# Run the API
dotnet run --project PspRouter.API

# Run the trainer (for ML model training)
dotnet run --project PspRouter.Trainer

# Run with specific environment
dotnet run --project PspRouter.API --environment Production
```

### Expected Output
```
=== PSP Router API Ready ===
API endpoints available at:
  POST /api/v1/routing/route - Route a transaction
  POST /api/v1/routing/feedback - Process feedback
  GET  /api/v1/routing/health - Check service health
  Swagger UI: https://localhost:7xxx/swagger

info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7000
```

### API Endpoints

#### Route a Transaction
```bash
POST /api/v1/routing/route
Content-Type: application/json

{
  "transaction": {
    "merchantId": "M001",
    "buyerCountry": "US",
    "merchantCountry": "US",
    "currencyId": 1,
    "amount": 150.00,
    "paymentMethodId": 1,
    "paymentCardBin": "411111",
    "isTokenized": false,
    "scaRequired": false,
    "riskScore": 15
  }
}
```

#### Process Feedback
```bash
POST /api/v1/routing/feedback
Content-Type: application/json

{
  "decisionId": "route_20241201_001",
  "merchantId": "M001",
  "pspName": "Stripe",
  "authorized": true,
  "transactionAmount": 150.00,
  "feeAmount": 3.75,
  "processingTimeMs": 1200,
  "riskScore": 15,
  "processedAt": "2024-12-01T12:00:00Z"
}
```

#### Check Service Health
```bash
GET /api/v1/routing/health

Response:
{
  "status": "healthy",
  "timestamp": "2024-12-01T12:00:00Z",
  "service": "PspRouter.API",
  "version": "1.0.0"
}
```

## 📚 Usage Examples

### 🚀 API Usage

#### Start the API
```bash
# Run the API
dotnet run --project PspRouter.API

# Access Swagger UI
https://localhost:7000/swagger
```

#### Test API Endpoints
```bash
# Test routing endpoint
curl -X POST https://localhost:7000/api/v1/routing/route \
  -H "Content-Type: application/json" \
  -d '{
    "transaction": {
      "merchantId": "M001",
      "buyerCountry": "US",
      "merchantCountry": "US",
      "currencyId": 1,
      "amount": 150.00,
      "paymentMethodId": 1,
      "paymentCardBin": "411111",
      "isTokenized": false,
      "scaRequired": false,
      "riskScore": 15
    }
  }'

# Test health endpoint
curl https://localhost:7000/api/v1/routing/health
```

### 📚 Direct API Usage

Call the API directly with transaction data:

```csharp
var client = new HttpClient();
var apiBaseUrl = "https://localhost:7000";

var tx = new
{
    transaction = new
    {
        merchantId = "M123",
        buyerCountry = "US",
        merchantCountry = "US",
        currencyId = 1,
        amount = 150.00m,
        paymentMethodId = 1,
        paymentCardBin = "411111",
        scaRequired = false,
        riskScore = 25
    }
};

var response = await client.PostAsJsonAsync($"{apiBaseUrl}/api/v1/routing/route", tx);
var decision = await response.Content.ReadFromJsonAsync<RouteDecision>();
Console.WriteLine($"Selected PSP: {decision.Candidate}");
```

## 🔧 Configuration

### ML.NET Configuration
```json
{
  "PspRouter": {
    "MLModels": {
      "ModelPath": "./models",
      "RetrainIntervalHours": 24,
      "MinTrainingDataPoints": 1000
    }
  }
}
```

## 📊 Decision Factors
The router applies guardrails first, then uses ML models with configurable weights:

1. **Compliance**: SCA/3DS requirements (guardrail + weighted bonus)
2. **Authorization Success Rates**: Weighted by ML predictions
3. **Fees**: Variable and fixed fees weighted by ML models
4. **Business Bias**: Per-PSP bias map scaled by weights
5. **Health**: "red" excluded by guardrails; "yellow" penalized
6. **Risk**: Penalized based on risk score
7. **3DS Support**: Bonus when SCA is required for cards

## 🛡 Security & Compliance

### Data Protection
- **No PII to ML**: Only aggregated data sent to models
- **Audit Trail**: Complete decision history
- **Secure Storage**: Encrypted database connections

### Compliance Features
- **SCA Enforcement**: Automatic 3DS when required
- **Regulatory Compliance**: Built-in compliance checks
- **Risk Management**: Integrated risk scoring

## 🔄 Training Loop

1. **Data Collection**: Gather historical transaction data from PaymentTransactions table
2. **Data Preparation**: Format data as ML features for training
3. **Model Training**: Train ML.NET models with LightGBM
4. **Model Deployment**: Deploy trained models to production
5. **Performance Monitoring**: Track model accuracy and routing success rates
6. **Model Updates**: Retrain with new data as needed

## 🚀 Production Deployment

### Scaling Considerations
- **Horizontal Scaling**: Stateless design supports multiple instances
- **Caching**: Consider Redis for frequently accessed data
- **Load Balancing**: Distribute traffic across PSP endpoints

### Monitoring Setup
- **Application Insights**: Azure monitoring integration
- **Prometheus/Grafana**: Custom metrics dashboard
- **Alerting**: Set up alerts for critical failures
- **Health Checks**: Endpoint monitoring for all PSPs

## 🧪 Testing

### Unit Tests
```csharp
[Test]
public async Task Router_ShouldFallbackToDeterministic_WhenMLFails()
{
    // Test fallback behavior
}
```

### Integration Tests
```csharp
[Test]
public async Task Learning_ShouldImproveOverTime()
{
    // Test learning convergence
}
```

## 📚 Additional Documentation

For detailed explanations of the AI/ML concepts used in this system, see:
- **[AI-ML-CONCEPTS.md](AI-ML-CONCEPTS.md)**

---

## 🎯 **4-Project Architecture Summary**

The PSP Router uses a professional 4-project solution:

### **✅ What We Achieved:**
- **🏗️ Clean Architecture**: Separation of concerns with Library, API, Tests, and Trainer
- **📚 Reusable Library**: Core business logic can be used by other applications
- **🧪 Comprehensive Testing**: Isolated unit tests for all core functionality
- **🚀 Production Ready**: ASP.NET Core Web API with enterprise-grade hosting
- **🎓 Training Ready**: Dedicated trainer service for ML model training and retraining
- **📦 Package Ready**: Library can be distributed as NuGet package
- **🔧 Maintainable**: Easy to extend and modify individual components
- **🤖 AI-Powered**: ML.NET models learn from historical data

### **🎯 Development Benefits:**
- **Faster Development**: Work on library, app, tests, and trainer independently
- **Better Testing**: Isolated testing without external dependencies
- **Easier Debugging**: Clear boundaries between components
- **Team Collaboration**: Multiple developers can work on different projects
- **CI/CD Ready**: Automated builds and tests for each project

### **🚀 Deployment Benefits:**
- **Flexible Deployment**: Deploy application independently
- **Library Distribution**: Share core logic across multiple applications
- **Version Management**: Independent versioning of library and application
- **Scalability**: Scale application and library separately
- **Model Training**: Separate training pipeline for ML model updates

---

**Last Updated:** December 2024  
**Status:** ✅ ML.NET-based system implemented and working  
**Architecture:** Four specialized LightGBM models with real-time learning
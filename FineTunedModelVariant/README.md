# 🚀 PSP Router - Intelligent Payment Routing System (Fine-Tuned Model variant)

## 🎯 Purpose
Decide the optimal PSP (Adyen / Stripe / Klarna / PayPal) per transaction to maximize auth success, minimize fees, and maintain compliance & reliability using a **fine-tuned LLM-based decision engine** that learns everything from historical transaction data.

## 🏗 Solution Overview
- **3-Project Architecture**: Clean separation with Library, Web API, and Tests
- **ASP.NET Core Web API**: RESTful API with Swagger documentation
- **Configuration Management**: JSON + environment variables with hierarchical config
- **Fine-Tuned Model**: AI learns PSP patterns, fees, and capabilities from historical data
- **Simplified Architecture**: No external providers needed - model learns everything
- **Production Ready**: Structured logging, health checks, and monitoring

## ✨ Key Features

### 🧠 **Fine-Tuned Model Routing**
- **Intelligent Decision Making**: Uses fine-tuned GPT model with structured JSON responses
- **Context-Aware**: Considers transaction context, merchant preferences, and historical data
- **Learned Patterns**: Model learns PSP health, fees, and capabilities from training data
- **No External Dependencies**: No need for real-time health/fee API calls

### 🏗️ **ASP.NET Core Web API Architecture**
- **Enterprise-Grade Web API**: Built on ASP.NET Core for production deployment
- **Dependency Injection**: Automatic service lifetime management and disposal
- **Configuration Management**: Hierarchical configuration with JSON, environment variables, and command line
- **Service Registration**: Clean separation of concerns with proper service lifetimes
- **Controller-Based**: RESTful API endpoints with proper HTTP semantics
- **Extensibility**: Easy to add middleware, filters, and background services

### 📊 **Comprehensive Monitoring**
- **Structured Logging**: Detailed logging with Microsoft.Extensions.Logging
- **Performance Metrics**: Processing time, success rates, fee optimization
- **Audit Trail**: Complete decision history with reasoning
- **Error Tracking**: Comprehensive error handling and reporting
- **Web API Integration**: Logging integrated with ASP.NET Core infrastructure

## 🏗 Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    ASP.NET Core Web API                        │
│  ┌─────────────────────────────────────────────────────────┐   │
│  │              Dependency Injection Container             │   │
│  │  ┌─────────────┐                                       │   │
│  │  │    Chat     │                                       │   │
│  │  │   Client    │                                       │   │
│  │  └─────────────┘                                       │   │
│  └─────────────────────────────────────────────────────────┘   │
└─────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────┐    ┌──────────────────┐     ┌─────────────────┐
│   HTTP Request  │───▶│  Routing         │───▶│   HTTP Response │
│   (JSON)        │    │  Controller      │     │   (JSON)        │
└─────────────────┘    └──────────────────┘     └─────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │   PSP Router     │
                    │   (Scoped)       │
                    └──────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │ Fine-Tuned Model │
                    │   (GPT-4)        │
                    └──────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │  Historical Data │
                    │   (Training)     │
                    └──────────────────┘
```

## 📦 Project Layout

### 🏗️ **3-Project Architecture**

```
PspRouter/
├── PspRouter.Lib/           # Core business logic library
├── PspRouter.API/           # ASP.NET Core Web API
├── PspRouter.Tests/         # Unit tests
├── PspRouter.Trainer/       # Fine-tuning service
├── PspRouter.sln           # Solution file
├── README.md               # Documentation
├── TODO.md                 # Implementation roadmap
├── AI-ML-CONCEPTS.md       # AI/ML concepts documentation
```

### 📚 **PspRouter.Lib** (Core Library)
- `Router.cs` – Decision engine using fine-tuned model
- `DTOs.cs` – Data transfer objects and contracts
- `Interfaces.cs` – Service abstractions (IChatClient only)
- `OpenAIChatClient.cs` – Chat wrapper with `response_format=json_object` for fine-tuned model
- `PspRouter.Lib.csproj` – .NET 8 library with minimal dependencies (`Microsoft.Extensions.Logging.Abstractions`)

### 🚀 **PspRouter.API** (ASP.NET Core Web API)
- `Program.cs` – **ASP.NET Core** application with dependency injection, configuration, and service registration
- `Controllers/RoutingController.cs` – REST API endpoints for routing transactions
- `Dummies.cs` – Mock implementations for local testing and development (DummyChatClient only)
- `appsettings.json` – Configuration file with logging and PSP router settings
- `PspRouter.API.csproj` – .NET 8 Web API with ASP.NET Core dependencies (`Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore`, etc.)

### 🎓 **PspRouter.Trainer** (Fine-Tuning Service)
- `Program.cs` – Console application for training fine-tuned models
- `TrainingService.cs` – OpenAI fine-tuning workflow implementation
- `TrainingDataProvider.cs` – Fetches training data from SQL Server database
- `TrainingManager.cs` – Orchestrates the training process
- `PspRouter.Trainer.csproj` – .NET 8 console application with training dependencies

### 🧪 **PspRouter.Tests** (Unit Tests)
- `UnitTests.cs` – Unit test for core functionality
- `IntegrationTests.cs` – Integration tests demonstrating routing flow with fine-tuned model
- `PspRouter.Tests.csproj` – .NET 8 test project with xUnit framework

### 🗄️ **Database & Configuration**
 

## 🏗️ Project Structure Benefits

### **📚 PspRouter.Lib (Core Library)**
- **Reusable Business Logic**: Core routing algorithms with LLM and deterministic fallback
- **Clean Interfaces**: Well-defined contracts for all services
- **Independent Testing**: Can be unit tested in isolation
- **NuGet Package Ready**: Can be packaged and distributed
- **Framework Agnostic**: No hosting dependencies, pure business logic

### **🚀 PspRouter.API (ASP.NET Core Web API)**
- **RESTful API**: Clean HTTP endpoints for routing transactions
- **Swagger Documentation**: Interactive API documentation with OpenAPI specification
- **Production Ready**: Structured logging, health checks, environment support
- **Flexible Deployment**: Can be deployed as web app, service, or container
- **Clean Architecture**: Minimal, focused API that hosts the router services

### **🧪 PspRouter.Tests (Unit Tests)**
- **Comprehensive Coverage**: Tests for all core business logic
- **Integration Testing**: Complete routing flow with learning demonstration
- **Fast Execution**: Isolated tests without external dependencies
- **CI/CD Ready**: Automated testing in build pipelines
- **Quality Assurance**: Ensures reliability and correctness

### **🎯 Development Workflow**
```bash
# 1. Develop core logic in PspRouter.Lib
# 2. Test with PspRouter.Tests
# 3. Integrate and demo with PspRouter.API
# 4. Package library for distribution
```


## 🔄 Flow Diagrams

### 🔄 PSP Router Decision Flow
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Transaction   │───▶│   Fine-Tuned     │───▶│   Route         │
│   Request       │    │   Model          │    │   Decision      │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                │
                                ▼
                    ┌──────────────────┐
                    │  Historical Data │
                    │   (Training)     │
                    └──────────────────┘
```

### 🧠 Fine-Tuned Model Decision Process Flow
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Transaction   │───▶│   Build Context  │───▶│   System        │
│   Details       │    │   (Features)     │    │   Prompt        │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                        │
                                                        ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Learned       │◀───│   Fine-Tuned     │◀───│   Send to       │
│   Patterns      │    │   Model Analysis │    │   OpenAI        │
└─────────────────┘    └──────────────────┘    └─────────────────┘
        │                        │
        ▼                        ▼
┌─────────────────┐    ┌──────────────────┐
│   Historical    │    │   Structured     │
│   Knowledge     │    │   JSON Response  │
└─────────────────┘    └──────────────────┘
```




### 🔄 System Integration Flow
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   ASP.NET Core  │───▶│   Dependency     │───▶│   Service       │
│   Web API       │    │   Injection      │    │   Registration  │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                        │
                                                        ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Configuration │◀───│   Service        │◀───│   Lifetime      │
│   Management    │    │   Resolution     │    │   Management    │
└─────────────────┘    └──────────────────┘    └─────────────────┘
        │                        │
        ▼                        ▼
┌─────────────────┐    ┌──────────────────┐
│   Environment   │    │   HTTP Request   │
│   Variables     │    │   Processing     │
└─────────────────┘    └──────────────────┘
```

### 🎯 Decision Tree Flow
```
┌─────────────────┐
│   Transaction   │
│   Request       │
└─────────┬───────┘
          │
          ▼
┌─────────────────┐    ┌──────────────────┐
│   Fine-Tuned    │───▶│   Route          │
│   Model         │    │   Decision       │
└─────────────────┘    └──────────────────┘
          │
          ▼
┌─────────────────┐
│  Historical     │
│  Knowledge      │
│  (Training)     │
└─────────────────┘
```

### 📊 Flow Diagram Summary

- **🔄 PSP Router Decision Flow**: Shows the complete decision-making process from transaction request to final routing decision
- **🧠 Fine-Tuned Model Decision Process Flow**: Details how the fine-tuned model analyzes transactions and makes intelligent decisions
- **🔄 System Integration Flow**: Demonstrates the ASP.NET Core Web API architecture and dependency injection
- **🎯 Decision Tree Flow**: Provides a high-level view of the simplified decision logic

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- OpenAI API key with fine-tuning access
- SQL Server database with PaymentTransactions table
- .env file with API keys and database connection

### 🏗️ **3-Project Architecture Benefits**
This solution uses a clean 3-project architecture, providing:
- **Separation of Concerns**: Clear boundaries between business logic, application, and tests
- **Reusability**: Library can be used by other applications or packaged as NuGet package
- **Testability**: Isolated testing of core business logic
- **Maintainability**: Easier to maintain and extend individual components
- **Deployment Flexibility**: Application can be deployed independently
- **Professional Structure**: Industry-standard .NET solution organization

### 🏗️ **ASP.NET Core Web API Benefits**
The application uses ASP.NET Core Web API, providing:
- **Automatic Dependency Injection**: Services are automatically registered and managed
- **Configuration Management**: JSON + environment variables with hierarchical config
- **Structured Logging**: Built-in logging with multiple providers
- **Controller-Based Architecture**: RESTful API endpoints with proper HTTP semantics
- **Production Ready**: Enterprise-grade web application infrastructure
- **Extensibility**: Easy to add middleware, filters, and background services
- **Service Lifetime Management**: Automatic disposal and cleanup of resources
- **Environment Support**: Development, Staging, Production configurations
- **Command Line Integration**: Built-in support for command line arguments
- **Deployment Flexibility**: Can be deployed as web app, service, or container

### 2. Build and Run the Application

```bash
# Build all projects
dotnet build

# Run tests
dotnet test

# Run the API
dotnet run --project PspRouter.API

# Run the trainer (for fine-tuning)
dotnet run --project PspRouter.Trainer

# Run with specific environment
dotnet run --project PspRouter.API --environment Production

# Run with custom configuration
dotnet run --project PspRouter.API --configuration Release

# Build specific project
dotnet build PspRouter.Lib
dotnet build PspRouter.API
dotnet build PspRouter.Tests
dotnet build PspRouter.Trainer

# Run tests for specific project
dotnet test PspRouter.Tests
```

### Expected Output
```
=== PSP Router API Ready ===
API endpoints available at:
  POST /api/routing/route - Route a transaction
  GET  /api/routing/health - Check service health
  Swagger UI: https://localhost:7xxx/swagger

info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:7000
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
```

### API Endpoints

#### Route a Transaction
```bash
POST /api/routing/route
Content-Type: application/json

{
  "transaction": {
    "merchantId": "M001",
    "country": "US",
    "region": "IL",
    "currencyId": 1,
    "amount": 150.00,
    "paymentMethodId": 1,
    "paymentCardBin": "411111",
    "threeDSTypeId": null,
    "isTokenized": false,
    "scaRequired": false,
    "riskScore": 15
  },
  "candidates": [
    {
      "name": "Adyen",
      "available": true,
      "health": "green",
      "authRate30d": 0.89,
      "latencyMs": 50,
      "feeBps": 200,
      "supports3DS": true,
      "supportsRefunds": true
    }
  ],
  "preferences": {
    "prefer_low_fees": "true"
  },
  "statistics": {
    "Adyen_USD_Visa_auth": 0.89,
    "Stripe_USD_Visa_auth": 0.87
  }
}
```

/
#### Check Service Health
```bash
GET /api/routing/health

Response:
{
  "status": "healthy",
  "timestamp": "2024-12-01T12:00:00Z",
  "service": "PspRouter.API",
  "version": "1.0.0"
}
```

## 📋 Usage Examples

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
curl -X POST https://localhost:7000/api/routing/route \
  -H "Content-Type: application/json" \
  -d '{
    "transaction": {
      "merchantId": "M001",
      "country": "US",
      "region": "IL",
      "currencyId": 1,
      "amount": 150.00,
      "paymentMethodId": 1,
      "paymentCardBin": "411111",
      "threeDSTypeId": null,
      "isTokenized": false,
      "scaRequired": false,
      "riskScore": 15
    },
    "candidates": [
      {
        "name": "Adyen",
        "available": true,
        "health": "green",
        "authRate30d": 0.89,
        "latencyMs": 50,
        "feeBps": 200,
        "supports3DS": true,
        "supportsRefunds": true
      }
    ]
  }'

# Test health endpoint
curl https://localhost:7000/api/routing/health
```

#### PowerShell Examples
```powershell
# Test routing endpoint
Invoke-RestMethod -Uri "https://localhost:7000/api/routing/route" -Method POST -ContentType "application/json" -Body '{
  "transaction": {
    "merchantId": "M001",
    "country": "US",
    "region": "IL", 
    "currencyId": 1,
    "amount": 150.00,
    "paymentMethodId": 1,
    "paymentCardBin": "411111",
    "threeDSTypeId": null,
    "isTokenized": false,
    "scaRequired": false,
    "riskScore": 15
  },
  "candidates": [
    {
      "name": "Adyen",
      "available": true,
      "health": "green",
      "authRate30d": 0.89,
      "latencyMs": 50,
      "feeBps": 200,
      "supports3DS": true,
      "supportsRefunds": true
    }
  ]
}'

# Test health endpoint
Invoke-RestMethod -Uri "https://localhost:7000/api/routing/health" -Method GET
```

### 📚 Library Usage

#### Basic Routing
```csharp
var router = new PspRouter(chatClient, logger);
var decision = await router.Decide(context, cancellationToken);
```

## 🔧 Configuration

### Fine-Tuned Model Configuration
```csharp
var chatClient = new OpenAIChatClient(apiKey, model: "ft:gpt-3.5-turbo:your-org:psp-router:abc123");
```

## 📊 Decision Factors

The fine-tuned model considers multiple factors learned from historical data:

1. **Compliance** (SCA/3DS requirements)
2. **Authorization Success Rates** (learned from historical performance)
3. **Fee Optimization** (learned from transaction outcomes)
4. **Merchant Preferences** (learned from merchant patterns)
5. **Historical Performance** (learned from training data)
6. **PSP Capabilities** (learned from successful transaction patterns)
7. **Payment Method Compatibility** (learned from BIN and method patterns)
8. **Geographic Patterns** (learned from country/region success rates)


## 🛡 Security & Compliance

### Data Protection
- **No PII to LLM**: Only BIN/scheme/aggregates sent to AI
- **Audit Trail**: Complete decision history
- **Secure Storage**: Encrypted database connections

### Compliance Features
- **SCA Enforcement**: Automatic 3DS when required
- **Regulatory Compliance**: Built-in compliance checks
- **Risk Management**: Integrated risk scoring

## 🔄 Training Loop

1. **Data Collection**: Gather historical transaction data from PaymentTransactions table
2. **Data Preparation**: Format data as JSONL training examples (RouteInput → RouteDecision)
3. **Model Training**: Upload data to OpenAI and create fine-tuning job
4. **Model Deployment**: Deploy fine-tuned model to production
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
public async Task Router_ShouldFallbackToDeterministic_WhenLLMFails()
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

## 📚 API Reference

### 📚 PspRouter.Lib (Core Library)

#### Core Classes

##### `PspRouter` (in `Router.cs`)
Main routing engine using fine-tuned model.

##### `OpenAIChatClient` (in `OpenAIChatClient.cs`)
Fine-tuned model integration with structured JSON responses.

#### Key Methods

##### `Decide(RouteContext, CancellationToken)`
Makes routing decision using fine-tuned model.

### 🚀 PspRouter.API (ASP.NET Core Web API)

#### `Program.cs`
ASP.NET Core application with dependency injection and service registration.

#### `Controllers/RoutingController.cs`
REST API controller with endpoint for routing transactions.

#### `DummyChatClient`
Mock implementation for local testing and development.

### 🧪 PspRouter.Tests (Unit Tests)

#### `PspRouterTests`
Test cases for core functionality including:
- RouteInput structure validation
- Fine-tuned model integration

#### `IntegrationTests`
Integration tests demonstrating complete routing flow:
- End-to-end routing with fine-tuned model
- Complete system integration testing


---

## 📚 **Additional Documentation**

For detailed explanations of the AI/ML concepts used in this system, see:
- **[AI-ML-CONCEPTS.md](AI-ML-CONCEPTS.md)**

---

## 🎯 **4-Project Architecture Summary**

The PSP Router has been successfully restructured into a professional 4-project solution:

### **✅ What We Achieved:**
- **🏗️ Clean Architecture**: Separation of concerns with Library, Application, Tests, and Trainer
- **📚 Reusable Library**: Core business logic can be used by other applications
- **🧪 Comprehensive Testing**: Isolated unit tests for all core functionality
- **🚀 Production Ready**: ASP.NET Core Web API with enterprise-grade hosting
- **🎓 Training Ready**: Dedicated trainer service for fine-tuning models
- **📦 Package Ready**: Library can be distributed as NuGet package
- **🔧 Maintainable**: Easy to extend and modify individual components
- **🤖 AI-Powered**: Fine-tuned model learns from historical data

### **🎯 Development Benefits:**
- **Faster Development**: Work on library, app, and tests independently
- **Better Testing**: Isolated testing without external dependencies
- **Easier Debugging**: Clear boundaries between components
- **Team Collaboration**: Multiple developers can work on different projects
- **CI/CD Ready**: Automated builds and tests for each project

### **🚀 Deployment Benefits:**
- **Flexible Deployment**: Deploy application independently
- **Library Distribution**: Share core logic across multiple applications
- **Version Management**: Independent versioning of library and application
- **Scalability**: Scale application and library separately

### **📈 Next Steps:**
1. **Train Model**: Use PspRouter.Trainer to create fine-tuned model
2. **Package Library**: Create NuGet package for distribution
3. **Add More Tests**: Expand test coverage for all components
4. **Add Monitoring**: Integrate with Application Insights or Prometheus
5. **Documentation**: Add XML documentation for public APIs
6. **Authentication**: Add API authentication and authorization

---
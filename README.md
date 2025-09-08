# 🚀 PSP Router - Intelligent Payment Routing System (Pre-Trained LLM Variant)

## 🎯 Purpose
Decide the optimal PSP (Adyen / Stripe / Klarna / PayPal) per transaction to maximize auth success, minimize fees, and maintain compliance & reliability using a pre-trained **LLM-based decision engine** with deterministic fallback.

## 🏗 Solution Overview
- **3-Project Architecture**: Clean separation with Library, Web API, and Tests
- **ASP.NET Core Web API**: RESTful API with Swagger documentation
- **Configuration Management**: JSON + environment variables with hierarchical config
- **Deterministic Guardrails**: Capabilities, SCA/3DS, health checks
- **LLM Decision Engine**: GPT-4 with tool calling and structured responses
 
// Vector Memory System removed in this variant
- **Graceful Fallback**: Deterministic scoring when LLM is unavailable
- **Production Ready**: Structured logging, health checks, and monitoring

## ✨ Key Features

### 🧠 **LLM-Powered Routing**
- **Intelligent Decision Making**: Uses GPT-4 with structured JSON responses
- **Context-Aware**: Considers transaction context, merchant preferences, and historical data
- **Tool Integration**: Can call external APIs for real-time health and fee data
- **Fallback Safety**: Graceful degradation to deterministic scoring when LLM is unavailable

// Bandit learning and vector memory are not used in the pre-trained model variant

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
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐       │   │
│  │  │   Health    │ │    Fees     │ │    Chat     │       │   │
│  │  │  Provider   │ │  Provider   │ │   Client    │       │   │
│  │  └─────────────┘ └─────────────┘ └─────────────┘       │   │
│  │  ┌─────────────┐ ┌─────────────┐ ┌─────────────┐       │   │
│  │                                                     │   │
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
                    │   LLM Engine     │
                    │   (GPT-4)        │
                    └──────────────────┘
                              │
                              ▼
 
```

## 📦 Project Layout

### 🏗️ **3-Project Architecture**

```
PspRouter/
├── PspRouter.Lib/           # Core business logic library
├── PspRouter.API/           # ASP.NET Core Web API
├── PspRouter.Tests/         # Unit tests
├── PspRouter.sln           # Solution file
├── README.md               # Documentation
 
```

### 📚 **PspRouter.Lib** (Core Library)
- `Router.cs` – Decision engine (LLM first, fallback on parse/errors)
- `DTOs.cs` – Data transfer objects and contracts
- `Interfaces.cs` – Service abstractions and interfaces
- `Tools.cs` – LLM tool implementations (`get_psp_health`, `get_fee_quote`)
- `OpenAIChatClient.cs` – Chat wrapper with `response_format=json_object` and tool-calling loop
- `CapabilityMatrix.cs` – Deterministic PSP support rules (method→PSP gating)
- `PspRouter.Lib.csproj` – .NET 8 library with core dependencies (`OpenAI`, `Microsoft.Extensions.Logging.Abstractions`)

### 🚀 **PspRouter.API** (ASP.NET Core Web API)
- `Program.cs` – **ASP.NET Core** application with dependency injection, configuration, and service registration
- `Controllers/RoutingController.cs` – REST API endpoints for routing transactions
- `Dummies.cs` – Mock implementations for local testing and development
- `appsettings.json` – Configuration file with logging and PSP router settings
- `PspRouter.API.csproj` – .NET 8 Web API with ASP.NET Core dependencies (`Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore`, etc.)

### 🧪 **PspRouter.Tests** (Unit Tests)
- `UnitTests.cs` – Unit test for core functionality (CapabilityMatrix)
- `IntegrationTests.cs` – Integration tests demonstrating routing flow
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
│   Transaction   │───▶│   Guardrails     │───▶│   LLM Router    │
│   Request       │    │   (Compliance)   │    │   (Primary)     │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                        │
                                                        ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
 
│   Learning      │    │   Router         │    │   (Parse Error) │
└─────────────────┘    └──────────────────┘    └─────────────────┘
        │                        │
        ▼                        ▼
┌─────────────────┐    ┌──────────────────┐
│   Update        │    │   Route          │
│   Rewards       │    │   Decision       │
└─────────────────┘    └──────────────────┘
```

### 🧠 LLM Decision Process Flow
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Transaction   │───▶│   Build Context  │───▶│   System        │
│   Details       │    │   (Features)     │    │   Prompt        │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                        │
                                                        ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Tool Calls    │◀───│   LLM Analysis   │◀───│   Send to       │
│   (Health/Fees) │    │   & Reasoning    │    │   OpenAI        │
└─────────────────┘    └──────────────────┘    └─────────────────┘
        │                        │
        ▼                        ▼
┌─────────────────┐    ┌──────────────────┐
│   Real-time     │    │   Structured     │
│   Data          │    │   JSON Response  │
└─────────────────┘    └──────────────────┘
```

 
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Transaction   │───▶│   Extract        │───▶│   Calculate     │
│   Context       │    │   Features       │    │   Scores        │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                        │
                                                        ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Update        │◀───│   Select Arm     │◀───│   Epsilon       │
│   Statistics    │    │   (Exploit/      │    │   Decision      │
│   & Features    │    │   Explore)       │    │   (Random?)     │
└─────────────────┘    └──────────────────┘    └─────────────────┘
        │                        │
        ▼                        ▼
┌─────────────────┐    ┌──────────────────┐
│   Learning      │    │   PSP            │
│   Progress      │    │   Selection      │
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
│   Capability    │───▶│   Supported?     │
│   Check         │    │   (Yes/No)       │
└─────────────────┘    └─────────┬────────┘
                                 │
                                 ▼
                    ┌─────────────────┐
                    │   Health        │
                    │   Check         │
                    └─────────┬───────┘
                              │
                              ▼
                    ┌─────────────────┐    ┌──────────────────┐
                    │   LLM           │───▶│   Success?       │
                    │   Decision      │    │   (Yes/No)       │
                    └─────────────────┘    └─────────┬────────┘
                                                     │
                                                     ▼
                                        ┌─────────────────┐
                                        │   Bandit        │
                                        │   Fallback      │
                                        └─────────────────┘
```

### 📊 Flow Diagram Summary

- **🔄 PSP Router Decision Flow**: Shows the complete decision-making process from transaction request to final routing decision
- **🧠 LLM Decision Process Flow**: Details how the LLM analyzes transactions and makes intelligent decisions
- // Bandit Learning Flow not applicable in this variant
- // Vector Memory Flow not applicable in this variant
- **🔄 System Integration Flow**: Demonstrates the ASP.NET Core Web API architecture and dependency injection
- **🎯 Decision Tree Flow**: Provides a high-level view of the decision logic and fallback mechanisms

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- // PostgreSQL with pgvector extension not required in this variant
- OpenAI API key

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


 

### 2. Environment Configuration

```bash
# Windows (PowerShell)
$env:OPENAI_API_KEY="sk-your-openai-key"
 

# Linux/Mac
export OPENAI_API_KEY="sk-your-openai-key"
 
```

### 3. Configuration Management

The application uses hierarchical configuration with the following precedence (highest to lowest):

1. **Command Line Arguments**: `dotnet run -- --setting=value`
2. **Environment Variables**: `OPENAI_API_KEY`
3. **appsettings.{Environment}.json**: Environment-specific settings
4. **appsettings.json**: Default configuration

#### Configuration Files

**`appsettings.json`** (default):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  },
  "PspRouter": {
    "OpenAI": {
      "Model": "gpt-4.1",
      
  }
}
```

**`appsettings.Production.json`** (production overrides):
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Warning",
      "PspRouter": "Information"
    }
  },
  "PspRouter": {
  }
}
```

### 4. Service Registration & Lifetimes

The application uses proper dependency injection with appropriate service lifetimes:

```csharp
// Singleton services (shared across the application)
services.AddSingleton<IHealthProvider, DummyHealthProvider>();
services.AddSingleton<IFeeQuoteProvider, DummyFeeProvider>();
services.AddSingleton<IChatClient>(provider => new OpenAIChatClient(apiKey, model: "gpt-4.1"));
// Embeddings, vector memory, and bandit registrations removed in this variant

// Scoped services (per request/operation)
services.AddScoped<PspRouter.PspRouter>(provider => /* ... */);

// Transient services (new instance each time)
services.AddTransient<PspRouterDemo>();
```

**Service Lifetime Benefits:**
- **Singleton**: Shared state, efficient resource usage (health providers, embeddings)
- **Scoped**: Per-operation state, thread-safe (PSP router instances)
- **Transient**: No shared state, always fresh (demo services)

### 5. Build and Run the Application

```bash
# Build all projects
dotnet build

# Run tests
dotnet test

# Run the API
dotnet run --project PspRouter.API

# Run with specific environment
dotnet run --project PspRouter.API --environment Production

# Run with custom configuration
dotnet run --project PspRouter.API --configuration Release

# Build specific project
dotnet build PspRouter.Lib
dotnet build PspRouter.API
dotnet build PspRouter.Tests

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
    "currency": "USD",
    "amount": 150.00,
    "method": "Card",
    "scheme": "Visa",
    "scaRequired": false,
    "riskScore": 15,
    "bin": "411111"
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

// Outcome endpoint removed in pre-trained model variant

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
      "currency": "USD",
      "amount": 150.00,
      "method": "Card",
      "scheme": "Visa",
      "scaRequired": false,
      "riskScore": 15,
      "bin": "411111"
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

# Outcome endpoint removed in pre-trained model variant
```

#### PowerShell Examples
```powershell
# Test routing endpoint
Invoke-RestMethod -Uri "https://localhost:7000/api/routing/route" -Method POST -ContentType "application/json" -Body '{
  "transaction": {
    "merchantId": "M001",
    "country": "US",
    "region": "IL", 
    "currency": "USD",
    "amount": 150.00,
    "method": "Card",
    "scheme": "Visa",
    "scaRequired": false,
    "riskScore": 15,
    "bin": "411111"
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
var router = new PspRouter(chatClient, healthProvider, feeProvider, tools, logger);
var decision = await router.DecideAsync(context, cancellationToken);
```

// No online learning in pre-trained model variant

## 🔧 Configuration

// Bandit learning removed in pre-trained model variant

### LLM Configuration
```csharp
var chatClient = new OpenAIChatClient(apiKey, model: "gpt-4.1");
```

// Vector memory removed in pre-trained model variant

## 📊 Decision Factors

The system considers multiple factors in routing decisions:

1. **Compliance** (SCA/3DS requirements)
2. **Authorization Success Rates** (historical performance)
3. **Fee Optimization** (minimize transaction costs)
4. **Merchant Preferences** (configured preferences)
5. **Historical Performance** (if provided by external stats)
6. **Real-time Health** (PSP availability and latency)

## 🎯 Reward Calculation

The learning system uses a sophisticated reward function:

```csharp
reward = baseAuthReward - feePenalty + speedBonus - riskPenalty
```

- **Base Reward**: +1.0 for successful authorization, 0.0 for decline
- **Fee Penalty**: Normalized fee amount as percentage of transaction
- **Speed Bonus**: +0.1 for processing under 1 second
- **Risk Penalty**: -0.2 for high-risk transactions (>50 risk score)


## 🛡 Security & Compliance

### Data Protection
- **No PII to LLM**: Only BIN/scheme/aggregates sent to AI
- **Audit Trail**: Complete decision history
- **Secure Storage**: Encrypted database connections

### Compliance Features
- **SCA Enforcement**: Automatic 3DS when required
- **Regulatory Compliance**: Built-in compliance checks
- **Risk Management**: Integrated risk scoring

## 🔄 Learning Loop

1. **Route Decision**: LLM selects PSP (with deterministic fallback)
2. **Transaction Processing**: PSP processes payment
3. **Outcome Capture**: Success/failure, fees, timing
4. **Reward Calculation**: Multi-factor reward computation
// No in-service learning in this variant

## 🚀 Production Deployment

### Environment Variables for Production
```bash
# Production OpenAI API key
export OPENAI_API_KEY="sk-prod-your-key"

 
```

 

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
Main routing engine with LLM and deterministic fallback.

// Bandit and vector memory classes removed in pre-trained model variant

##### `OpenAIChatClient` (in `OpenAIChatClient.cs`)
LLM integration with tool calling support.

#### Key Methods

##### `DecideAsync(RouteContext, CancellationToken)`
Makes routing decision using LLM or fallback logic.

// No UpdateReward in pre-trained model variant

### 🚀 PspRouter.API (ASP.NET Core Web API)

#### `Program.cs`
ASP.NET Core application with dependency injection and service registration.

#### `Controllers/RoutingController.cs`
REST API controller with endpoint for routing transactions.

#### `DummyHealthProvider` & `DummyFeeProvider`
Mock implementations for local testing and development.

### 🧪 PspRouter.Tests (Unit Tests)

#### `PspRouterTests`
Test cases for core functionality including:
- CapabilityMatrix validation
- Bandit-related tests removed in pre-trained model variant

#### `IntegrationTests`
Integration tests demonstrating complete routing flow:
- End-to-end routing with LLM decision and deterministic fallback
- Complete system integration testing

#### `AddAsync(string, string, Dictionary, float[], CancellationToken)`
Adds lesson to vector memory.

#### `SearchAsync(float[], int, CancellationToken)`
Searches vector memory for relevant lessons.


---

## 📚 **Additional Documentation**

For detailed explanations of the AI/ML concepts used in this system, see:
- **[AI-ML-CONCEPTS.md](AI-ML-CONCEPTS.md)** - LLM decision engine and current pre-trained model approach

---

## 🎯 **3-Project Architecture Summary**

The PSP Router has been successfully restructured into a professional 3-project solution:

### **✅ What We Achieved:**
- **🏗️ Clean Architecture**: Separation of concerns with Library, Application, and Tests
- **📚 Reusable Library**: Core business logic can be used by other applications
- **🧪 Comprehensive Testing**: Isolated unit tests for all core functionality
- **🚀 Production Ready**: ASP.NET Core Web API with enterprise-grade hosting
- **📦 Package Ready**: Library can be distributed as NuGet package
- **🔧 Maintainable**: Easy to extend and modify individual components

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
1. **Package Library**: Create NuGet package for distribution
2. **Add More Tests**: Expand test coverage for all components
3. **Add Monitoring**: Integrate with Application Insights or Prometheus
4. **Documentation**: Add XML documentation for public APIs
5. **Authentication**: Add API authentication and authorization

---
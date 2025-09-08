# 🚀 PSP Router - Intelligent Payment Routing System (RAG-based variant)

## 🎯 Purpose
Decide the optimal PSP (Adyen / Stripe / Klarna / PayPal) per transaction to maximize auth success, minimize fees, and maintain compliance & reliability using **LLM-based decision making**, **multi-armed bandit learning**, and **vector memory**.

## 🏗 Solution Overview
- **3-Project Architecture**: Clean separation with Library, Web API, and Tests
- **ASP.NET Core Web API**: RESTful API with Swagger documentation
- **Configuration Management**: JSON + environment variables with hierarchical config
- **Deterministic Guardrails**: Capabilities, SCA/3DS, health checks
- **LLM Decision Engine**: GPT-4 with tool calling and structured responses
- **Contextual Bandit Learning**: Enhanced epsilon-greedy with transaction context
- **Vector Memory System**: pgvector-powered semantic search for lessons learned
- **Graceful Fallback**: Deterministic scoring when LLM is unavailable
- **Production Ready**: Structured logging, health checks, and monitoring

## ✨ Key Features

### 🧠 **LLM-Powered Routing**
- **Intelligent Decision Making**: Uses GPT-4 with structured JSON responses
- **Context-Aware**: Considers transaction context, merchant preferences, and historical data
- **Tool Integration**: Can call external APIs for real-time health and fee data
- **Fallback Safety**: Graceful degradation to deterministic scoring when LLM is unavailable

### 🎰 **Multi-Armed Bandit Learning**
- **Contextual Bandits**: Enhanced epsilon-greedy with transaction context awareness
- **Epsilon-Greedy Algorithm**: Balances exploration vs exploitation (configurable epsilon)
- **Thompson Sampling**: Bayesian approach for optimal arm selection
- **Contextual Features**: Uses transaction amount, risk score, and other features for better decisions
- **Real-Time Updates**: Continuous learning from transaction outcomes
- **Production Ready**: Thread-safe, efficient, and well-tested implementations

### 🧠 **Vector Memory System**
- **Semantic Search**: pgvector-powered similarity search for routing lessons
- **Embedding Storage**: OpenAI embeddings for contextual memory
- **Lesson Learning**: Automatic capture and retrieval of routing insights
- **Historical Context**: Leverages past decisions for better routing
- **LLM Integration**: Used exclusively for LLM-based routing decisions (not fallback)
- **System Memory**: Acts as the "memory" of the system, helping the LLM make more informed decisions based on past experience, similar to how a human expert would recall similar past situations when making routing decisions

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
│  │  │   Bandit    │ │   Memory    │ │  Embeddings │       │   │
│  │  │  Learning   │ │   System    │ │   Service   │       │   │
│  │  └─────────────┘ └─────────────┘ └─────────────┘       │   │
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
                    ┌──────────────────┐
                    │   Contextual     │
                    │   Bandit         │
                    └──────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │   Vector         │
                    │   Memory         │
                    │   (pgvector)     │
                    └──────────────────┘
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
└── setup-database.sql      # Database setup
```

### 📚 **PspRouter.Lib** (Core Library)
- `Router.cs` – Decision engine (LLM first, fallback on parse/errors)
- `DTOs.cs` – Data transfer objects and contracts
- `Interfaces.cs` – Service abstractions and interfaces
- `Tools.cs` – LLM tool implementations (`get_psp_health`, `get_fee_quote`)
- `Bandit.cs` – Multi-armed bandit implementations (`IBandit`, `IContextualBandit`, `EpsilonGreedyBandit`, `ThompsonSamplingBandit`, `ContextualEpsilonGreedyBandit`)
- `MemoryPgVector.cs` – `PgVectorMemory` implementation with pgvector (ensure schema, add/search)
- `OpenAIChatClient.cs` – Chat wrapper with `response_format=json_object` and tool-calling loop
- `EmbeddingsHelper.cs` – `OpenAIEmbeddings` service for vector embeddings
- `CapabilityMatrix.cs` – Deterministic PSP support rules (method→PSP gating)
- `PspRouter.Lib.csproj` – .NET 8 library with core dependencies (`Npgsql`, `OpenAI`, `Pgvector`, `Microsoft.Extensions.Logging.Abstractions`)

### 🚀 **PspRouter.API** (ASP.NET Core Web API)
- `Program.cs` – **ASP.NET Core** application with dependency injection, configuration, and service registration
- `Controllers/RoutingController.cs` – REST API endpoints for routing transactions and updating outcomes
- `Dummies.cs` – Mock implementations for local testing and development
- `appsettings.json` – Configuration file with logging and PSP router settings
- `PspRouter.API.csproj` – .NET 8 Web API with ASP.NET Core dependencies (`Microsoft.AspNetCore.OpenApi`, `Swashbuckle.AspNetCore`, etc.)

### 🧪 **PspRouter.Tests** (Unit Tests)
- `UnitTests.cs` – Unit test for core functionality (CapabilityMatrix, Bandit algorithms)
- `IntegrationTests.cs` – Integration tests demonstrating complete routing flow with learning
- `PspRouter.Tests.csproj` – .NET 8 test project with xUnit framework

### 🗄️ **Database & Configuration**
- `setup-database.sql` – PostgreSQL database setup script with pgvector extension

## 🏗️ Project Structure Benefits

### **📚 PspRouter.Lib (Core Library)**
- **Reusable Business Logic**: Core routing algorithms, bandit learning, and memory systems
- **Clean Interfaces**: Well-defined contracts for all services
- **Independent Testing**: Can be unit tested in isolation
- **NuGet Package Ready**: Can be packaged and distributed
- **Framework Agnostic**: No hosting dependencies, pure business logic

### **🚀 PspRouter.API (ASP.NET Core Web API)**
- **RESTful API**: Clean HTTP endpoints for routing transactions and updating outcomes
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
│   Bandit        │◀───│   Fallback       │◀───│   LLM Failed?   │
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

### 🎰 Bandit Learning Flow
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
- **🎰 Bandit Learning Flow**: Illustrates the contextual bandit learning and arm selection process
- **🧠 Vector Memory Flow**: Shows how lessons are stored and retrieved using semantic search
- **🔄 System Integration Flow**: Demonstrates the ASP.NET Core Web API architecture and dependency injection
- **🎯 Decision Tree Flow**: Provides a high-level view of the decision logic and fallback mechanisms

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- PostgreSQL with pgvector extension
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

### 🗄️ **Database Access Strategy: Raw SQL vs Entity Framework**
This project uses **raw SQL with Npgsql** instead of Entity Framework Core for several critical reasons:

#### **Why Raw SQL is Optimal Here:**

**🚀 Performance-Critical Operations:**
- **Vector Similarity Search**: Complex pgvector operations require optimized SQL
- **Analytics Queries**: Aggregation queries need precise control over execution plans
- **High-Throughput**: Payment routing requires sub-millisecond database response times

**🔧 Specialized Database Features:**
- **pgvector Extension**: Vector operations (`<=>`, `cosine similarity`) not well-supported in EF
- **Custom Indexes**: IVFFlat indexes for vector search require direct SQL control
- **PostgreSQL-Specific**: Advanced features like `FILTER` clauses and window functions

**📊 Complex Analytics Queries:**
```sql
-- Example: Calculate authorization rates per PSP
SELECT 
    psp_name,
    COUNT(*) FILTER (WHERE authorized = true) / COUNT(*) as auth_rate,
    AVG(processing_time_ms) as avg_processing_time
FROM transaction_outcomes 
WHERE processed_at >= NOW() - INTERVAL '30 days'
GROUP BY psp_name;
```

**⚡ Performance Benefits:**
- **Direct Control**: No ORM overhead or query translation
- **Optimized Queries**: Hand-tuned SQL for maximum performance
- **Connection Pooling**: Direct Npgsql connection management
- **Vector Operations**: Native pgvector support without abstraction layers

#### **Type Safety Without EF:**
We maintain type safety through:
- **Entity Classes (POCOs)**: Strongly-typed data models
- **Database Context Interface**: Clean abstraction over raw SQL
- **Parameterized Queries**: SQL injection protection
- **Compile-Time Checking**: Full IntelliSense and type validation

#### **When EF Would Be Problematic:**
- **Vector Operations**: EF doesn't understand pgvector syntax
- **Complex Analytics**: LINQ translation would be inefficient
- **Performance**: ORM overhead unacceptable for payment processing
- **Custom Indexes**: EF migrations don't handle specialized indexes well

#### **Best of Both Worlds:**
- ✅ **Type Safety**: Entity classes provide compile-time checking
- ✅ **Performance**: Raw SQL gives optimal database performance  
- ✅ **Flexibility**: Direct access to all PostgreSQL features
- ✅ **Maintainability**: Clean separation with database context interface
- ✅ **Security**: Parameterized queries prevent SQL injection

### 1. Database Setup

#### Install PostgreSQL with pgvector
```bash
# Install PostgreSQL (Ubuntu/Debian)
sudo apt update
sudo apt install postgresql postgresql-contrib

# Install pgvector extension
sudo apt install postgresql-16-pgvector  # Adjust version as needed

# Or using Docker
docker run --name psp-router-db -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=psp_router -p 5432:5432 -d pgvector/pgvector:pg16
```

#### Create Database and Run Setup
```sql
-- Connect to PostgreSQL as superuser
sudo -u postgres psql

-- Create database
CREATE DATABASE psp_router;

-- Create user (optional)
CREATE USER psp_router_user WITH PASSWORD 'your_password';
GRANT ALL PRIVILEGES ON DATABASE psp_router TO psp_router_user;
```

```bash
# Run the setup script
psql -U postgres -d psp_router -f setup-database.sql
```

#### Verify Setup
```sql
-- Connect to the database
psql -U postgres -d psp_router

-- Check if vector extension is installed
SELECT * FROM pg_extension WHERE extname = 'vector';

-- Check if tables are created
\dt

-- Check sample data
SELECT key, content FROM psp_lessons LIMIT 5;
```

### 2. Environment Configuration

```bash
# Windows (PowerShell)
$env:OPENAI_API_KEY="sk-your-openai-key"
$env:PGVECTOR_CONNSTR="Host=localhost;Username=postgres;Password=postgres;Database=psp_router"

# Linux/Mac
export OPENAI_API_KEY="sk-your-openai-key"
export PGVECTOR_CONNSTR="Host=localhost;Username=postgres;Password=postgres;Database=psp_router"
```

### 3. Configuration Management

The application uses hierarchical configuration with the following precedence (highest to lowest):

1. **Command Line Arguments**: `dotnet run -- --setting=value`
2. **Environment Variables**: `OPENAI_API_KEY`, `PGVECTOR_CONNSTR`
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
      "EmbeddingModel": "text-embedding-3-large"
    },
    "Bandit": {
      "Epsilon": 0.1,
      "Algorithm": "ContextualEpsilonGreedy"
    },
    "Database": {
      "TableName": "psp_lessons"
    }
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
    "Bandit": {
      "Epsilon": 0.05
    }
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
services.AddSingleton<OpenAIEmbeddings>(provider => new OpenAIEmbeddings(apiKey, model: "text-embedding-3-large"));
services.AddSingleton<IVectorMemory>(provider => new PgVectorMemory(pgConn, table: "psp_lessons"));
services.AddSingleton<IContextualBandit>(provider => new ContextualEpsilonGreedyBandit(epsilon: 0.1, logger: logger));

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
  POST /api/routing/outcome - Update transaction outcome
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

#### Update Transaction Outcome
```bash
POST /api/routing/outcome
Content-Type: application/json

{
  "decisionId": "decision-123",
  "pspName": "Adyen",
  "authorized": true,
  "transactionAmount": 150.00,
  "feeAmount": 3.30,
  "processingTimeMs": 1200,
  "riskScore": 15,
  "processedAt": "2024-12-01T12:00:00Z",
  "errorCode": null,
  "errorMessage": null
}
```

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

# Update transaction outcome
curl -X POST https://localhost:7000/api/routing/outcome \
  -H "Content-Type: application/json" \
  -d '{
    "decisionId": "decision-123",
    "pspName": "Adyen",
    "authorized": true,
    "transactionAmount": 150.00,
    "feeAmount": 3.30,
    "processingTimeMs": 1200,
    "riskScore": 15,
    "processedAt": "2024-12-01T12:00:00Z"
  }'
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
var router = new PspRouter(chatClient, healthProvider, feeProvider, tools, bandit, memory, logger);
var decision = await router.DecideAsync(context, cancellationToken);
```

#### Learning from Outcomes
```csharp
var outcome = new TransactionOutcome(/* transaction results */);
router.UpdateReward(decision, outcome);
```

## 🔧 Configuration

### Bandit Learning
```csharp
// Contextual Epsilon-Greedy (recommended)
var bandit = new ContextualEpsilonGreedyBandit(epsilon: 0.1, logger);

// Standard Epsilon-Greedy
var bandit = new EpsilonGreedyBandit(epsilon: 0.1);

// Thompson Sampling (Bayesian)
var bandit = new ThompsonSamplingBandit();
```

### LLM Configuration
```csharp
var chatClient = new OpenAIChatClient(apiKey, model: "gpt-4.1");
```

### Memory Configuration
```csharp
var memory = new PgVectorMemory(connectionString, table: "psp_lessons");
```

## 📊 Decision Factors

The system considers multiple factors in routing decisions:

1. **Compliance** (SCA/3DS requirements)
2. **Authorization Success Rates** (historical performance)
3. **Fee Optimization** (minimize transaction costs)
4. **Merchant Preferences** (configured preferences)
5. **Historical Performance** (vector memory insights)
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

1. **Route Decision**: LLM or bandit selects PSP
2. **Transaction Processing**: PSP processes payment
3. **Outcome Capture**: Success/failure, fees, timing
4. **Reward Calculation**: Multi-factor reward computation
5. **Learning Update**: Bandit algorithm updates
6. **Memory Storage**: Lessons stored in vector database
7. **Continuous Improvement**: Better decisions over time

## 🚀 Production Deployment

### Environment Variables for Production
```bash
# Production OpenAI API key
export OPENAI_API_KEY="sk-prod-your-key"

# Production database connection
export PGVECTOR_CONNSTR="Host=prod-db-host;Username=psp_router;Password=secure-password;Database=psp_router;Port=5432;SSL Mode=Require"
```

### Database Security
```sql
-- Create production user with limited privileges
CREATE USER psp_router_prod WITH PASSWORD 'secure_password';
GRANT CONNECT ON DATABASE psp_router TO psp_router_prod;
GRANT USAGE ON SCHEMA public TO psp_router_prod;
GRANT SELECT, INSERT, UPDATE, DELETE ON ALL TABLES IN SCHEMA public TO psp_router_prod;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO psp_router_prod;
```

### Scaling Considerations
- **Horizontal Scaling**: Stateless design supports multiple instances
- **Database Optimization**: Proper indexing for vector searches
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
Main routing engine with LLM and bandit integration.

##### `ContextualEpsilonGreedyBandit` (in `Bandit.cs`)
Enhanced contextual bandit with transaction feature awareness.

##### `EpsilonGreedyBandit` (in `Bandit.cs`)
Standard multi-armed bandit with epsilon-greedy exploration strategy.

##### `ThompsonSamplingBandit` (in `Bandit.cs`)
Bayesian multi-armed bandit with Thompson sampling.

##### `PgVectorMemory` (in `MemoryPgVector.cs`)
Vector database integration for semantic memory.

##### `OpenAIChatClient` (in `OpenAIChatClient.cs`)
LLM integration with tool calling support.

#### Key Methods

##### `DecideAsync(RouteContext, CancellationToken)`
Makes routing decision using LLM or fallback logic.

##### `UpdateReward(RouteDecision, TransactionOutcome)`
Updates bandit learning with transaction outcome.

### 🚀 PspRouter.API (ASP.NET Core Web API)

#### `Program.cs`
ASP.NET Core application with dependency injection and service registration.

#### `Controllers/RoutingController.cs`
REST API controller with endpoints for routing transactions and updating outcomes.

#### `DummyHealthProvider` & `DummyFeeProvider`
Mock implementations for local testing and development.

### 🧪 PspRouter.Tests (Unit Tests)

#### `PspRouterTests`
Test cases for core functionality including:
- CapabilityMatrix validation
- Bandit algorithm testing
- Contextual bandit feature handling

#### `IntegrationTests`
Integration tests demonstrating complete routing flow:
- End-to-end routing with LLM and bandit learning
- Transaction outcome simulation and learning updates
- Complete system integration testing

#### `AddAsync(string, string, Dictionary, float[], CancellationToken)`
Adds lesson to vector memory.

#### `SearchAsync(float[], int, CancellationToken)`
Searches vector memory for relevant lessons.


---

## 📚 **Additional Documentation**

For detailed explanations of the AI/ML concepts used in this system, see:
- **[AI-ML-CONCEPTS.md](AI-ML-CONCEPTS.md)** - Comprehensive guide to Multi-Armed Bandits, LLM, Vector Search, and other AI/ML concepts explained for programmers

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
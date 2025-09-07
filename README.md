# 🚀 PSP Router - Intelligent Payment Routing System

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

### 🏗️ **Generic Host Architecture**
- **Enterprise-Grade Hosting**: Built on .NET Generic Host for production deployment
- **Dependency Injection**: Automatic service lifetime management and disposal
- **Configuration Management**: Hierarchical configuration with JSON, environment variables, and command line
- **Service Registration**: Clean separation of concerns with proper service lifetimes
- **Graceful Shutdown**: Proper application lifecycle management
- **Extensibility**: Easy to add hosted services, health checks, and background tasks

### 📊 **Comprehensive Monitoring**
- **Structured Logging**: Detailed logging with Microsoft.Extensions.Logging
- **Performance Metrics**: Processing time, success rates, fee optimization
- **Audit Trail**: Complete decision history with reasoning
- **Error Tracking**: Comprehensive error handling and reporting
- **Host Integration**: Logging integrated with .NET hosting infrastructure

## 🏗 Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                    .NET Generic Host                            │
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
│   Transaction   │───▶│   PSP Router     │───▶│   Decision      │
│   Input         │    │   (Scoped)       │     │   Output        │
└─────────────────┘    └──────────────────┘     └─────────────────┘
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

## 🧠 LLM's Role in PSP Routing

### **Primary Function: Intelligent Decision Engine**

The LLM serves as the **brain** of the PSP Router, acting as an expert payment routing consultant that makes complex, context-aware decisions. It's the primary decision maker that considers multiple factors simultaneously and provides reasoning for its choices.

### **Decision Flow: LLM vs Bandit**

```
Transaction Request
        ↓
    [Guardrails] ← Compliance checks, health status
        ↓
    [LLM Decision] ← Primary intelligent routing
        ↓
    [Fallback] ← Bandit learning if LLM fails
        ↓
    [Final Decision]
```

### **What the LLM Receives**

The LLM gets comprehensive context for each decision:

```json
{
  "Transaction": {
    "MerchantId": "M123",
    "Amount": 150.00,
    "Currency": "USD", 
    "Method": "Card",
    "Scheme": "Visa",
    "SCARequired": false,
    "RiskScore": 18
  },
  "Candidates": [
    {
      "Name": "Adyen",
      "Health": "green",
      "AuthRate30d": 0.89,
      "FeeBps": 200,
      "Supports3DS": true
    }
  ],
  "MerchantPreferences": {
    "prefer_low_fees": "true"
  },
  "SegmentStats": {
    "Adyen_USD_Visa_auth": 0.89,
    "Stripe_USD_Visa_auth": 0.87
  },
  "RelevantLessons": [
    "USD Visa transactions work well with Adyen for low-risk merchants"
  ]
}
```

### **LLM's Decision Process**

#### **1. Context Analysis**
- Transaction characteristics (amount, risk, currency, method)
- PSP capabilities and real-time health status
- Merchant preferences and business rules
- Historical performance data from bandit learning
- Relevant lessons from vector memory

#### **2. Business Logic Application**
- **Compliance Enforcement**: SCA/3DS requirements, payment method support
- **Health Checks**: Only routes to healthy PSPs
- **Preference Handling**: Considers merchant fee vs auth rate preferences
- **Risk Management**: Applies risk-based routing rules

#### **3. Intelligent Reasoning**
The LLM can handle complex scenarios like:
- "High-risk transaction needs 3DS support"
- "Merchant prefers low fees, but auth rate is more important for this amount"
- "Historical data shows Stripe performs better for SCA transactions"
- "Weekend transactions have different patterns"

#### **4. Structured Decision Output**
```json
{
  "Candidate": "Stripe",
  "Reasoning": "Chose Stripe over Adyen because: 1) Transaction requires SCA and Stripe has better 3DS performance, 2) Historical data shows 3% higher auth rate for SCA transactions, 3) Fee difference is minimal for this amount",
  "Features_Used": ["sca_required=true", "auth_rate=0.87", "fee_bps=200"]
}
```

### **Advanced LLM Capabilities**

#### **Tool Calling for Real-Time Data**
```csharp
// LLM can call external APIs for current information
var tools = new List<IAgentTool>
{
    new GetHealthTool(health),      // Get current PSP health status
    new GetFeeQuoteTool(fees, tx)   // Get real-time fee quotes
};
```

#### **Memory Integration**
```csharp
// LLM uses vector memory for contextual lessons
var lessons = await GetRelevantLessonsAsync(ctx, ct);
// Example: "Similar transactions: Adyen failed for high-risk SCA, Stripe succeeded"
```

#### **Complex Scenario Handling**
The LLM excels at handling edge cases:
- **Compliance Conflicts**: "Merchant has 3D Secure preference but transaction is low-risk"
- **Trade-off Analysis**: "Fee difference is $0.50 but auth rate difference is 5%"
- **Temporal Patterns**: "Historical data shows weekend transactions perform differently"
- **Multi-factor Decisions**: Balancing auth rates, fees, compliance, and preferences

### **LLM vs Bandit: When Each is Used**

#### **LLM is Primary When:**
- ✅ **Complex context** needs analysis
- ✅ **Business rules** must be applied
- ✅ **Compliance** requirements exist
- ✅ **Merchant preferences** matter
- ✅ **Historical lessons** are relevant
- ✅ **Edge cases** need expert reasoning

#### **Bandit is Fallback When:**
- ⚠️ **LLM is unavailable** (API failure, timeout)
- ⚠️ **LLM response is invalid** (parsing error)
- ⚠️ **Simple scenarios** where bandit is sufficient
- ⚠️ **High-volume** transactions where speed matters

### **Learning Integration**

#### **1. Feeds Bandit Learning**
```csharp
// LLM decisions become training data for bandit
var outcome = ProcessTransaction(decision);
router.UpdateReward(decision, outcome); // Updates bandit statistics
```

#### **2. Learns from Bandit Data**
```csharp
// LLM uses bandit statistics in its decisions
"SegmentStats": {
    "Adyen_USD_Visa_auth": 0.89,  // From bandit learning
    "Stripe_USD_Visa_auth": 0.87
}
```

#### **3. Creates Memory Lessons**
```csharp
// LLM decisions become lessons for future use
var lesson = $"Transaction {tx.Amount} {tx.Currency}: {decision.Candidate} succeeded because {decision.Reasoning}";
await memory.AddAsync(lesson, embedding);
```

### **Real-World Example**

#### **Scenario**: $500 USD Visa, risk=40, SCA required, merchant prefers low fees

**LLM's Analysis:**
```
1. Compliance: SCA required → Need 3DS support
2. Context: High amount + high risk + SCA = complex scenario  
3. Preferences: Low fees vs auth success trade-off
4. History: "High-risk SCA transactions: Stripe 92% vs Adyen 85%"
5. Reasoning: "Fee difference is $2.50, but auth rate difference is 7%"
6. Decision: Choose Stripe (auth success more valuable than fee savings)
```

**LLM Response:**
```json
{
  "Candidate": "Stripe",
  "Reasoning": "Selected Stripe for high-risk SCA transaction. While Adyen has lower fees ($2.50 difference), Stripe's 7% higher auth rate for SCA transactions provides better overall value. Historical data shows 92% vs 85% success rate for similar transactions.",
  "Features_Used": ["sca_required=true", "risk_score=40", "amount=500", "auth_rate=0.92"]
}
```

### **System Prompt Engineering**

The LLM uses a carefully crafted system prompt that defines its role:

```
You are an expert payment service provider (PSP) routing system. Your job is to select the optimal PSP for each transaction to maximize authorization success, minimize fees, and ensure compliance.

CRITICAL RULES:
1. NEVER route to a PSP that doesn't support the payment method
2. ALWAYS enforce SCA/3DS requirements when specified
3. Consider authorization rates, fees, and merchant preferences
4. Use historical lessons to inform decisions
5. Provide clear reasoning for your choice

DECISION FACTORS (in order of importance):
1. Compliance (SCA/3DS requirements)
2. Authorization success rates
3. Fee optimization
4. Merchant preferences
5. Historical performance
```

### **Summary: LLM's Value**

The LLM transforms the PSP Router from a simple statistical system into an **intelligent payment expert** that:

1. **🧠 Thinks**: Analyzes complex scenarios with multiple factors
2. **📋 Applies Rules**: Enforces compliance and business logic
3. **📚 Learns**: Uses historical lessons and bandit data
4. **🔧 Adapts**: Handles edge cases and complex scenarios
5. **📊 Explains**: Provides clear reasoning for decisions
6. **🛡️ Ensures Safety**: Fallback to bandit when needed

This makes the system capable of handling real-world payment routing scenarios that require expert-level decision making.

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

## 🧠 **Vector Database: The System's Memory**

### **Role and Purpose**
The vector database serves as the **"memory" of the PSP Router system**, providing the LLM with historical context and lessons learned from past routing decisions. It acts like a human expert's experience - when making a routing decision, the system can recall similar past situations and their outcomes to make more informed choices.

### **How It Works**

#### **1. Learning Phase (After Each Transaction)**
```
Transaction Outcome → Generate Embedding → Store in Vector DB
```
- **Captures Lessons**: After each transaction completes, the system captures the outcome
- **Creates Embeddings**: Uses OpenAI embeddings to convert the transaction context and outcome into a vector representation
- **Stores Knowledge**: Saves the embedding along with metadata (PSP used, success/failure, fees, etc.) in PostgreSQL with pgvector

#### **2. Decision Phase (During Routing)**
```
New Transaction → Generate Query Embedding → Search Similar Contexts → Retrieve Lessons → Provide to LLM
```
- **Query Generation**: Creates an embedding for the current transaction context
- **Semantic Search**: Finds similar past transactions using cosine similarity
- **Lesson Retrieval**: Returns the most relevant historical lessons
- **LLM Enhancement**: Provides these lessons as context to the LLM for better decision-making

### **Key Benefits**

#### **🧠 Human-Like Decision Making**
- **Pattern Recognition**: Identifies patterns in successful/failed transactions
- **Contextual Learning**: Learns from similar situations (same merchant, country, payment method, etc.)
- **Experience Accumulation**: Builds institutional knowledge over time

#### **📈 Continuous Improvement**
- **Adaptive Learning**: System gets smarter with each transaction
- **Historical Insights**: Leverages months/years of routing experience
- **Contextual Awareness**: Considers nuanced factors that simple rules might miss

#### **🎯 Enhanced Accuracy**
- **Better Decisions**: LLM makes more informed choices with historical context
- **Reduced Errors**: Avoids repeating past mistakes
- **Optimized Routing**: Learns which PSPs work best in specific scenarios

### **Technical Implementation**

#### **Database Schema**
```sql
CREATE TABLE psp_lessons (
    key TEXT PRIMARY KEY,
    content TEXT NOT NULL,           -- Transaction context and outcome
    meta JSONB NOT NULL,             -- Metadata (PSP, fees, success, etc.)
    embedding vector(3072)           -- OpenAI embedding vector
);
```

#### **Search Process**
```csharp
// 1. Generate query embedding
var query = $"PSP routing for {currency} {method} {scheme} merchant {country}";
var queryEmbedding = await embeddings.CreateEmbeddingAsync(query, ct);

// 2. Search for similar contexts
var results = await memory.SearchAsync(queryEmbedding, k: 5, ct);

// 3. Provide lessons to LLM
var contextJson = JsonSerializer.Serialize(new {
    Transaction = currentTx,
    Candidates = validCandidates,
    RelevantLessons = results.Select(r => r.text).ToList()  // Historical insights
});
```

### **Vector Memory Flow**
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Transaction   │───▶│   Generate       │───▶│   Store in      │
│   Outcome       │    │   Embedding      │    │   pgvector      │
└─────────────────┘    └──────────────────┘    └─────────────────┘
                                                        │
                                                        ▼
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Retrieve      │◀───│   Semantic       │◀───│   Query         │
│   Lessons       │    │   Search         │    │   Similar       │
│   Learned       │    │   (Cosine)       │    │   Contexts      │
└─────────────────┘    └──────────────────┘    └─────────────────┘
        │                        │
        ▼                        ▼
┌─────────────────┐    ┌──────────────────┐
│   Apply         │    │   Contextual     │
│   Insights      │    │   Knowledge      │
└─────────────────┘    └──────────────────┘
```

### **Important Distinction**
- **Vector DB is FOR the LLM**: Used exclusively to enhance LLM decision-making
- **NOT a Fallback**: The deterministic fallback system does not use vector memory
- **Memory vs. Logic**: Vector DB provides historical context, not routing logic

## 🧠 **OpenAI Embeddings: The Magic Behind Semantic Search**

### **What are OpenAI Embeddings?**
OpenAI embeddings are **numerical representations of text** that capture semantic meaning. They convert words, sentences, or documents into high-dimensional vectors (arrays of numbers) that can be used for similarity comparisons and semantic search.

### **How They Work**
```
Text Input → OpenAI API → Vector Output (array of numbers)
"Payment routing for Visa cards" → [0.1, -0.3, 0.8, 0.2, ...] (3072 numbers)
```

### **Key Properties**

#### **🎯 Semantic Similarity**
- **Similar texts** produce **similar vectors**
- **Different texts** produce **different vectors**
- **Meaning matters more than exact words**

#### **📊 Dimensionality**
- **OpenAI embeddings**: 3072 dimensions (numbers) for `text-embedding-3-large`
- **Each dimension** represents some aspect of meaning
- **High-dimensional space** allows for nuanced representations

#### **🔢 Mathematical Operations**
- **Cosine similarity**: Measure how similar two texts are
- **Vector arithmetic**: Can find relationships between concepts
- **Distance calculations**: Find nearest neighbors in meaning space

### **Real-World Example in PSP Router**

#### **Scenario: Learning from Past Transactions**

**1. Store a Lesson**
```csharp
// After a successful transaction
var lesson = "Visa card from US merchant, $150, routed to Stripe, authorized in 2.3s";
var embedding = await embeddings.CreateEmbeddingAsync(lesson, ct);
await memory.AddAsync("lesson_001", lesson, metadata, embedding, ct);
```

**2. Search for Similar Contexts**
```csharp
// New transaction: "Visa card from US merchant, $200"
var query = "Visa card from US merchant, $200";
var queryEmbedding = await embeddings.CreateEmbeddingAsync(query, ct);
var results = await memory.SearchAsync(queryEmbedding, k: 3, ct);

// Results might include:
// 1. "Visa card from US merchant, $150, routed to Stripe, authorized in 2.3s" (similarity: 0.95)
// 2. "Visa card from US merchant, $300, routed to Stripe, authorized in 1.8s" (similarity: 0.92)
// 3. "Visa card from US merchant, $100, routed to Adyen, authorized in 3.1s" (similarity: 0.88)
```

**3. Provide Context to LLM**
```csharp
var contextJson = JsonSerializer.Serialize(new {
    Transaction = currentTx,
    Candidates = validCandidates,
    RelevantLessons = results.Select(r => r.text).ToList()
    // LLM gets: "Based on similar past transactions, Stripe has been successful for US Visa cards"
});
```

### **Why Embeddings are Game-Changing**

#### **Traditional Approach (Exact Matching)**
```
Query: "Visa card from US merchant, $200"
Search: Only finds exact matches
Result: No results found
```

#### **Embedding Approach (Semantic Matching)**
```
Query: "Visa card from US merchant, $200"
Search: Finds semantically similar transactions
Result: "Visa card from US merchant, $150, routed to Stripe, authorized in 2.3s"
```

### **Benefits for PSP Router**

1. **🧠 Semantic Understanding**: Finds relevant lessons even with different wording
2. **📈 Contextual Learning**: Learns from similar situations, not just exact matches
3. **🔍 Pattern Recognition**: Identifies successful routing patterns across different scenarios
4. **🚀 Continuous Improvement**: System gets smarter as it accumulates more lessons
5. **👨‍💼 Human-Like Reasoning**: Mimics how human experts recall similar past experiences

### **Technical Implementation**

#### **OpenAI Embedding Models**
- **text-embedding-3-large**: 3072 dimensions (used in PSP Router)
- **text-embedding-3-small**: 1536 dimensions
- **text-embedding-ada-002**: 1536 dimensions (older model)

#### **Database Storage**
```sql
CREATE TABLE psp_lessons (
    key TEXT PRIMARY KEY,
    content TEXT NOT NULL,           -- Original text
    meta JSONB NOT NULL,             -- Metadata
    embedding vector(3072)           -- OpenAI embedding vector
);
```

#### **Similarity Search**
```sql
-- Find most similar lessons using cosine similarity
SELECT content, meta, 1 - (embedding <=> @query_embedding) AS similarity
FROM psp_lessons
ORDER BY embedding <=> @query_embedding
LIMIT 5;
```

### **In Summary**
OpenAI embeddings are the **"translation layer"** that converts human language into mathematical representations that computers can understand and compare. In the PSP Router, they enable the system to find relevant historical lessons based on meaning, not just exact text matches - making the routing decisions much more intelligent and contextually aware! 🧠✨

### 🔄 System Integration Flow
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Generic Host  │───▶│   Dependency     │───▶│   Service       │
│   Application   │    │   Injection      │    │   Registration  │
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
│   Environment   │    │   Graceful       │
│   Variables     │    │   Shutdown       │
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
- **🔄 System Integration Flow**: Demonstrates the Generic Host architecture and dependency injection
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

### 🏗️ **Generic Host Benefits**
The application uses .NET Generic Host, providing:
- **Automatic Dependency Injection**: Services are automatically registered and managed
- **Configuration Management**: JSON + environment variables with hierarchical config
- **Structured Logging**: Built-in logging with multiple providers
- **Graceful Shutdown**: Proper application lifecycle management
- **Production Ready**: Enterprise-grade hosting infrastructure
- **Extensibility**: Easy to add hosted services, health checks, and background tasks
- **Service Lifetime Management**: Automatic disposal and cleanup of resources
- **Environment Support**: Development, Staging, Production configurations
- **Command Line Integration**: Built-in support for command line arguments
- **Hosting Flexibility**: Can be deployed as console app, Windows service, or container

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

## 🔍 Monitoring & Observability

### Logging Levels
- **Information**: Routing decisions, learning updates
- **Warning**: Fallback scenarios, degraded performance
- **Error**: LLM failures, database issues, parsing errors
- **Debug**: Detailed decision reasoning, memory operations

### Key Metrics
- **Authorization Success Rate**: Per PSP, per segment
- **Average Processing Time**: Response time optimization
- **Fee Optimization**: Cost reduction over time
- **Learning Convergence**: Bandit algorithm performance

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

## 📦 Packaging & Distribution

### Creating NuGet Package

The `PspRouter.Lib` can be packaged as a NuGet package for distribution:

```bash
# Build the library in Release mode
dotnet build PspRouter.Lib --configuration Release

# Create NuGet package
dotnet pack PspRouter.Lib --configuration Release

# The package will be created in:
# PspRouter.Lib/bin/Release/PspRouter.Lib.1.0.0.nupkg
```

### Using as NuGet Package

Other projects can reference the PSP Router library:

```xml
<PackageReference Include="PspRouter.Lib" Version="1.0.0" />
```

```csharp
using PspRouter.Lib;

// Use the library in your application
var router = new PspRouter(chatClient, healthProvider, feeProvider, tools, bandit, memory, logger);
```

### Library Dependencies

The library has minimal external dependencies:
- `Npgsql` - PostgreSQL client
- `OpenAI` - OpenAI API client  
- `Pgvector` - Vector database support
- `Microsoft.Extensions.Logging.Abstractions` - Logging interfaces

### Integration Examples

#### ASP.NET Core Web API
```csharp
// In Startup.cs or Program.cs
services.AddScoped<PspRouter.Lib.PspRouter>();
services.AddSingleton<IHealthProvider, YourHealthProvider>();
services.AddSingleton<IFeeQuoteProvider, YourFeeProvider>();
```

#### Azure Functions
```csharp
[FunctionName("RoutePayment")]
public async Task<IActionResult> RoutePayment(
    [HttpTrigger(AuthorizationLevel.Function, "post")] HttpRequest req)
{
    var router = serviceProvider.GetRequiredService<PspRouter.Lib.PspRouter>();
    // Use router for payment routing
}
```

#### Console Application
```csharp
// Direct instantiation
var router = new PspRouter.Lib.PspRouter(chatClient, healthProvider, feeProvider, tools, bandit, memory, logger);
```

## 🔧 Customization

### External Bandit Libraries
For production use, consider integrating with industry-standard libraries:

```csharp
// VowpalWabbit (requires .NET Framework compatibility)
// Install-Package VowpalWabbit
var bandit = new VowpalWabbitBandit();

// Accord.NET (machine learning framework)
// Install-Package Accord.MachineLearning
// Custom implementation using Accord's algorithms

// ML.NET (Microsoft's machine learning framework)
// Install-Package Microsoft.ML
// Custom contextual bandit implementation
```

### Adding New PSPs
1. Update `CapabilityMatrix.Supports()` method
2. Implement health and fee providers
3. Add PSP-specific logic to reward calculation
4. Update system prompts with new PSP characteristics

### Custom Reward Functions
```csharp
private static double CalculateCustomReward(TransactionOutcome outcome)
{
    // Your custom reward logic here
    return customReward;
}
```

### Enhanced Memory Integration
```csharp
private async Task<List<string>> GetRelevantLessonsAsync(RouteContext ctx, CancellationToken ct)
{
    // Implement full embedding-based search
    var queryEmbedding = await _embeddings.EmbedAsync(query, ct);
    var results = await _memory.SearchAsync(queryEmbedding, k: 5, ct);
    return results.Select(r => r.text).ToList();
}
```

## 📈 Performance Optimization

### Caching Strategy
- **PSP Health**: Cache health status for 30 seconds
- **Fee Tables**: Cache fee data for 5 minutes
- **Memory Results**: Cache vector search results
- **Bandit State**: In-memory bandit statistics

### Database Optimization
```sql
-- Optimize vector search performance
CREATE INDEX CONCURRENTLY psp_lessons_embedding_idx 
ON psp_lessons USING ivfflat (embedding vector_cosine_ops) 
WITH (lists = 100);

-- Analyze tables for better query planning
ANALYZE psp_lessons;
ANALYZE transaction_outcomes;
```

### Connection Pooling
```csharp
// In your connection string, add pooling parameters
"Host=localhost;Username=postgres;Password=postgres;Database=psp_router;Pooling=true;MinPoolSize=5;MaxPoolSize=20"
```

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

## 🛠 Replace Dummies
- Implement `IHealthProvider` and `IFeeQuoteProvider` with your metrics/config.
- Swap in your own stats for `AuthRate30d` and pass them in `RouteContext`.

## 🔧 Tuning
- Adjust embedding model & `vector(N)` dimension in `MemoryPgVector.cs` to match your chosen model.
- Configure model name in `OpenAIChatClient` (default: `gpt-4.1`).

## 🚨 Troubleshooting

### Common Issues

#### 1. Database Connection Failed
```
Error: 3D000: database "psp_router" does not exist
```
**Solution**: Create the database using the setup script

#### 2. Vector Extension Not Found
```
Error: extension "vector" does not exist
```
**Solution**: Install pgvector extension
```bash
sudo apt install postgresql-16-pgvector
```

#### 3. OpenAI API Key Invalid
```
Error: Invalid API key
```
**Solution**: Verify your API key and ensure it has sufficient credits

#### 4. Permission Denied
```
Error: permission denied for table psp_lessons
```
**Solution**: Grant proper permissions to your database user

### Debug Mode
```bash
# Enable debug logging
export DOTNET_ENVIRONMENT=Development
dotnet run
```

## 🔒 Security Considerations

### 1. API Key Security
- Never commit API keys to version control
- Use environment variables or secure key management
- Rotate keys regularly

### 2. Database Security
- Use strong passwords
- Enable SSL connections in production
- Restrict database access by IP
- Regular security updates

### 3. Network Security
- Use VPN or private networks for database access
- Implement rate limiting
- Monitor for suspicious activity

## 💾 Backup and Recovery

### 1. Database Backup
```bash
# Create backup
pg_dump -U postgres -h localhost psp_router > psp_router_backup.sql

# Restore backup
psql -U postgres -h localhost psp_router < psp_router_backup.sql
```

### 2. Automated Backups
```bash
# Add to crontab for daily backups
0 2 * * * pg_dump -U postgres psp_router | gzip > /backups/psp_router_$(date +\%Y\%m\%d).sql.gz
```

## 🤝 Contributing

1. Fork the repository
2. Create feature branch
3. Add tests for new functionality
4. Ensure all tests pass
5. Submit pull request

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🆘 Support

For questions and support:
- Create an issue in the repository
- Check the troubleshooting section
- Review the logs for error details
- Check the documentation

---

## 📚 Appendix: Bandit Learning Deep Dive

### A.1 Understanding Multi-Armed Bandits

#### What is a Multi-Armed Bandit?
A multi-armed bandit is a machine learning problem where an agent must choose between multiple actions (arms) to maximize cumulative reward over time. The name comes from slot machines (one-armed bandits) - imagine choosing between multiple slot machines to maximize winnings.

#### Key Concepts
- **Arms**: Available choices (in our case: Adyen, Stripe, Klarna, PayPal)
- **Reward**: Outcome of choosing an arm (authorization success, fee optimization)
- **Exploration vs Exploitation**: Balance between trying new arms vs using known good arms
- **Regret**: Difference between optimal choice and actual choice

### A.2 Bandit Algorithms Explained

#### Epsilon-Greedy Algorithm
```csharp
// Simple epsilon-greedy logic
if (random < epsilon)
    return random_arm();  // Explore (10% of time)
else
    return best_known_arm();  // Exploit (90% of time)
```

**How it works:**
- **Exploration (ε%)**: Randomly try different PSPs to learn
- **Exploitation (1-ε%)**: Use the PSP with highest success rate
- **Learning**: Update success rates after each transaction

**Example:**
```
ε = 0.1 (10% exploration)
- 90% of time: Choose Adyen (best known)
- 10% of time: Randomly try Stripe/Klarna/PayPal
```

#### Thompson Sampling Algorithm
```csharp
// Bayesian approach
for each arm:
    sample = beta_distribution(alpha, beta)
    if sample > best_sample:
        best_arm = arm
return best_arm
```

**How it works:**
- **Bayesian**: Uses probability distributions instead of averages
- **Uncertainty**: Naturally balances exploration/exploitation based on confidence
- **Adaptive**: More exploration when uncertain, less when confident

### A.3 Contextual Bandits: The Game Changer

#### Traditional vs Contextual Bandits

**Traditional Bandit:**
```
Segment: "US_USD_Visa"
Decision: Always choose Adyen (89% success rate)
```

**Contextual Bandit:**
```
Segment: "US_USD_Visa"
Context: {amount: 500, risk: 30, sca: true}
Decision: Choose Stripe (better for high-risk SCA transactions)
```

#### Context Feature Engineering

Our implementation extracts features from transaction context:

```csharp
// Transaction context
var context = {
    "amount": 150.00m,      // → 150.0
    "risk_score": 25,       // → 25.0
    "sca_required": true,   // → 1.0
    "currency": "USD",      // → 0.123 (hash-based)
    "merchant_tier": "premium" // → 0.456 (hash-based)
};

// Extracted features
var features = {
    "amount": 150.0,
    "risk_score": 25.0,
    "sca_required": 1.0,
    "currency": 0.123,
    "merchant_tier": 0.456
};
```

#### Contextual Scoring Algorithm

```csharp
score = base_success_rate + contextual_bonus

// Base success rate
base_success_rate = total_rewards / total_attempts

// Contextual bonus
contextual_bonus = Σ(similarity * 0.1)
similarity = 1.0 - |context_value - arm_learned_value|
```

**Example Calculation:**
```
Adyen for "US_USD_Visa":
- Base rate: 0.89
- Learned features: {amount: 200, risk: 20, sca: 0.3}
- Current context: {amount: 150, risk: 25, sca: 1.0}

Similarity calculations:
- amount: 1.0 - |150-200|/max(150,200) = 0.75
- risk: 1.0 - |25-20|/max(25,20) = 0.8
- sca: 1.0 - |1.0-0.3| = 0.3

Contextual bonus: (0.75 + 0.8 + 0.3) * 0.1 = 0.185
Final score: 0.89 + 0.185 = 1.075
```

### A.4 Learning Process Deep Dive

#### 1. Initial State (Cold Start)
```
All PSPs start with no data:
Adyen: {sum: 0, count: 0, features: {}}
Stripe: {sum: 0, count: 0, features: {}}
```

#### 2. First Transactions (Exploration)
```
Transaction 1: $100, risk=10, sca=false
→ Random selection: Adyen
→ Outcome: Success (reward=1.0)
→ Update: Adyen {sum: 1.0, count: 1, features: {amount: 100, risk: 10, sca: 0}}

Transaction 2: $200, risk=30, sca=true  
→ Random selection: Stripe
→ Outcome: Success (reward=0.8) // Lower due to fees
→ Update: Stripe {sum: 0.8, count: 1, features: {amount: 200, risk: 30, sca: 1}}
```

#### 3. Learning Phase (Exploitation with Exploration)
```
Transaction 3: $150, risk=20, sca=false
→ Context: {amount: 150, risk: 20, sca: 0}
→ Adyen score: 1.0 + similarity_bonus = 1.05
→ Stripe score: 0.8 + similarity_bonus = 0.85
→ Choose: Adyen (exploitation)
```

#### 4. Mature Learning (Smart Decisions)
```
After 1000+ transactions:
Adyen: {sum: 850, count: 1000, features: {amount: 180, risk: 15, sca: 0.2}}
Stripe: {sum: 780, count: 1000, features: {amount: 220, risk: 25, sca: 0.6}}

Transaction: $300, risk=40, sca=true
→ Context: {amount: 300, risk: 40, sca: 1}
→ Adyen: 0.85 + low_similarity_bonus = 0.87
→ Stripe: 0.78 + high_similarity_bonus = 0.92
→ Choose: Stripe (contextual decision!)
```

### A.5 Reward Function Design

#### Multi-Factor Reward Calculation
```csharp
reward = base_auth_reward - fee_penalty + speed_bonus - risk_penalty

// Base reward
base_auth_reward = authorized ? 1.0 : 0.0

// Fee penalty (normalized)
fee_penalty = fee_amount / transaction_amount

// Speed bonus
speed_bonus = processing_time < 1000ms ? 0.1 : 0.0

// Risk penalty
risk_penalty = risk_score > 50 ? 0.2 : 0.0

// Clamp to [-1, 1]
final_reward = max(-1.0, min(1.0, reward))
```

#### Example Reward Calculations
```
Transaction 1: $100, authorized=true, fee=$2.30, time=800ms, risk=20
reward = 1.0 - 0.023 + 0.1 - 0.0 = 1.077 → 1.0

Transaction 2: $100, authorized=false, fee=$0, time=2000ms, risk=60  
reward = 0.0 - 0.0 + 0.0 - 0.2 = -0.2

Transaction 3: $1000, authorized=true, fee=$20.30, time=1200ms, risk=30
reward = 1.0 - 0.0203 + 0.0 - 0.0 = 0.9797
```

### A.6 Performance Characteristics

#### Convergence Analysis
```
Transactions 1-100:    High exploration, learning patterns
Transactions 100-500:  Balanced exploration/exploitation  
Transactions 500+:     Mostly exploitation, fine-tuning
```

#### Memory Usage
```
Per segment: ~1KB (statistics + features)
1000 segments: ~1MB total
Features per arm: ~100 bytes
```

#### Computational Complexity
```
Selection: O(arms × features) = O(4 × 10) = O(40) operations
Update: O(features) = O(10) operations
Total: Very fast, suitable for real-time routing
```

### A.7 Advanced Features

#### Feature Decay (Optional Enhancement)
```csharp
// Gradually forget old patterns
foreach (feature in arm_features):
    feature.value = feature.value * 0.99  // 1% decay per update
```

#### Segment Hierarchies (Optional Enhancement)
```csharp
// Learn at multiple levels
Global: All transactions
Country: US transactions  
Currency: US USD transactions
Scheme: US USD Visa transactions
```

#### Confidence Intervals (Optional Enhancement)
```csharp
// Track uncertainty
confidence = 1.0 / sqrt(count)
if confidence > threshold:
    explore_more()
```

### A.8 Comparison with External Libraries

#### VowpalWabbit Advantages
- **Advanced algorithms**: LinUCB, Neural Bandits
- **Feature interactions**: Automatic feature combinations
- **Online learning**: Continuous model updates
- **Industry proven**: Used by major companies

#### Our Implementation Advantages  
- **Customizable**: Easy to modify for PSP routing
- **Lightweight**: No external dependencies
- **Fast**: Optimized for our use case
- **Debuggable**: Full control over algorithm

#### When to Use Each
```
Use Our Implementation When:
- Transaction volume < 1M/day
- Simple feature interactions sufficient
- Need full control over algorithm
- Want minimal dependencies

Use VowpalWabbit When:
- Transaction volume > 1M/day  
- Need advanced ML features
- Complex feature interactions required
- Want industry-standard algorithms
```

### A.9 Monitoring and Debugging

#### Key Metrics to Track
```csharp
// Learning metrics
exploration_rate = exploration_decisions / total_decisions
convergence_rate = change_in_scores / time_period
regret = optimal_reward - actual_reward

// Performance metrics  
auth_rate_by_psp = successes / attempts
fee_optimization = total_fees_saved / total_volume
decision_time = average_routing_time
```

#### Debugging Tools
```csharp
// Log bandit decisions
logger.LogDebug("Bandit selected {PSP} with score {Score} for context {Context}", 
    selectedPSP, score, context);

// Track learning progress
logger.LogInformation("Segment {Segment} learning: {Stats}", 
    segmentKey, armStatistics);
```

---

## 🎯 **3-Project Architecture Summary**

The PSP Router has been successfully restructured into a professional 3-project solution:

### **✅ What We Achieved:**
- **🏗️ Clean Architecture**: Separation of concerns with Library, Application, and Tests
- **📚 Reusable Library**: Core business logic can be used by other applications
- **🧪 Comprehensive Testing**: Isolated unit tests for all core functionality
- **🚀 Production Ready**: Generic Host application with enterprise-grade hosting
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
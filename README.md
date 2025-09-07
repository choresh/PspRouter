# 🚀 Enhanced PSP Router - Intelligent Payment Routing System

## 🎯 Purpose
Decide the optimal PSP (Adyen / Stripe / Klarna / PayPal) per transaction to maximize auth success, minimize fees, and maintain compliance & reliability using **LLM-based decision making**, **multi-armed bandit learning**, and **vector memory**.

## 🏗 Solution Overview
- Deterministic **guardrails** (capabilities, SCA/3DS, health).
- **LLM decision engine** with tool calling.
- **Bandit learning** (ε-greedy / Thompson).
- **pgvector memory** for "lessons learned."
- Deterministic **fallback** scoring when LLM is unavailable.

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

### 📊 **Comprehensive Monitoring**
- **Structured Logging**: Detailed logging with Microsoft.Extensions.Logging
- **Performance Metrics**: Processing time, success rates, fee optimization
- **Audit Trail**: Complete decision history with reasoning
- **Error Tracking**: Comprehensive error handling and reporting

## 🏗 Architecture

```
┌─────────────────┐    ┌──────────────────┐     ┌─────────────────┐
│   Transaction   │───▶│   PSP Router     │───▶│   Decision      │
│   Input         │    │                  │     │   Output        │
└─────────────────┘    └──────────────────┘     └─────────────────┘
                              │
                              ▼
                    ┌──────────────────┐
                    │   Learning       │
                    │   Components     │
                    └──────────────────┘
                              │
                    ┌─────────┴─────────┐
                    ▼                   ▼
            ┌─────────────┐    ┌─────────────┐
            │   Bandit    │    │   Vector    │
            │   Learning  │    │   Memory    │
            └─────────────┘    └─────────────┘
```

## 📦 Project Layout
- `Program.cs` – minimal runnable example wiring **OpenAIChatClient**, tools, **PgVectorMemory**, and **OpenAIEmbeddings**.
- `Router.cs` – decision engine (LLM first, fallback on parse/errors).
- `DTOs.cs` – contracts.
- `Interfaces.cs` – abstractions for clients/providers/tools.
- `Tools.cs` – `get_psp_health`, `get_fee_quote` tool wrappers.
- `Bandit.cs` – `IBandit`, `IContextualBandit`, `EpsilonGreedyBandit`, `ThompsonSamplingBandit`, `ContextualEpsilonGreedyBandit`.
- `MemoryPgVector.cs` – `PgVectorMemory` (ensure schema, add/search).
- `OpenAIChatClient.cs` – chat wrapper with `response_format=json_object` and tool-calling loop.
- `EmbeddingsHelper.cs` – `OpenAIEmbeddings` helper (HTTP) for embeddings.
- `CapabilityMatrix.cs` – method→PSP gating.
- `Dummies.cs` – dummy providers for local testing.
- `PspRouter.csproj` – .NET 8, refs: `Npgsql`, `Pgvector`.

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

## 🚀 Quick Start

### Prerequisites
- .NET 8.0 SDK
- PostgreSQL with pgvector extension
- OpenAI API key

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

### 3. Run the Application

```bash
# Build the project
dotnet build

# Run the application
dotnet run
```

### Expected Output
```
=== Configuration ===
✓ Database schema ensured
✓ Components initialized

--- Transaction 1 ---
Merchant: M123, Amount: 120.00 USD, Method: Card
Decision: Adyen
Reasoning: LLM routing - Auth: 89.00%, Fee: 200bps + $0.30
Method: LLM routing
Outcome: ✓ Authorized - Fee: $2.70 - Time: 450ms
✓ Lesson added to memory

Top memory results:
score=0.950 key=sample_1 meta_candidate=Adyen
USD Visa transactions work well with Adyen for low-risk merchants
---

=== PSP Router Ready ===
The enhanced PSP Router is now running with:
• LLM-based intelligent routing
• Multi-armed bandit learning
• Vector memory for lessons
• Comprehensive logging and monitoring
• Real PostgreSQL database integration
```

## 📋 Usage Examples

### Basic Routing
```csharp
var router = new PspRouter(chatClient, healthProvider, feeProvider, tools, bandit, memory, logger);
var decision = await router.DecideAsync(context, cancellationToken);
```

### Learning from Outcomes
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

### Core Classes

#### `PspRouter`
Main routing engine with LLM and bandit integration.

#### `ContextualEpsilonGreedyBandit`
Enhanced contextual bandit with transaction feature awareness.

#### `EpsilonGreedyBandit`
Standard multi-armed bandit with epsilon-greedy exploration strategy.

#### `ThompsonSamplingBandit`
Bayesian multi-armed bandit with Thompson sampling.

#### `PgVectorMemory`
Vector database integration for semantic memory.

#### `OpenAIChatClient`
LLM integration with tool calling support.

### Key Methods

#### `DecideAsync(RouteContext, CancellationToken)`
Makes routing decision using LLM or fallback logic.

#### `UpdateReward(RouteDecision, TransactionOutcome)`
Updates bandit learning with transaction outcome.

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

**Happy Routing! 🎯**

The Enhanced PSP Router provides a production-ready foundation for intelligent payment routing with continuous learning and optimization.

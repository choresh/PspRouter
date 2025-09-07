# 🚀 PSP Router - Implementation TODO List

## 📋 Overview
This document outlines all the components that need to be implemented or replaced to complete the PSP Router solution. The system is currently functional with dummy implementations and needs real data integrations for production use.

---

## 📅 **Implementation Timeline & Next Stages**

### **📋 Implementation Priority Order**
1. **CRITICAL FIRST**: Bandit Statistics Persistence & Recovery (Item #4) - Production blocker
2. **Phase 1 Core**: Environment Configuration (Item #1) → Database Setup (Item #2) → Vector Memory Integration (Item #3)
3. **Phase 2 Integration**: Health Provider (Item #5) → Fee Provider (Item #6) → Service Registration (Item #7)
4. **Phase 3 Production**: Monitoring (Item #11) → Security (Item #14) → Performance Testing (Item #16)
5. **Phase 4 Advanced**: ML Enhancements (Item #21) → Analytics (Item #22) → Multi-Region (Item #23)

### **⚠️ Critical Dependencies**
- **Bandit Persistence** must be implemented FIRST - it's blocking production use
- **Database Setup** must be complete before bandit persistence
- **Environment Configuration** must be done before any real integrations
- **Vector Memory Integration** can be done in parallel with bandit persistence

### **🎯 Success Criteria**
- **Week 2**: System can restart without losing learning progress
- **Week 3**: All core functionality working with real data
- **Week 6**: Real PSP integrations working reliably
- **Week 8**: Production-ready with monitoring and security
- **Week 12**: Enterprise-grade system with advanced features

---

## 🚨 **CRITICAL - Phase 1: Core Functionality**

### 1. **Environment Configuration** ⚠️ **REQUIRED**
- [ ] Set up environment variables:
  ```bash
  OPENAI_API_KEY=sk-your-openai-key-here
  PGVECTOR_CONNSTR=Host=localhost;Username=postgres;Password=postgres;Database=psp_router
  ```
- [ ] Create `.env` file or configure in your deployment environment
- [ ] Verify OpenAI API key has sufficient credits and access to GPT-4

### 2. **Database Setup** ⚠️ **REQUIRED**
- [ ] Install PostgreSQL with pgvector extension
- [ ] Run `setup-database.sql` to create database schema
- [ ] Verify database connection in `Program.cs`
- [ ] Test vector operations with sample data

### 3. **Vector Memory Integration** ⚠️ **INCOMPLETE**
**File:** `PspRouter.Lib/Router.cs` (lines 102-128)

**Current Issue:** `GetRelevantLessonsAsync` method returns empty list

**Important Clarifications:**
- **Vector DB is FOR the LLM**: Used exclusively to enhance LLM decision-making
- **NOT a Fallback**: The deterministic fallback system does not use vector memory
- **Memory vs. Logic**: Vector DB provides historical context, not routing logic

**Implementation Required:**
```csharp
private async Task<List<string>> GetRelevantLessonsAsync(RouteContext ctx, CancellationToken ct)
{
    if (_memory == null) return new List<string>();

    try
    {
        // TODO: Implement embedding integration
        // 1. Create query embedding using OpenAIEmbeddings
        // 2. Search vector memory with embedding
        // 3. Return top-k relevant lessons
        
        var query = $"PSP routing for {ctx.Tx.Currency} {ctx.Tx.Method} {ctx.Tx.Scheme} merchant {ctx.Tx.MerchantCountry}";
        
        // IMPLEMENTATION NEEDED:
        // var embeddings = _embeddingsService; // Get from DI
        // var queryEmbedding = await embeddings.CreateEmbeddingAsync(query, ct);
        // var results = await _memory.SearchAsync(queryEmbedding, k: 5, ct);
        // return results.Select(r => r.text).ToList();
        
        return new List<string>();
    }
    catch (Exception ex)
    {
        _logger?.LogError(ex, "Failed to retrieve relevant lessons from memory");
        return new List<string>();
    }
}
```

### 4. **Bandit Statistics Persistence & Recovery** ❌ **MISSING**
**Files:** `PspRouter.Lib/Bandit.cs`, `PspRouter.API/Controllers/RoutingController.cs`

**Current Issue:** Bandit statistics are lost on server restart - no persistence or recovery

**Critical Problems:**
- ❌ Transaction outcomes are only logged, not stored in database
- ❌ Bandit statistics exist only in memory
- ❌ No recovery mechanism on application startup
- ❌ Server restart = complete loss of learning progress

**Implementation Required:**

#### **A. Transaction Outcome Storage**
```csharp
// File: PspRouter.API/Controllers/RoutingController.cs
[HttpPost("outcome")]
public async Task<ActionResult> UpdateOutcome([FromBody] TransactionOutcome outcome)
{
    try
    {
        // TODO: Store in database instead of just logging
        // await _transactionOutcomeService.StoreAsync(outcome);
        
        // TODO: Update bandit learning
        // await _router.UpdateRewardAsync(decision, outcome);
        
        _logger.LogInformation("Transaction outcome: {Outcome}", JsonSerializer.Serialize(outcome));
        return Ok(new { message = "Outcome recorded successfully" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error updating transaction outcome");
        return StatusCode(500, new { error = "Internal server error", message = ex.Message });
    }
}
```

#### **B. Bandit Statistics Persistence**
```csharp
// File: PspRouter.Lib/Bandit.cs
public interface IBanditPersistence
{
    Task SaveStatisticsAsync(Dictionary<string, Dictionary<string, (double sum, int count)>> stats);
    Task<Dictionary<string, Dictionary<string, (double sum, int count)>>> LoadStatisticsAsync();
    Task RebuildFromTransactionHistoryAsync();
}

public class DatabaseBanditPersistence : IBanditPersistence
{
    // TODO: Implement database persistence
    // - Save bandit statistics to bandit_stats table
    // - Load statistics on startup
    // - Rebuild from transaction_outcomes table
}
```

#### **C. Application Startup Recovery**
```csharp
// File: PspRouter.API/Program.cs
public async Task StartAsync()
{
    // TODO: Rebuild bandit statistics from database
    // await _banditPersistence.RebuildFromTransactionHistoryAsync();
    
    _logger.LogInformation("Bandit statistics rebuilt from transaction history");
}
```

**Database Schema (Already Exists):**
```sql
-- ✅ Tables already created in setup-database.sql
CREATE TABLE IF NOT EXISTS bandit_stats (
    segment_key TEXT NOT NULL,
    arm_name TEXT NOT NULL,
    alpha REAL DEFAULT 1.0,
    beta REAL DEFAULT 1.0,
    sum_rewards REAL DEFAULT 0.0,
    count_pulls INTEGER DEFAULT 0,
    last_updated TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
    PRIMARY KEY (segment_key, arm_name)
);

CREATE TABLE IF NOT EXISTS transaction_outcomes (
    decision_id TEXT PRIMARY KEY,
    psp_name TEXT NOT NULL,
    authorized BOOLEAN NOT NULL,
    transaction_amount DECIMAL(10,2) NOT NULL,
    fee_amount DECIMAL(10,2) NOT NULL,
    processing_time_ms INTEGER NOT NULL,
    risk_score INTEGER NOT NULL,
    processed_at TIMESTAMP NOT NULL,
    error_code TEXT,
    error_message TEXT,
    merchant_id TEXT,
    currency TEXT,
    payment_method TEXT
);
```

---

## 🔧 **Phase 2: Replace Dummy Implementations**

### 5. **Health Provider Implementation** ❌ **DUMMY**
**File:** `PspRouter.API/Dummies.cs` (lines 5-11)

**Current:** Returns hardcoded "green" health status

**Replace with:**
```csharp
public class RealHealthProvider : IHealthProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RealHealthProvider> _logger;

    public async Task<(string health, int latencyMs)> GetAsync(string psp, CancellationToken ct)
    {
        // TODO: Implement real PSP health checks
        // - Adyen: https://status.adyen.com/api
        // - Stripe: https://status.stripe.com/api
        // - PayPal: https://www.paypal-status.com/api
        // - Klarna: https://status.klarna.com/api
        
        // Implementation example:
        // var response = await _httpClient.GetAsync($"/api/health/{psp}", ct);
        // var healthData = await response.Content.ReadFromJsonAsync<HealthResponse>();
        // return (healthData.Status, healthData.LatencyMs);
        
        throw new NotImplementedException("Real health provider not implemented");
    }
}
```

### 6. **Fee Provider Implementation** ❌ **DUMMY**
**File:** `PspRouter.API/Dummies.cs` (lines 13-19)

**Current:** Returns hardcoded 200 bps + $0.30 fees

**Replace with:**
```csharp
public class RealFeeProvider : IFeeQuoteProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RealFeeProvider> _logger;

    public async Task<(int feeBps, decimal fixedFee)> GetAsync(string psp, RouteInput tx, CancellationToken ct)
    {
        // TODO: Implement real PSP fee calculations
        // - Adyen: https://docs.adyen.com/api-explorer/#/CheckoutService/v68/post/paymentMethods
        // - Stripe: https://stripe.com/docs/api/prices
        // - PayPal: https://developer.paypal.com/docs/api/orders/v2/
        // - Klarna: https://developers.klarna.com/api/reference/
        
        // Implementation example:
        // var feeRequest = new FeeRequest(tx.Currency, tx.Amount, tx.Method);
        // var response = await _httpClient.PostAsJsonAsync($"/api/fees/{psp}", feeRequest, ct);
        // var feeData = await response.Content.ReadFromJsonAsync<FeeResponse>();
        // return (feeData.BasisPoints, feeData.FixedFee);
        
        throw new NotImplementedException("Real fee provider not implemented");
    }
}
```

### 7. **Update Service Registration** ⚠️ **REQUIRED**
**File:** `PspRouter.API/Program.cs` (lines 53-54)

**Replace:**
```csharp
// ❌ CURRENT (DUMMY):
builder.Services.AddSingleton<IHealthProvider, PspRouter.API.DummyHealthProvider>();
builder.Services.AddSingleton<IFeeQuoteProvider, PspRouter.API.DummyFeeProvider>();

// ✅ REPLACE WITH:
builder.Services.AddSingleton<IHealthProvider, PspRouter.API.RealHealthProvider>();
builder.Services.AddSingleton<IFeeQuoteProvider, PspRouter.API.RealFeeProvider>();
```

---

## 📊 **Phase 3: Real Data Integration**

### 8. **Historical Statistics Implementation** ❌ **HARDCODED**
**Current Issue:** `AuthRate30d` values are hardcoded in test data

**Implementation Required:**
```csharp
public class StatisticsProvider
{
    public async Task<Dictionary<string, double>> GetAuthRates30dAsync(
        IEnumerable<string> psps, 
        CancellationToken ct)
    {
        // TODO: Implement real statistics calculation
        // - Query transaction database for last 30 days
        // - Calculate authorization rates per PSP
        // - Return dictionary of PSP -> AuthRate
        
        // Example implementation:
        // var stats = new Dictionary<string, double>();
        // foreach (var psp in psps)
        // {
        //     var authRate = await CalculateAuthRateAsync(psp, DateTime.UtcNow.AddDays(-30), ct);
        //     stats[psp] = authRate;
        // }
        // return stats;
        
        throw new NotImplementedException("Statistics provider not implemented");
    }
}
```

### 9. **Merchant Preferences System** ❌ **EMPTY**
**Current Issue:** `MerchantPrefs` and `SegmentStats` are empty

**Implementation Required:**
```csharp
public class MerchantPreferencesProvider
{
    public async Task<Dictionary<string, string>> GetPreferencesAsync(
        string merchantId, 
        CancellationToken ct)
    {
        // TODO: Implement merchant preference retrieval
        // - Preferred PSPs
        // - Fee tolerance
        // - Risk preferences
        // - Geographic preferences
        
        throw new NotImplementedException("Merchant preferences not implemented");
    }
}
```

### 10. **Transaction Segmentation** ❌ **MISSING**
**Implementation Required:**
```csharp
public class SegmentAnalytics
{
    public Dictionary<string, double> CalculateSegmentStats(RouteInput tx)
    {
        // TODO: Implement transaction segmentation
        // - Risk-based segments
        // - Geographic segments
        // - Amount-based segments
        // - Payment method segments
        
        return new Dictionary<string, double>();
    }
}
```

---

## 🏗️ **Phase 4: Production Features**

### 11. **Monitoring & Observability** ❌ **BASIC**
- [ ] Add structured logging with correlation IDs
- [ ] Implement health check endpoints for all PSPs
- [ ] Add metrics collection (authorization rates, response times)
- [ ] Set up alerting for PSP failures
- [ ] Add distributed tracing

### 12. **Caching Layer** ❌ **MISSING**
- [ ] Implement Redis caching for PSP health status
- [ ] Cache fee calculations with TTL
- [ ] Cache merchant preferences
- [ ] Implement cache invalidation strategies

### 13. **Rate Limiting & Circuit Breakers** ❌ **MISSING**
- [ ] Add rate limiting for API endpoints
- [ ] Implement circuit breakers for PSP calls
- [ ] Add retry policies with exponential backoff
- [ ] Implement bulkhead pattern for PSP isolation

### 14. **Security Enhancements** ⚠️ **BASIC**
- [ ] Add API authentication/authorization
- [ ] Implement request/response encryption
- [ ] Add input validation and sanitization
- [ ] Implement audit logging for compliance

---

## 🧪 **Phase 5: Testing & Quality**

### 15. **Integration Tests** ⚠️ **PARTIAL**
**File:** `PspRouter.Tests/IntegrationTests.cs`

**Current:** Uses dummy implementations

**Enhance with:**
- [ ] Real PSP API integration tests (with test accounts)
- [ ] Database integration tests
- [ ] Vector memory integration tests
- [ ] End-to-end routing flow tests

### 16. **Performance Testing** ❌ **MISSING**
- [ ] Load testing for routing decisions
- [ ] Stress testing for concurrent requests
- [ ] Memory usage profiling
- [ ] Database performance optimization

### 17. **Data Quality Tests** ❌ **MISSING**
- [ ] PSP data validation tests
- [ ] Fee calculation accuracy tests
- [ ] Health status consistency tests
- [ ] Vector search relevance tests

---

## 🔄 **Phase 6: Deployment & Operations**

### 18. **Containerization** ❌ **MISSING**
- [ ] Create Dockerfile for API
- [ ] Create docker-compose.yml with PostgreSQL
- [ ] Add health checks to containers
- [ ] Configure multi-stage builds

### 19. **CI/CD Pipeline** ❌ **MISSING**
- [ ] Set up automated testing pipeline
- [ ] Add code quality checks (SonarQube)
- [ ] Implement automated deployment
- [ ] Add rollback capabilities

### 20. **Configuration Management** ⚠️ **BASIC**
- [ ] Implement configuration validation
- [ ] Add environment-specific configs
- [ ] Implement feature flags
- [ ] Add configuration hot-reloading

---

## 📈 **Phase 7: Advanced Features**

### 21. **Machine Learning Enhancements** ❌ **BASIC**
- [ ] Implement A/B testing framework
- [ ] Add model performance monitoring
- [ ] Implement online learning for bandits
- [ ] Add feature engineering pipeline

### 22. **Analytics & Reporting** ❌ **MISSING**
- [ ] Implement routing decision analytics
- [ ] Add PSP performance dashboards
- [ ] Create merchant-specific reports
- [ ] Implement cost optimization insights

### 23. **Multi-Region Support** ❌ **MISSING**
- [ ] Implement geographic routing
- [ ] Add region-specific PSP preferences
- [ ] Implement data residency compliance
- [ ] Add cross-region failover

---


---

## 📝 **Implementation Notes**

### 10. **Priority Levels:**
- 🚨 **CRITICAL**: Required for basic functionality
- ⚠️ **HIGH**: Required for production use
- ❌ **MEDIUM**: Important for scalability
- 📈 **LOW**: Nice-to-have features

### 11. **Dependencies:**
- Phase 1 must be completed before Phase 2
- Phase 2 must be completed before Phase 3
- Phases 4-7 can be implemented in parallel

### 12. **Testing Strategy:**
- Unit tests for each new implementation
- Integration tests with real PSP APIs (test accounts)
- End-to-end tests for complete routing flow
- Performance tests for production readiness

---

## 🔗 **Useful Resources**

### 13. **PSP API Documentation:**
- [Adyen API](https://docs.adyen.com/api-explorer/)
- [Stripe API](https://stripe.com/docs/api)
- [PayPal API](https://developer.paypal.com/docs/api/)
- [Klarna API](https://developers.klarna.com/api/)

### 14. **Technical References:**
- [pgvector Documentation](https://github.com/pgvector/pgvector)
- [OpenAI API Documentation](https://platform.openai.com/docs)
- [ASP.NET Core Best Practices](https://docs.microsoft.com/en-us/aspnet/core/)

---

**Last Updated:** December 2024
**Status:** Ready for implementation
**Estimated Total Effort:** 4-6 weeks for core functionality, 8-12 weeks for full production system

---

## 🛠 **Implementation Tasks & Guidance**

### 15. **🔄 Replace Dummy Implementations**
- [ ] Implement `IHealthProvider` and `IFeeQuoteProvider` with your metrics/config
- [ ] Swap in your own stats for `AuthRate30d` and pass them in `RouteContext`
- [ ] Replace `DummyHealthProvider` and `DummyFeeProvider` with real implementations
- [ ] Update service registration in `Program.cs`

### 16. **🔧 System Tuning & Configuration**
- [ ] Adjust embedding model & `vector(N)` dimension in `MemoryPgVector.cs` to match your chosen model
- [ ] Configure model name in `OpenAIChatClient` (default: `gpt-4.1`)
- [ ] Tune bandit epsilon values for optimal exploration/exploitation balance
- [ ] Configure logging levels for production vs development environments

### 17. **📊 Monitoring & Observability Setup**

#### **Logging Configuration**
- [ ] **Information**: Routing decisions, learning updates
- [ ] **Warning**: Fallback scenarios, degraded performance
- [ ] **Error**: LLM failures, database issues, parsing errors
- [ ] **Debug**: Detailed decision reasoning, memory operations

#### **Key Metrics to Track**
- [ ] **Authorization Success Rate**: Per PSP, per segment
- [ ] **Average Processing Time**: Response time optimization
- [ ] **Fee Optimization**: Cost reduction over time
- [ ] **Learning Convergence**: Bandit algorithm performance

#### **Monitoring Setup**
- [ ] Set up Application Insights or Prometheus integration
- [ ] Configure alerting for critical failures
- [ ] Implement health check endpoints for all PSPs
- [ ] Set up performance dashboards

### 18. **📦 Packaging & Distribution**

#### **Creating NuGet Package**
- [ ] Build the library in Release mode
  ```bash
  dotnet build PspRouter.Lib --configuration Release
  ```
- [ ] Create NuGet package
  ```bash
  dotnet pack PspRouter.Lib --configuration Release
  ```
- [ ] Package will be created in: `PspRouter.Lib/bin/Release/PspRouter.Lib.1.0.0.nupkg`

#### **Library Integration Examples**
- [ ] **ASP.NET Core Web API Integration**
  ```csharp
  services.AddScoped<PspRouter.Lib.PspRouter>();
  services.AddSingleton<IHealthProvider, YourHealthProvider>();
  services.AddSingleton<IFeeQuoteProvider, YourFeeProvider>();
  ```
- [ ] **Azure Functions Integration**
- [ ] **Console Application Integration**

#### **Library Dependencies**
The library has minimal external dependencies:
- `Npgsql` - PostgreSQL client
- `OpenAI` - OpenAI API client  
- `Pgvector` - Vector database support
- `Microsoft.Extensions.Logging.Abstractions` - Logging interfaces

### 19. **🔧 Customization & Extensions**

#### **External Bandit Libraries**
For production use, consider integrating with industry-standard libraries:
- [ ] **VowpalWabbit** (requires .NET Framework compatibility)
- [ ] **Accord.NET** (machine learning framework)
- [ ] **ML.NET** (Microsoft's machine learning framework)

#### **Adding New PSPs**
- [ ] Update `CapabilityMatrix.Supports()` method
- [ ] Implement health and fee providers
- [ ] Add PSP-specific logic to reward calculation
- [ ] Update system prompts with new PSP characteristics

#### **Custom Reward Functions**
- [ ] Implement custom reward calculation logic
- [ ] Test reward function with historical data
- [ ] Validate reward function performance

### 20. **📈 Performance Optimization**

#### **Caching Strategy**
- [ ] **PSP Health**: Cache health status for 30 seconds
- [ ] **Fee Tables**: Cache fee data for 5 minutes
- [ ] **Memory Results**: Cache vector search results
- [ ] **Bandit State**: In-memory bandit statistics

#### **Database Optimization**
- [ ] Optimize vector search performance with proper indexing
  ```sql
  CREATE INDEX CONCURRENTLY psp_lessons_embedding_idx 
  ON psp_lessons USING ivfflat (embedding vector_cosine_ops) 
  WITH (lists = 100);
  ```
- [ ] Analyze tables for better query planning
- [ ] Configure connection pooling parameters

#### **Connection Pooling**
- [ ] Configure pooling parameters in connection string
  ```
  "Host=localhost;Username=postgres;Password=postgres;Database=psp_router;Pooling=true;MinPoolSize=5;MaxPoolSize=20"
  ```

### 21. **🚨 Troubleshooting Guide**

#### **Common Issues & Solutions**

**1. Database Connection Failed**
```
Error: 3D000: database "psp_router" does not exist
```
- [ ] Create the database using the setup script

**2. Vector Extension Not Found**
```
Error: extension "vector" does not exist
```
- [ ] Install pgvector extension: `sudo apt install postgresql-16-pgvector`

**3. OpenAI API Key Invalid**
```
Error: Invalid API key
```
- [ ] Verify your API key and ensure it has sufficient credits

**4. Permission Denied**
```
Error: permission denied for table psp_lessons
```
- [ ] Grant proper permissions to your database user

#### **Debug Mode**
- [ ] Enable debug logging: `export DOTNET_ENVIRONMENT=Development`
- [ ] Run with verbose logging: `dotnet run --verbosity detailed`

### 22. **🔒 Security Implementation**

#### **API Key Security**
- [ ] Never commit API keys to version control
- [ ] Use environment variables or secure key management
- [ ] Rotate keys regularly

#### **Database Security**
- [ ] Use strong passwords
- [ ] Enable SSL connections in production
- [ ] Restrict database access by IP
- [ ] Regular security updates

#### **Network Security**
- [ ] Use VPN or private networks for database access
- [ ] Implement rate limiting
- [ ] Monitor for suspicious activity

### 23. **💾 Backup and Recovery Setup**

#### **Database Backup**
- [ ] Create backup script: `pg_dump -U postgres -h localhost psp_router > psp_router_backup.sql`
- [ ] Restore backup script: `psql -U postgres -h localhost psp_router < psp_router_backup.sql`

#### **Automated Backups**
- [ ] Add to crontab for daily backups:
  ```bash
  0 2 * * * pg_dump -U postgres psp_router | gzip > /backups/psp_router_$(date +\%Y\%m\%d).sql.gz
  ```

---


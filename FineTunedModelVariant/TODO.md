# 🚀 PSP Router - Implementation TODO List (Fine-Tuned Model Variant)

## 📋 Overview
This document outlines all the components that need to be implemented or replaced to complete the PSP Router solution. The system is currently functional with dummy implementations and needs real data integrations for production use.

---

## 📅 **Implementation Timeline & Next Stages**

### **📋 Implementation Priority Order**
1. Core wiring: environment, DI, prompt/schema, timeouts/fallback
2. Replace dummies: Health Provider, Fee Provider, Capability Provider
3. Inputs & guardrails: capability matrix, PSP snapshots, preferences
4. Observability: logging, metrics, tracing; robust error handling
5. Testing: integration and performance (latency + fallback)
6. Deployment: containerization, CI/CD, config management
7. Hardening: security, rate limiting, caching

### **⚠️ Critical Dependencies**
- Environment configuration before real integrations
- Health/Fee providers before production testing
- Observability before production rollout

### **🔄 Sequential vs Parallel Tasks**
**Sequential:** Core wiring → Providers → Observability → Testing → Deployment

**Parallel:** Health + Fee providers; Monitoring/Rate limiting/Caching once endpoints stable

### **🎯 Success Criteria**
- P95 latency within SLA with deterministic fallback
- Schema-valid decisions with clear reasoning
- Production-grade logs/metrics/traces

---

## 🚀 **Phase 1: Core Functionality**


### 1. **Environment Configuration** ⚠️ **REQUIRED**
- [ ] Set up environment variables:
  ```bash
  OPENAI_API_KEY=sk-your-openai-key-here
  ```
- [ ] Create `.env` file or configure in your deployment environment
- [ ] Verify OpenAI API key has sufficient credits and access to GPT-4

#### LLM Setup
- [ ] Set fine-tuned model ID via `OPENAI_FT_MODEL` or `PspRouter:OpenAI:FineTunedModel`
- [ ] Confirm strict `RouteDecision` JSON schema in `BuildSystemPrompt()`
- [ ] (Optional) few-shot examples to reinforce reasoning style
- [ ] Configure timeout/fallback (e.g., 800–1200 ms) and strict JSON validation
- [ ] Ensure tools for health/fees are active
- [ ] Observability: latency, tokens, fallback rate, schema errors
- [ ] Tests: unit (schema/fallback), integration (end-to-end), performance (P95)

#### Fine-tuning workflow (optional but recommended)
- [ ] Data prep: collect de-PII’d labeled decisions (inputs → `RouteDecision`)
- [ ] Upload training file (API: Files.Upload)
- [ ] Create fine-tune job (API: FineTuning.CreateJob)
- [ ] Track job and retrieve model id (API: FineTuning.GetJob)
- [ ] Offline eval against holdout; fix errors; iterate
- [ ] Shadow/A-B test in staging; then production rollout with monitoring

---

## 🔧 **Phase 2: Replace Dummy Implementations**

### 2. **Health Provider Implementation** ❌ **DUMMY**
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

### 3. **Fee Provider Implementation** ❌ **DUMMY**
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

### 4. **Capability Provider Implementation** ❌ **DUMMY**
**File:** `PspRouter.API/Dummies.cs` (lines 31-40)

**Current:** Returns hardcoded results

**Replace with:**
```csharp
public class RealCapabilityProvider : ICapabilityProvider
{
    // TODO: Load capability rules from configuration at startup (per PSP)
    // - AllowedMethods: Card, PayPal, KlarnaPayLater
    // - AllowedSchemes (for Card): Visa, Mastercard, Amex
    // - Supports3DS / Requires3DS flags
    // - Optional allowlists: MerchantCountries, Currencies
    // Pseudocode:
    // private readonly CapabilityConfig _config;
    // public RealCapabilityProvider(CapabilityConfig config) { _config = config; }
    // public bool Supports(string psp, RouteInput tx) {
    //   if (!_config.Providers.TryGetValue(psp, out var p)) return false;
    //   if (!p.AllowedMethods.Contains(tx.Method)) return false;
    //   if (tx.Method == PaymentMethod.Card && p.AllowedSchemes?.Count > 0 && !p.AllowedSchemes.Contains(tx.Scheme)) return false;
    //   if (tx.Method == PaymentMethod.Card && tx.SCARequired && p.Requires3DS && p.Supports3DS == false) return false;
    //   if (p.AllowedMerchantCountries?.Count > 0 && !p.AllowedMerchantCountries.Contains(tx.MerchantCountry)) return false;
    //   if (p.AllowedCurrencies?.Count > 0 && !p.AllowedCurrencies.Contains(tx.Currency)) return false;
    //   return true;
    // }

      throw new NotImplementedException("Real capability provider not implemented");
}
```

### 5. **Update Service Registration** ⚠️ **REQUIRED**
**File:** `PspRouter.API/Program.cs` (lines 53-54)

**Replace:**
```csharp
// ❌ CURRENT (DUMMY):
builder.Services.AddSingleton<ICapabilityProvider, PspRouter.API.DummyCapabilityProvider>();
builder.Services.AddSingleton<IHealthProvider, PspRouter.API.DummyHealthProvider>();
builder.Services.AddSingleton<IFeeQuoteProvider, PspRouter.API.DummyFeeProvider>();

// ✅ REPLACE WITH:
builder.Services.AddSingleton<ICapabilityProvider, PspRouter.API.RealCapabilityProvider>();
builder.Services.AddSingleton<IHealthProvider, PspRouter.API.RealHealthProvider>();
builder.Services.AddSingleton<IFeeQuoteProvider, PspRouter.API.RealFeeProvider>();
```

---

## 🏗️ **Phase 4: Production Features**

### 6. **Monitoring & Observability** ❌ **BASIC**
- [ ] Add structured logging with correlation IDs
- [ ] Implement health check endpoints for all PSPs
- [ ] Add metrics collection (authorization rates, response times)
- [ ] Set up alerting for PSP failures
- [ ] Add distributed tracing

### 7. **Caching Layer** ❌ **MISSING**
- [ ] Implement Redis caching for PSP health status
- [ ] Cache fee calculations with TTL
- [ ] Cache merchant preferences
- [ ] Implement cache invalidation strategies

### 8. **Rate Limiting & Circuit Breakers** ❌ **MISSING**
- [ ] Add rate limiting for API endpoints
- [ ] Implement circuit breakers for PSP calls
- [ ] Add retry policies with exponential backoff
- [ ] Implement bulkhead pattern for PSP isolation

### 9. **Security Enhancements** ⚠️ **BASIC**
- [ ] Add API authentication/authorization
- [ ] Implement request/response encryption
- [ ] Add input validation and sanitization
- [ ] Implement audit logging for compliance

---

## 🧪 **Phase 5: Testing & Quality**

### 10. **Integration Tests** ⚠️ **PARTIAL**
**File:** `PspRouter.Tests/IntegrationTests.cs`

**Current:** Uses dummy implementations

**Enhance with:**
- [ ] Real PSP API integration tests (with test accounts) 
- [ ] End-to-end routing flow tests

### 11. **Performance Testing** ❌ **MISSING**
- [ ] Load testing for routing decisions
- [ ] Stress testing for concurrent requests
- [ ] Memory usage profiling
 

### 12. **Data Quality Tests** ❌ **MISSING**
- [ ] PSP data validation tests
- [ ] Fee calculation accuracy tests
- [ ] Health status consistency tests
 

---

## 🔄 **Phase 6: Deployment & Operations**

### 13. **Containerization** ❌ **MISSING**
- [ ] Create Dockerfile for API
 
- [ ] Add health checks to containers
- [ ] Configure multi-stage builds

### 14. **CI/CD Pipeline** ❌ **MISSING**
- [ ] Set up automated testing pipeline
- [ ] Add code quality checks (SonarQube)
- [ ] Implement automated deployment
- [ ] Add rollback capabilities

### 15. **Configuration Management** ⚠️ **BASIC**
- [ ] Implement configuration validation
- [ ] Add environment-specific configs
- [ ] Implement feature flags
- [ ] Add configuration hot-reloading

---

## 📈 **Phase 7: Advanced Features**

### 16. **Machine Learning Enhancements** ❌ **BASIC**
- [ ] Implement A/B testing framework
- [ ] Add model performance monitoring
- [ ] Implement online learning for bandits
- [ ] Add feature engineering pipeline

### 17. **Analytics & Reporting** ❌ **MISSING**
- [ ] Implement routing decision analytics
- [ ] Add PSP performance dashboards
- [ ] Create merchant-specific reports
- [ ] Implement cost optimization insights

### 18. **Multi-Region Support** ❌ **MISSING**
- [ ] Implement geographic routing
- [ ] Add region-specific PSP preferences
- [ ] Implement data residency compliance
- [ ] Add cross-region failover

---

## 📝 **Implementation Notes**

### **Priority Levels:**
- 🚨 **CRITICAL**: Required for basic functionality
- ⚠️ **HIGH**: Required for production use
- ❌ **MEDIUM**: Important for scalability
- 📈 **LOW**: Nice-to-have features

### **Dependencies:**
- Phase 1 must be completed before Phase 2
- Phase 2 must be completed before Phase 3
- Phases 4-7 can be implemented in parallel

### **Testing Strategy:**
- Unit tests for each new implementation
- Integration tests with real PSP APIs (test accounts)
- End-to-end tests for complete routing flow
- Performance tests for production readiness

---

## 🔗 **Useful Resources**

### **PSP API Documentation:**
- [Adyen API](https://docs.adyen.com/api-explorer/)
- [Stripe API](https://stripe.com/docs/api)
- [PayPal API](https://developer.paypal.com/docs/api/)
- [Klarna API](https://developers.klarna.com/api/)

### **Technical References:**
 
- [OpenAI API Documentation](https://platform.openai.com/docs)
- [ASP.NET Core Best Practices](https://docs.microsoft.com/en-us/aspnet/core/)

---

**Last Updated:** December 2024
**Status:** Ready for implementation
**Estimated Total Effort:** 4-6 weeks for core functionality, 8-12 weeks for full production system

---

## 🛠 **Implementation Tasks & Guidance**

### **🔄 Replace Dummy Implementations**
- [ ] Implement `IHealthProvider` and `IFeeQuoteProvider` with your metrics/config
- [ ] Swap in your own stats for `AuthRate30d` and pass them in `RouteContext`
- [ ] Replace `DummyHealthProvider` and `DummyFeeProvider` with real implementations
- [ ] Update service registration in `Program.cs`

### **🔧 System Tuning & Configuration**
- [ ] Adjust embedding model & `vector(N)` dimension in `MemoryPgVector.cs` to match your chosen model
- [ ] Configure model name in `OpenAIChatClient` (default: `gpt-4.1`)
- [ ] Tune bandit epsilon values for optimal exploration/exploitation balance
- [ ] Configure logging levels for production vs development environments

### **📊 Monitoring & Observability Setup**

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

### **📦 Packaging & Distribution**

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
  services.AddSingleton<ICapabilityProvider, YourCapabilityProvider>();
  services.AddSingleton<IHealthProvider, YourHealthProvider>();
  services.AddSingleton<IFeeQuoteProvider, YourFeeProvider>();
  ```
- [ ] **Azure Functions Integration**
- [ ] **Console Application Integration**

#### **Library Dependencies**
The library has minimal external dependencies:
- `OpenAI` - OpenAI API client  
- `Microsoft.Extensions.Logging.Abstractions` - Logging interfaces

### **🔧 Customization & Extensions**

 
#### **Adding New PSPs**
- [ ] Implement/update health, capability and fee providers

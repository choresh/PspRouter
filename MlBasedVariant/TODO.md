# 🚀 PSP Router - Implementation TODO List (ML-Based Variant)

## 📋 Overview
This document outlines all the components that need to be implemented to complete the PSP Router solution. The system uses a **fine-tuned model approach** where the AI learns everything from historical transaction data.

---

## 📅 **Implementation Timeline & Next Stages**

### **📋 Implementation Priority Order**
1. Environment configuration: OpenAI API key, fine-tuned model setup
2. Training data preparation: collect and format historical transaction data
3. Fine-tuning workflow: upload data, create job, monitor training
4. Observability: logging, metrics, tracing; robust error handling
5. Testing: integration and performance (latency + fallback)
6. Deployment: containerization, CI/CD, config management
7. Hardening: security, rate limiting, caching
8. Client SDK: sender-side library to build candidates and call API

### **⚠️ Critical Dependencies**
- Environment configuration before training
- Historical transaction data before fine-tuning
- Observability before production rollout
- Database connectivity for Client SDK

### **🔄 Sequential vs Parallel Tasks**
**Sequential:** Environment setup → Training data → Fine-tuning → Observability → Testing → Deployment

**Parallel:** Monitoring/Rate limiting/Caching once endpoints stable

### **🎯 Success Criteria**
- P95 latency within SLA with deterministic fallback
- Schema-valid decisions with clear reasoning
- Production-grade logs/metrics/traces

---

## 🚀 **Phase 1: Core Functionality**

### 1. **Environment Configuration** ⚠️ **REQUIRED**
- [ ] Set database connection string `PSPROUTER_DB_CONNECTION` in `.env`

#### ML-Based Setup
   - [ ] Use the trainer app (`PspRouter.Trainer`) for all training operations
   - Optional: adjust sampling via `Trainer:Sampling` in `PspRouter.Trainer/appsettings.json` (e.g., TargetSampleSize, DateWindowMonths, MaxPerSegment)
   - Run `dotnet run --project PspRouter.Trainer` to execute the workflow
   - Set the resulted model ID at env variable OPENAI_FT_MODEL (at file `FineTunedModelVariant\PspRouter.API\.env`)

#### Product-Tunable Weights (Routing)
- [ ] Configure `PspRouter:Routing:Weights` in `PspRouter.API/appsettings.json`
- [ ] Configure `AllowedHealthStatuses` in `PspRouter.API/appsettings.json`


#### Fine-tuning workflow (REQUIRED for production)
- [ ] Use the trainer app (`PspRouter.Trainer`) for all training operations
   - Adjust sampling via `Trainer:Sampling` in `PspRouter.Trainer/appsettings.json` (e.g., TargetSampleSize, DateWindowMonths, MaxPerSegment)
   - `Trainer:Sampling:TargetSampleSize` should be thousands to hundreds of thousands of rows
   - Optional: change the SQL query (within `GetTrainingData`), to achive better diverse sample for training, covering successes and failures, multiple currencies, payment methods, regions, 3DS vs non-3DS, tokenized vs non-tokenized, and a wide risk-score distribution.
   - Run `dotnet run --project PspRouter.Trainer` to execute the workflow
   - Set the resulted model ID at env variable OPENAI_FT_MODEL (at file `FineTunedModelVariant\PspRouter.API\.env`)

---

## 🏗️ **Phase 2: Production Features**
### 1. **Client SDK (PspRouter.Client)** ✅ **ADDED**
- [x] Create `PspRouter.Client` project
- [x] Implement `IPspDataProvider` with SQL Server queries from `PaymentTransactions`
- [x] Implement `PspRouterClient` to build candidates and call API
- [x] Add examples and README for sender integration
- [ ] Package as NuGet and publish (optional)


### 2. **Monitoring & Observability** ❌ **BASIC**
- [ ] Add structured logging with correlation IDs
- [ ] Add metrics collection (authorization rates, response times, model performance)
- [ ] Set up alerting for model failures and routing errors
- [ ] Add distributed tracing for fine-tuned model calls
- [ ] Monitor fine-tuned model performance and accuracy

### 3. **Caching Layer** ❌ **MISSING**
- [ ] Implement Redis caching for model responses
- [ ] Cache merchant preferences and routing patterns
- [ ] Implement cache invalidation strategies
- [ ] Cache training data for model updates

### 4. **Rate Limiting & Circuit Breakers** ❌ **MISSING**
- [ ] Add rate limiting for API endpoints
- [ ] Implement circuit breakers for OpenAI API calls
- [ ] Add retry policies with exponential backoff
- [ ] Implement fallback routing when model is unavailable

### 5. **Security Enhancements** ⚠️ **BASIC**
- [ ] Add API authentication/authorization
- [ ] Implement request/response encryption
- [ ] Add input validation and sanitization
- [ ] Implement audit logging for compliance
- [ ] Secure OpenAI API key management

---

## 🧪 **Phase 3: Testing & Quality**

### 1. **Integration Tests** ⚠️ **PARTIAL**
**File:** `PspRouter.Tests/IntegrationTests.cs`

**Current:** Uses simplified architecture with DummyChatClient

**Enhance with:**
- [ ] Fine-tuned model integration tests
- [ ] End-to-end routing flow tests with real model
- [ ] Training data validation tests

### 2. **Performance Testing** ❌ **MISSING**
- [ ] Load testing for routing decisions
- [ ] Stress testing for concurrent requests
- [ ] Memory usage profiling
- [ ] Model response time testing

### 3. **Data Quality Tests** ❌ **MISSING**
- [ ] Training data validation tests
- [ ] Model accuracy validation tests
- [ ] RouteDecision schema validation tests
- [ ] Historical data quality checks
- [ ] Scoring with edge weights (values `PspRouter:Routing:Weights` in `PspRouter.API/appsettings.json`)
 
---

## 🔄 **Phase 4: Deployment & Operations**

### 1. **Containerization** ❌ **MISSING**
- [ ] Create Dockerfile for API
- [ ] Add health checks to containers
- [ ] Configure multi-stage builds
- [ ] Add training service containerization

### 2. **CI/CD Pipeline** ❌ **MISSING**
- [ ] Set up automated testing pipeline
- [ ] Add code quality checks (SonarQube)
- [ ] Implement automated deployment
- [ ] Add rollback capabilities
- [ ] Automated model training pipeline

### 3. **Configuration Management** ⚠️ **BASIC**
- [ ] Implement configuration validation
- [ ] Add environment-specific configs
- [ ] Implement feature flags
- [ ] Add configuration hot-reloading
- [ ] Secure .env file management

---

## 📈 **Phase 5: Advanced Features**

### 1. **Machine Learning Enhancements** ❌ **BASIC**
- [ ] Implement A/B testing framework for model versions
- [ ] Add model performance monitoring and drift detection
- [ ] Implement continuous learning from new transaction data
- [ ] Add feature engineering pipeline for training data

### 2. **Analytics & Reporting** ❌ **MISSING**
- [ ] Implement routing decision analytics
- [ ] Add model performance dashboards
- [ ] Create merchant-specific reports
- [ ] Implement cost optimization insights
- [ ] Model accuracy and confidence metrics

### 3. **Multi-Region Support** ❌ **MISSING**
- [ ] Implement geographic routing
- [ ] Add region-specific model training
- [ ] Implement data residency compliance
- [ ] Add cross-region model deployment

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
- Phases 4-5 can be implemented in parallel

### **Testing Strategy:**
- Unit tests for fine-tuned model integration
- Integration tests with real model responses
- End-to-end tests for complete routing flow
- Performance tests for production readiness
- Model accuracy and validation tests

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
**Status:** ✅ Architecture simplified, ready for fine-tuning and training
**Estimated Total Effort:** 2-3 weeks for training and deployment, 4-6 weeks for full production system

---

## 🛠 **Implementation Tasks & Guidance**

### **🔄 ML-Based Setup**
- [ ] ✅ **COMPLETED**: Architecture simplified (removed all external providers)
- [ ] Set up OpenAI API key and fine-tuned model configuration
- [ ] Prepare training data from `PaymentTransactions` table
- [ ] Run training workflow using `PspRouter.Trainer` project
- [ ] Deploy fine-tuned model to production

### **🔧 System Tuning & Configuration**
- [ ] Configure fine-tuned model name in `OpenAIChatClient` (set via `OPENAI_FT_MODEL`)
- [ ] Tune model temperature and response format for optimal routing decisions
- [ ] Configure logging levels for production vs development environments
- [ ] Set up database connection for training data access

### **📊 Monitoring & Observability Setup**

#### **Logging Configuration**
- [ ] **Information**: Routing decisions, learning updates
- [ ] **Warning**: Fallback scenarios, degraded performance
- [ ] **Error**: LLM failures, database issues, parsing errors
- [ ] **Debug**: Detailed decision reasoning, memory operations

#### **Key Metrics to Track**
- [ ] **Model Accuracy**: Routing decision correctness
- [ ] **Average Processing Time**: Model response time optimization
- [ ] **Cost Optimization**: Fee reduction over time
- [ ] **Model Performance**: Success rates and confidence scores

#### **Monitoring Setup**
- [ ] Set up Application Insights or Prometheus integration
- [ ] Configure alerting for model failures and routing errors
- [ ] Implement health check endpoints for the routing service
- [ ] Set up performance dashboards for model metrics

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
  services.AddSingleton<IChatClient, OpenAIChatClient>();
  ```
- [ ] **Azure Functions Integration**
- [ ] **Console Application Integration**

#### **Library Dependencies**
The library has minimal external dependencies:
- `Microsoft.Extensions.Logging.Abstractions` - Logging interfaces
- Direct HTTP calls to OpenAI API (no external SDK)

### **🔧 Customization & Extensions**

 
#### **Adding New PSPs**
- [ ] Add new PSP data to training dataset
- [ ] Retrain fine-tuned model with expanded data
- [ ] Client surfaces PSPs in real time from `PaymentTransactions` (no static candidate lists)

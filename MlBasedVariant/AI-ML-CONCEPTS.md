# 📚 AI/ML Concepts Explained for Programmers (ML-Based variant)

## **A.0 Overview**
This document explains the key AI/ML concepts used in the PSP Router in programmer-friendly terms. The system uses **ML.NET with LightGBM** to create three specialized ML models that predict PSP performance, eliminating the need for external providers. No advanced mathematics required - just practical understanding of how these concepts work in our payment routing system.


## 🧠 **A.1 ML.NET: The Machine Learning Framework**

### **What is ML.NET?**
ML.NET is Microsoft's open-source machine learning framework for .NET developers. It allows us to build, train, and deploy ML models directly in our C# application without external dependencies. In our PSP Router, we use ML.NET to create three specialized models that predict different aspects of PSP performance.

### **Why ML.NET is Perfect for PSP Routing**
- **No External Dependencies**: Everything runs in-process with our .NET application
- **High Performance**: Optimized for production workloads with fast inference
- **Type Safety**: Strongly-typed C# code with IntelliSense support
- **Easy Deployment**: Models are just files that can be versioned and deployed
- **Real-time Learning**: Models can be retrained with new data without downtime

---

## 🎯 **A.2 Four-Model Architecture: Specialized Prediction System**

### **Why Four Models?**
Instead of one complex model, we use four specialized models, each optimized for a specific prediction task:

1. **Main Model**:
   - **Goal**: Predicts best PSP selection (Binary Classification)
   - **Training Data**: Massive historical dataset from PaymentTransactions table (trained once)
   - **Update Stage**: Never updated - provides stable baseline intelligence

2. **Auxiliary Models**:
   - **Goal**: Provide real-time performance predictions for each PSP
   - **Training Data**: Recent transaction data from PaymentTransactions table (last 7 days)
   - **Update Stage**: Continuously retrained with fresh data when triggers occur
   - **Success Rate Model**: Predicts authorization probability (Binary Classification)
   - **Processing Time Model**: Predicts transaction processing time (Regression)
   - **Health Status Model**: Predicts PSP health classification (Multiclass Classification)

### **Model Architecture**
```
Transaction Input → Feature Engineering → Four Specialized Models → Combined Decision
RouteInput → PspPerformanceFeatures → [Success Rate, Processing Time, Health, Overall Routing] → RouteDecision
```

### **Benefits of Four-Model Approach**
- **Specialized Accuracy**: Each model is optimized for its specific task
- **Independent Training**: Models can be retrained separately based on data availability
- **Fault Tolerance**: If one model fails, others continue working
- **Performance**: Smaller, focused models are faster than one large model
- **Interpretability**: Easier to understand what each model is predicting
- **Layered Decision Making**: Overall routing model combines insights from specialized models

---

## 🌳 **A.3 LightGBM: The Gradient Boosting Algorithm**

### **What is LightGBM?**
LightGBM (Light Gradient Boosting Machine) is a fast, distributed, high-performance gradient boosting framework. It's particularly effective for tabular data like our transaction features and is the core algorithm powering all four of our ML models.

### **How LightGBM Works**
```
Training Data → Decision Trees → Gradient Boosting → Ensemble Model
Features + Labels → Multiple Trees → Learn from Errors → Final Predictor
```

### **Why LightGBM for PSP Routing**
- **Handles Mixed Data Types**: Works well with both numerical and categorical features
- **Feature Importance**: Automatically identifies which features matter most
- **Fast Training**: Efficiently trains on large datasets
- **Robust Predictions**: Less prone to overfitting than other algorithms
- **Production Ready**: Optimized for real-time inference

---

## 🧠 **A.4 ML-Based's Role in PSP Routing: The Intelligent Decision Engine**

### **Primary Function: Intelligent Decision Engine**

The ML models serve as the **brain** of the PSP Router, acting as expert payment routing consultants that make complex, context-aware decisions based on learned patterns from historical transaction data. They're the primary decision makers that consider multiple factors simultaneously and provide reasoning for their choices.

### **Decision Flow**

```
Transaction Request
        ↓
    [Guardrails] ← Filter valid PSPs (supports flag, health status, SCA compliance)
        ↓
    [ML Models] ← Predict success rate, processing time, health status
        ↓
    [Scoring] ← Combine ML predictions with business logic
        ↓
    [Success?] ← ML prediction valid?
        ↓
    [Final Decision] ← Based on ML predictions
        ↓
    [Fallback] ← Deterministic scoring if ML fails
        ↓
    [Final Decision] ← Based on mathematical scoring
```

### **ML-Based Routing Algorithm (Step-by-step)**

1. **Guardrails Applied**
   - Filter PSPs by `Supports` flag (boolean capability indicator)
   - Filter by health status (green/yellow only, excludes red)
   - Filter by SCA compliance (if SCA required for cards, PSP must support 3DS)

2. **ML Predictions Generated**
   - **Success Rate**: Predict authorization probability for each PSP
   - **Processing Time**: Predict transaction processing time
   - **Health Status**: Predict current PSP health classification

3. **Scoring and Selection**
   - Combine ML predictions with business weights
   - Apply business bias and preferences
   - Select PSP with highest combined score

4. **Success or Fallback**
   - If ML predictions valid → return ML-based decision
   - If ML predictions fail → fallback to deterministic scoring

5. **Deterministic Fallback** (if needed)
   - Score PSPs by: `AuthRate30d - (FeeBps/10000.0) - (FixedFee/Amount)`
   - Select highest-scoring PSP
   - Return deterministic decision

#### Code entry points (used by controller)
- `Router.Decide(...)` - Main routing endpoint called by `/api/v1/routing/route`
- `PspCandidateProvider.ProcessFeedback(...)` - Feedback processing called by `/api/v1/routing/feedback`
- `PspCandidateProvider.GetAllCandidates(...)` - PSP candidates retrieval called by `/api/v1/routing/candidates`
- `IPredictionService.IsModelLoaded` - ML model status check called by `/api/v1/routing/ml-status`

### **Deterministic Fallback System**

#### **When Fallback is Used**
- ML models fail to load
- ML predictions are invalid or timeout
- ML models choose an invalid PSP
- ML prediction service is unavailable

#### **Fallback Algorithm**
```csharp
// Mathematical scoring formula
var score = c.AuthRate30d - (c.FeeBps/10000.0) - (double)(c.FixedFee/Math.Max(tx.Amount,1m));

// Select highest-scoring PSP
var best = candidates.OrderByDescending(c => score).First();
```

#### **Fallback Benefits**
- **Reliability**: Always provides a decision
- **Transparency**: Clear mathematical scoring
- **Performance**: Fast execution without ML inference
- **Consistency**: Predictable results for same inputs

---

## 🔧 **A.5 Feature Engineering: Transforming Raw Data into ML Features**

### **What is Feature Engineering?**
Feature engineering is the process of transforming raw transaction data into meaningful features that ML models can understand and learn from. It's like translating business logic into mathematical representations.

### **Our Feature Set**
```csharp
public class PspPerformanceFeatures
{
    // Transaction Context Features
    public float Amount { get; set; }                    // Transaction amount
    public float PaymentMethodId { get; set; }          // Card, PayPal, etc.
    public float CurrencyId { get; set; }               // USD, EUR, etc.
    public float CountryId { get; set; }                // Geographic location
    public float RiskScore { get; set; }                // Risk assessment (0-100)
    public float IsTokenized { get; set; }              // Tokenized payment (0/1)
    public float HasThreeDS { get; set; }               // 3D Secure enabled (0/1)
    public float IsRerouted { get; set; }               // Retry transaction (0/1)
    
    // PSP Identification
    public float PspId { get; set; }                    // PSP identifier
    public string PspName { get; set; }                 // PSP name
    
    // Temporal Features
    public float HourOfDay { get; set; }                // 0-23
    public float DayOfWeek { get; set; }                // 0-6 (Sunday=0)
    public float DayOfMonth { get; set; }               // 1-31
    public float MonthOfYear { get; set; }              // 1-12
    
    // Historical Performance Features
    public float RecentSuccessRate { get; set; }        // Last 7 days auth rate
    public float RecentProcessingTime { get; set; }     // Last 7 days avg time
    public float RecentVolume { get; set; }             // Last 7 days transaction count
    
    // Derived Features
    public float AmountLog { get; set; }                // log10(amount) for normalization
    public float RiskAdjustedAmount { get; set; }       // amount * (1 - risk_score/100)
    public float TimeOfDayCategory { get; set; }        // 0=night, 1=morning, 2=afternoon, 3=evening
}
```

### **Feature Engineering Pipeline**
```csharp
// 1. Concatenate all features into a single vector
var pipeline = _mlContext.Transforms.Concatenate("Features", 
    nameof(PspPerformanceFeatures.Amount),
    nameof(PspPerformanceFeatures.PaymentMethodId),
    nameof(PspPerformanceFeatures.CurrencyId),
    // ... all other features
);

// 2. Normalize features to 0-1 range for better training
.Append(_mlContext.Transforms.NormalizeMinMax("Features"));

// 3. Train the model
.Append(_mlContext.BinaryClassification.Trainers.LightGbm(...));
```

### **Why These Features Matter**
- **Transaction Context**: Amount, payment method, currency affect PSP performance
- **Temporal Patterns**: Time of day, day of week influence success rates
- **Historical Performance**: Recent PSP performance predicts future behavior
- **Risk Factors**: Risk score and tokenization affect authorization rates
- **Derived Features**: Mathematical combinations capture complex relationships

---

## 🔄 **A.6 Real-time Learning: Continuous Model Improvement**

### **Feedback Loop Architecture**
```
Transaction → Routing Decision → PSP Processing → Feedback → Model Update
RouteInput → RouteDecision → PSP Response → TransactionFeedback → Retrain Models
```

### **How Real-time Learning Works**

#### **Two Types of Models Working Together**

**1. Main Model (Trained Once)**
- **Purpose**: One intelligent model trained on huge amounts of historical payment transactions
- **Characteristics**: Very clever and sophisticated, but unaware of real-time feedbacks and changes
- **Training**: Trained once on massive historical dataset from PaymentTransactions table
- **Usage**: Provides baseline intelligent routing decisions

**2. Three Auxiliary Models (Periodic Retraining)**
- **Characteristics**: Retrained periodically (every 24 hours or when triggers occur) using fresh data from database
- **Training**: Retrained using small amount of PaymentTransactions data from monolith's database (last 7 days)
- **Update Triggers**: 
  - Never retrained before
  - Last retraining was more than 24 hours ago
  - Performance predictor indicates retraining needed
- **The 3 Auxiliary Models**:
  - **Success Rate Model**: Predicts authorization probability based on recent performance
  - **Processing Time Model**: Predicts transaction processing time based on recent data  
  - **Health Status Model**: Predicts PSP health classification (green/yellow/red)

#### **PSP Candidates Collection and Usage**

**How PSP Candidates are Built:**
- **Database Query**: Load PSP performance data from PaymentTransactions table
- **Historical Metrics**: Calculate 30-day authorization rates, average processing times, health status
- **Real-time Updates**: Update candidate performance metrics with incoming feedback
- **In-Memory Cache**: Store last 1000 transactions per PSP for immediate updates

**How Candidates are Used:**
- **Guardrails**: Filter candidates by support status, health (green/yellow only), SCA compliance
- **ML Predictions**: Apply auxiliary models to predict success rate, processing time, health
- **Combined Scoring**: Merge main model intelligence with auxiliary model predictions
- **Final Selection**: Choose best PSP based on combined insights

#### **Combination of Candidates, Feedbacks, and 3 Models**

**Real-time Learning Flow:**
1. **Transaction Processing**: Route using main model + auxiliary model predictions
2. **Feedback Receiving**: Receive transaction outcome (success/failure, processing time)
3. **Cache Update**: Store feedback in in-memory cache (last 1000 transactions per PSP)
4. **Auxiliary Model Retraining**: Retrain 3 auxiliary models with fresh data from database when triggers occur
5. **Continuous Improvement**: System gets smarter with each transaction


### **Cached Feedback System**
The system maintains an in-memory cache of recent transaction feedback for each PSP. When feedback is received, it's stored in the cache (limited to last 1000 transactions per PSP) and used to immediately update cached performance metrics like success rate, processing time, and health status. This provides real-time performance updates without requiring database queries for every prediction.

### **Incremental Learning**
The system supports incremental learning where auxiliary models can be updated with new feedback data without full retraining. When feedback is received, the system checks if incremental updates are needed based on data volume and time thresholds. This allows for faster adaptation to changing PSP performance patterns while maintaining model stability.

### **Guardrails System**

#### **Guardrail Logic**
The guardrail system applies multiple filters to ensure only valid PSPs are considered for routing. It filters PSPs by support status (must be marked as supported), health status (only green or yellow PSPs allowed), and SCA compliance (if transaction requires SCA and is a card payment, PSP must support 3DS). This pre-filtering reduces decision complexity and ensures compliance with business rules and regulatory requirements.

#### **Guardrail Types**
1. **Supports Flag**: Boolean indicator that PSP is available for routing
2. **Health Status**: Only "green" or "yellow" PSPs allowed (excludes "red")
3. **SCA Compliance**: If transaction requires SCA and is a card payment, PSP must support 3DS

#### **Guardrail Benefits**
- **Safety**: Prevents routing to unhealthy or unsupported PSPs
- **Compliance**: Ensures regulatory requirements are met
- **Reliability**: Filters out problematic PSPs before AI decision
- **Performance**: Reduces decision complexity by pre-filtering candidates


### **ML-Based's Decision Process**

#### **1. Context Analysis**
- Transaction characteristics (amount, risk, currency, method, BIN)
- Current PSP snapshots (health, auth rates, fees, capabilities)

#### **2. Pattern Application**
- **Compliance Enforcement**: SCA/3DS requirements using current transaction and PSP data
- **PSP Performance Analysis**: Using current auth rates, fees, and health status
- **Risk Assessment**: Using current transaction risk score and pre-trained risk patterns
- **Multi-factor Decision Making**: Balancing compliance, performance, and risk using pre-trained patterns

#### **3. Intelligent Reasoning**
The fine-tuned model can handle complex scenarios using pre-trained patterns:
- "High-risk transaction (RiskScore=85) needs 3DS support" (using current risk score + pre-trained patterns)
- "Low auth rate PSP (0.75) not suitable for high-value transaction" (using current PSP data + pre-trained patterns)
- "Weekend transactions have different success patterns" (pre-trained temporal patterns)

#### **4. Structured Decision Output**
```json
{
  "Schema_Version": "1.0",
  "Decision_Id": "route_20241201_001",
  "Candidate": "Stripe",
  "Alternates": ["Adyen", "PayPal"],
  "Reasoning": "Chose Stripe over Adyen because: 1) Transaction requires SCA and Stripe has better 3DS performance based on historical data, 2) Fee difference is minimal for this amount",
  "Guardrail": "none",
  "Constraints": {
    "Must_Use_3ds": true,
    "Retry_Window_Ms": 8000,
    "Max_Retries": 1
  },
  "Features_Used": ["sca_required=true", "auth_rate=0.87", "fee_bps=200"]
}
```

#### **RouteDecision Structure Explained**
- **Schema_Version**: Version of the decision format (currently "1.0")
- **Decision_Id**: Unique identifier for tracking and audit purposes
- **Candidate**: The chosen PSP for this transaction
- **Alternates**: Backup PSPs in case the primary choice fails
- **Reasoning**: Human-readable explanation of the decision logic
- **Guardrail**: Type of constraint applied ("none", "compliance", "health", "capability")
- **Constraints**: Transaction-specific requirements (3DS, retry settings)
- **Features_Used**: Key factors that influenced the decision

### **Advanced ML-Based Capabilities**

#### **Learned Knowledge from Training Data**
The fine-tuned model has learned from historical transaction data:
- **PSP Health Patterns**: Success rates and response times for different scenarios
- **Fee Structures**: Actual costs and outcomes from past transactions
- **PSP Capabilities**: Which PSPs work best for different payment methods and regions
- **Complex Routing Logic**: Multi-factor decision making from training examples

#### **Complex Scenario Handling**
The fine-tuned model excels at handling edge cases using learned patterns:
- **Compliance Conflicts**: "Merchant has 3D Secure preference but transaction is low-risk" (learned from training data)
- **Trade-off Analysis**: "Fee difference is $0.50 but auth rate difference is 5%" (learned from outcomes)
- **Temporal Patterns**: "Historical data shows weekend transactions perform differently" (learned from temporal patterns)
- **Multi-factor Decisions**: Balancing auth rates, fees, compliance, and preferences (learned from training examples)

### **Summary: ML-Based's Value**

The fine-tuned model transforms the PSP Router into an **intelligent payment expert** that:

1. **🧠 Thinks**: Analyzes complex scenarios with multiple factors using learned patterns
2. **📋 Applies Rules**: Enforces compliance and business logic learned from training data
3. **🔧 Adapts**: Handles edge cases and complex scenarios using historical knowledge
4. **📊 Explains**: Provides clear reasoning for decisions based on training data
5. **🎯 Optimizes**: Makes decisions using pre-trained patterns from historical data

This makes the system capable of handling real-world payment routing scenarios that require expert-level decision making, using pre-trained patterns from historical transaction data.

## 🎯 **A.7 Benefits of ML.NET-Based Approach**

### **Why This Approach**
- **No External Dependencies**: Everything runs in-process
- **High Performance**: Optimized for production workloads
- **Real-time Learning**: Models improve with new data
- **Type Safety**: Strongly-typed C# code
- **Easy Deployment**: Models are just files
- **Cost Effective**: No external API costs
- **Data Privacy**: All data stays within your infrastructure

### **What the Models Learn**
- **PSP Performance Patterns**: Success rates and response times from historical data
- **Transaction Characteristics**: How different transaction types affect PSP performance
- **Temporal Patterns**: Time-based variations in PSP performance
- **Risk Assessment**: How risk factors influence authorization rates
- **Compliance Requirements**: SCA/3DS requirements and their impact

### **Operational Benefits**
- **Automated Retraining**: Models improve automatically with new data
- **Performance Monitoring**: Real-time tracking of model accuracy
- **Fault Tolerance**: Fallback to deterministic routing if ML fails
- **Scalability**: Models can handle high transaction volumes
- **Maintainability**: Easy to update and improve models

## 🎓 **A.8 Training Data: The Foundation of ML Models**

### **What is Training Data?**
Training data is the **labeled examples** that teach our ML models how to make predictions. Each example shows a transaction input and the actual PSP performance outcome, allowing the models to learn patterns and relationships.

### **How Training Data Works**
```
Historical Transaction → Actual Outcome → Training Example
"Visa $150, US merchant" → "Success: 1, Time: 1200ms, Health: Green" → Model learns this pattern
```

### **Training Data Source**
```sql
-- Extract training data from PaymentTransactions table
SELECT 
    pt.PaymentTransactionId,
    pt.Amount,
    pt.PaymentMethodId,
    pt.CurrencyId,
    pt.PaymentGatewayId as PspId,
    pt.PaymentTransactionStatusId,
    pt.DateCreated,
    pt.DateStatusLastUpdated,
    CASE WHEN pt.PaymentTransactionStatusId IN (5, 7, 9) THEN 1 ELSE 0 END as IsSuccessful,
    DATEDIFF(MILLISECOND, pt.DateCreated, pt.DateStatusLastUpdated) as ProcessingTimeMs
FROM PaymentTransactions pt
WHERE pt.DateCreated >= DATEADD(MONTH, -3, GETDATE())
  AND pt.PaymentGatewayId IS NOT NULL
```

### **Data Quality Requirements**
- **Volume**: Minimum 1,000 examples per PSP, recommended 10,000+
- **Diversity**: Cover different transaction types, amounts, currencies, times
- **Freshness**: Recent data reflecting current PSP performance
- **Accuracy**: Only use reliable, validated transaction outcomes

### **Feature Engineering from Raw Data**
```csharp
// Convert raw transaction data to ML features
var features = new PspPerformanceFeatures
{
    Amount = (float)transaction.Amount,
    PaymentMethodId = transaction.PaymentMethodId,
    CurrencyId = transaction.CurrencyId,
    RiskScore = transaction.RiskScore,
    IsTokenized = transaction.IsTokenized ? 1.0f : 0.0f,
    HourOfDay = transaction.DateCreated.Hour,
    DayOfWeek = (float)transaction.DateCreated.DayOfWeek,
    AmountLog = (float)Math.Log10(Math.Max((float)transaction.Amount, 1)),
    IsSuccessful = transaction.IsSuccessful,
    ProcessingTimeMs = transaction.ProcessingTimeMs
};
```


### **Training Process**

#### **1. Load Training Data**
```csharp
// Load training data from database
var trainingData = await GetTrainingDataFromDatabase();
```

#### **2. Create Training Pipeline**
```csharp
// Create ML.NET training pipeline
var pipeline = _mlContext.Transforms.Concatenate("Features", featureColumns)
    .Append(_mlContext.Transforms.NormalizeMinMax("Features"))
    .Append(_mlContext.BinaryClassification.Trainers.LightGbm(...));
```

#### **3. Train and Evaluate**
```csharp
// Train the model
var trainedModel = pipeline.Fit(trainTestSplit.TrainSet);

// Evaluate model performance
var predictions = trainedModel.Transform(trainTestSplit.TestSet);
var metrics = _mlContext.BinaryClassification.Evaluate(predictions);
```

#### **4. Save Model**
```csharp
// Save trained model to disk
_mlContext.Model.Save(trainedModel, trainingData.Schema, modelPath);
```

### **Benefits of Training Data**

1. **🎯 Domain Expertise**: Models learn payment routing patterns
2. **📊 Pattern Recognition**: Identifies successful routing strategies
3. **🔧 Consistency**: Standardized decision-making process
4. **🚀 Performance**: Optimized for payment routing scenarios
5. **📈 Scalability**: Handles diverse transaction types

### **In Summary**
Training data is the **foundation** that transforms ML.NET into a specialized payment routing expert. By learning from historical transaction outcomes, the ML models become capable of making intelligent routing decisions that optimize for authorization rates, fees, and compliance! 🎓✨

---

## 🎯 **Summary**

This document has covered the key AI/ML concepts used in the PSP Router:

1. **🧠 ML.NET**: The machine learning framework that powers our system
2. **🎯 Four-Model Architecture**: Specialized models for different prediction tasks
3. **🌳 LightGBM**: The gradient boosting algorithm for all our models
4. **🔧 Feature Engineering**: Transforming raw data into ML features
5. **🔄 Real-time Learning**: Continuous model improvement with feedback
6. **🧠 ML-Based Routing**: How ML models make intelligent routing decisions

These concepts work together to create an intelligent payment routing system that uses ML.NET and LightGBM to make optimal routing decisions based on learned patterns from historical transaction data! 🚀

---

**Last Updated:** December 2024  
**Status:** ✅ ML.NET-based system implemented and working  
**Architecture:** Four specialized LightGBM models with real-time learning

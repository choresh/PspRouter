# 📚 AI/ML Concepts Explained for Programmers (ML-Based variant)

## **1. General**

### **Overview**
This document explains the key AI/ML concepts used in the PSP Router in programmer-friendly terms. The system uses **ML.NET with LightGBM** to create four specialized ML models that predict PSP performance, eliminating the need for external providers.

### **ML.NET: The Machine Learning Framework**
ML.NET is Microsoft's open-source machine learning framework for .NET developers. It allows us to build, train, and deploy ML models directly in our C# application without external dependencies.

**Why ML.NET is Perfect for PSP Routing:**
- **No External Dependencies**: Everything runs in-process with our .NET application
- **High Performance**: Optimized for production workloads with fast inference
- **Type Safety**: Strongly-typed C# code with IntelliSense support
- **Easy Deployment**: Models are just files that can be versioned and deployed

### **LightGBM: The Gradient Boosting Algorithm**
LightGBM (Light Gradient Boosting Machine) is a fast, distributed, high-performance gradient boosting framework. It's particularly effective for tabular data like our transaction features and is the core algorithm powering all four of our ML models.

**Why LightGBM for PSP Routing:**
- **Handles Mixed Data Types**: Works well with both numerical and categorical features
- **Feature Importance**: Automatically identifies which features matter most
- **Fast Training**: Efficiently trains on large datasets
- **Robust Predictions**: Less prone to overfitting than other algorithms

### **Four-Model Architecture**
Instead of one complex model, we use four specialized models, each optimized for a specific prediction task:

1. **Main Model**: Predicts best PSP selection (trained once on massive historical dataset)
2. **Success Rate Model**: Predicts authorization probability (retrained periodically)
3. **Processing Time Model**: Predicts transaction processing time (retrained periodically)
4. **Health Status Model**: Predicts PSP health classification (retrained periodically)

---

## **2. Main Flow (The Main Model, The Candidates)**

### **PSP Candidates Collection**
**How PSP Candidates are Built:**
- **Database Query**: Load PSP performance data from PaymentTransactions table
- **Historical Metrics**: Calculate 30-day authorization rates, average processing times, health status
- **In-Memory Cache**: Store candidates for fast access during routing

**How Candidates are Used:**
- **Guardrails**: Filter candidates by support status, health (green/yellow only), SCA compliance
- **ML Predictions**: Apply main model to predict best PSP selection
- **Final Selection**: Choose best PSP based on ML predictions

### **Main Model Decision Flow**
```
Transaction Request
        ↓
    [Guardrails] ← Filter valid PSPs (supports flag, health status, SCA compliance)
        ↓
    [Main Model] ← Predict best PSP selection
        ↓
    [Final Decision] ← Based on ML predictions
        ↓
    [Fallback] ← Deterministic scoring if ML fails
```

### **Guardrails System**
The guardrail system applies multiple filters to ensure only valid PSPs are considered for routing:
1. **Supports Flag**: Boolean indicator that PSP is available for routing
2. **Health Status**: Only "green" or "yellow" PSPs allowed (excludes "red")
3. **SCA Compliance**: If transaction requires SCA and is a card payment, PSP must support 3DS

### **Code Entry Points (used by controller)**
- `Router.Decide(...)` - Main routing endpoint called by `/api/v1/routing/route`
- `PspCandidateProvider.GetAllCandidates(...)` - PSP candidates retrieval called by `/api/v1/routing/candidates`
- `IPredictionService.IsModelLoaded` - ML model status check called by `/api/v1/routing/ml-status`

---

## **3. Real-time Enhancement (The Feedback, The 3 Models, The Improvement of Candidates)**

### **Feedback Loop Architecture**
```
Transaction → Routing Decision → PSP Processing → Feedback
RouteInput → RouteDecision → PSP Response → TransactionFeedback
```

**Model Update (Separate Process):**
```
Feedback → Check Conditions → Retrain Models (if conditions met)
TransactionFeedback → Update Triggers → Retrain Models (separate execution)
```

### **How Real-time Learning Works**

#### **The 3 Auxiliary Models**
- **Success Rate Model**: Predicts authorization probability based on recent performance
- **Processing Time Model**: Predicts transaction processing time based on recent data  
- **Health Status Model**: Predicts PSP health classification (green/yellow/red)

#### **Real-time Learning Flow**
1. **Transaction Processing**: Route using main model + auxiliary model predictions
2. **Feedback Receiving**: Receive transaction outcome (success/failure, processing time)
3. **Cache Update**: Store feedback in in-memory cache (last 1000 transactions per PSP)
4. **Check Retraining Conditions**: Evaluate if models need retraining (separate process)
5. **Auxiliary Model Retraining**: Retrain 3 auxiliary models with fresh data from database (only when conditions are met)

### **Cached Feedback System**
The system maintains an in-memory cache of recent transaction feedback for each PSP. When feedback is received, it's stored in the cache (limited to last 1000 transactions per PSP) and used to immediately update cached performance metrics like success rate, processing time, and health status.

### **Incremental Learning**
The system supports incremental updates where cached performance values are recalculated from recent feedback data. When feedback is received, the system checks if cached values need updating based on data volume and time thresholds. This is not actual ML model retraining - it's just updating cached performance metrics.

### **Update Triggers**
- Never retrained before
- Last retraining was more than 24 hours ago
- Performance predictor indicates retraining needed

### **Code Entry Points (used by controller)**
- `PspCandidateProvider.ProcessFeedback(...)` - Feedback processing called by `/api/v1/routing/feedback`

### **Training Data Source**
The auxiliary models are retrained using small amount of PaymentTransactions data from monolith's database (last 7 days).

---
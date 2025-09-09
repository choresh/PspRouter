# 📚 AI/ML Concepts Explained for Programmers (Fine-Tuned Model variant)

## **A.0 Overview**
This document explains the key AI/ML concepts used in the PSP Router in programmer-friendly terms. The system uses a **fine-tuned model approach** where the AI learns everything from historical transaction data, eliminating the need for external providers and tool calling. No advanced mathematics required - just practical understanding of how these concepts work in our payment routing system.


## 🧠 **A.7 LLM (Large Language Model): The AI Brain**

### **What is an LLM?**
A Large Language Model is an AI system trained on vast amounts of text data that can understand and generate human-like text. In our case, we use a **fine-tuned GPT model** that has been specifically trained on historical payment routing data to make intelligent routing decisions.

### **How It Works**
```
Input: Transaction context + PSP candidates
↓
Fine-Tuned Model Processing: Analyzes context using learned patterns from historical data
↓
Output: Structured decision with reasoning based on training data
```

### **Why It's Powerful**
- **Context Understanding**: Considers complex relationships learned from training data
- **Reasoning**: Can explain its decisions based on historical patterns
- **Flexibility**: Handles edge cases using learned knowledge
- **Domain Expertise**: Specialized knowledge from payment routing training data

### **In PSP Router**
```csharp
// Fine-tuned model receives comprehensive context
var context = {
    Transaction: currentTx,
    Candidates: validCandidates,
    MerchantPreferences: preferences
};

// Fine-tuned model makes intelligent decision using learned patterns
var decision = await fineTunedModel.CompleteJsonAsync(systemPrompt, userInstruction, context);
// Result: {"schema_version":"1.0","decision_id":"route_001","candidate":"Stripe","alternates":["Adyen"],"reasoning":"Best for high-risk SCA transactions based on historical data","guardrail":"none","constraints":{"must_use_3ds":true,"retry_window_ms":8000,"max_retries":1},"features_used":["sca_required=true","auth_rate=0.89"]}
```

---

## 🧠 **A.9 Fine-Tuned Model's Role in PSP Routing: The Intelligent Decision Engine**

### **Primary Function: Intelligent Decision Engine**

The fine-tuned model serves as the **brain** of the PSP Router, acting as an expert payment routing consultant that makes complex, context-aware decisions based on learned patterns from historical transaction data. It's the primary decision maker that considers multiple factors simultaneously and provides reasoning for its choices.

### **Decision Flow**

```
Transaction Request
        ↓
    [Guardrails] ← Filter valid PSPs (supports flag, health status, SCA compliance)
        ↓
    [Fine-Tuned Model] ← Primary intelligent routing using learned patterns from training
        ↓
    [Success?] ← Model response valid?
        ↓
    [Final Decision] ← Based on learned patterns
        ↓
    [Fallback] ← Deterministic scoring if model fails
        ↓
    [Final Decision] ← Based on mathematical scoring (auth rate - fees)
```

### **Fine-Tuned Model Routing Algorithm (Step-by-step)**

1. **Guardrails Applied**
   - Filter PSPs by `Supports` flag (boolean capability indicator)
   - Filter by health status (green/yellow only, excludes red)
   - Filter by SCA compliance (if SCA required for cards, PSP must support 3DS)

2. **Inputs prepared**
   - Valid PSP candidates after guardrails
   - Transaction context and PSP performance data

3. **Context built for the fine-tuned model**
   - Serialize a structured object with: `Transaction`, `Candidates`, `Weights`

4. **Prompting**
   - Construct a strict system prompt with rules and response schema
   - Provide a short user instruction: "Route this payment…"

5. **Inference**
   - Call the fine-tuned model to produce a JSON decision using learned patterns from training data
   - Model uses current transaction context, PSP snapshots, and product-defined `Weights`

6. **Validation**
   - Parse JSON to a `RouteDecision`
   - Ensure the chosen `Candidate` is one of the valid PSPs

7. **Success or Fallback**
   - If valid → return the fine-tuned model decision
   - If invalid/failed → fallback to deterministic scoring

8. **Deterministic Fallback** (if needed)
   - Score PSPs by: `AuthRate30d - (FeeBps/10000.0) - (FixedFee/Amount)`
   - Select highest-scoring PSP
   - Return deterministic decision

#### Code entry points
- `PspRouter.Decide(...)` orchestrates fine-tuned model routing with fallback
- `PspRouter.DecideWithLLM(...)` handles fine-tuned model inference
- `PspRouter.ScoreDeterministically(...)` provides mathematical fallback
- `PspRouter.BuildSystemPrompt()` defines the strict schema and rules

### **Deterministic Fallback System**

#### **When Fallback is Used**
- Fine-tuned model fails to respond
- Model response is invalid JSON
- Model chooses an invalid PSP
- Model response times out

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
- **Performance**: Fast execution without AI inference
- **Consistency**: Predictable results for same inputs

### **Guardrails System**

#### **Guardrail Logic**
```csharp
var valid = ctx.Candidates
    .Where(c => c.Supports)                    // PSP must be marked as supported
    .Where(c => c.Health is "green" or "yellow") // Only healthy PSPs
    .Where(c => !(ctx.Tx.SCARequired && ctx.Tx.PaymentMethodId == 1 && !c.Supports3DS)) // SCA compliance
    .ToList();
```

#### **Guardrail Types**
1. **Supports Flag**: Boolean indicator that PSP is available for routing
2. **Health Status**: Only "green" or "yellow" PSPs allowed (excludes "red")
3. **SCA Compliance**: If transaction requires SCA and is a card payment, PSP must support 3DS

#### **Guardrail Benefits**
- **Safety**: Prevents routing to unhealthy or unsupported PSPs
- **Compliance**: Ensures regulatory requirements are met
- **Reliability**: Filters out problematic PSPs before AI decision
- **Performance**: Reduces decision complexity by pre-filtering candidates

### **What the Fine-Tuned Model Receives**

```json
{
  "Transaction": {
    "MerchantId": "M123",
    "Amount": 150.00,
    "CurrencyId": 1, 
    "PaymentMethodId": 1,
    "PaymentCardBin": "411111",
    "ThreeDSTypeId": null,
    "IsTokenized": false,
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
  "Weights": {
    "AuthWeight": 1.2,
    "FeeBpsWeight": 0.8,
    "FixedFeeWeight": 0.5,
    "Supports3DSBonusWhenSCARequired": 0.0,
    "RiskScorePenaltyPerPoint": 0.0,
    "HealthYellowPenalty": 0.0,
    "BusinessBiasWeight": 0.1,
    "BusinessBias": { "Adyen": 0.02, "Stripe": -0.01 },
  }
}
```

### **Fine-Tuned Model's Decision Process**

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

### **Advanced Fine-Tuned Model Capabilities**

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

### **Summary: Fine-Tuned Model's Value**

The fine-tuned model transforms the PSP Router into an **intelligent payment expert** that:

1. **🧠 Thinks**: Analyzes complex scenarios with multiple factors using learned patterns
2. **📋 Applies Rules**: Enforces compliance and business logic learned from training data
3. **🔧 Adapts**: Handles edge cases and complex scenarios using historical knowledge
4. **📊 Explains**: Provides clear reasoning for decisions based on training data
5. **🎯 Optimizes**: Makes decisions using pre-trained patterns from historical data

This makes the system capable of handling real-world payment routing scenarios that require expert-level decision making, using pre-trained patterns from historical transaction data.

## ⚙️ **A.10 Fine-Tuned Model Approach**

### **What "fine-tuned" means here**
- Starts from a foundation LLM (e.g., GPT-3.5-turbo) and adapts it with your labeled payment routing data
- Decision quality comes from learned weights + strict JSON schema + runtime context
- Model learns PSP patterns, fees, capabilities, and routing logic from historical transaction data
- No external providers needed - everything is learned from training data

### **Why this approach**
- **Consistency**: Higher schema adherence and decision consistency
- **Efficiency**: Lower tokens and latency at scale
- **Control**: Encodes domain expertise directly in model weights
- **Simplicity**: No external API dependencies or real-time data fetching
- **Reliability**: No external service failures or timeouts

### **What the model learns**
- **PSP Health Patterns**: Success rates and response times from historical data
- **Fee Structures**: Actual costs and outcomes from past transactions
- **PSP Capabilities**: Which PSPs work best for different payment methods, currencies, regions
- **Complex Routing Logic**: Multi-factor decision making from training examples
- **Compliance Rules**: SCA/3DS requirements and regulatory patterns
- **Merchant Patterns**: Patterns from merchant-specific routing outcomes

### **Training data format**
```json
{
  "messages": [
    {
      "role": "system",
      "content": "You are a payment routing expert. Route transactions to optimal PSPs based on historical patterns."
    },
    {
      "role": "user", 
      "content": "Route this transaction: {\"merchantId\":\"M001\",\"amount\":150.00,\"currencyId\":1,\"paymentMethodId\":1,\"paymentCardBin\":\"411111\",\"scaRequired\":false,\"riskScore\":15}"
    },
    {
      "role": "assistant",
      "content": "{\"schema_version\":\"1.0\",\"decision_id\":\"route_20241201_001\",\"candidate\":\"Stripe\",\"alternates\":[\"Adyen\"],\"reasoning\":\"Chose Stripe for optimal auth rate and fee structure for this transaction type\",\"guardrail\":\"none\",\"constraints\":{\"must_use_3ds\":false,\"retry_window_ms\":8000,\"max_retries\":1},\"features_used\":[\"auth_rate=0.89\",\"fee_bps=200\"]}"
    }
  ]
}
```

### **Trade-offs**
- **Data/MLOps**: Requires curated dataset, training, evaluation, versioning
- **Drift**: Periodic re-training when transaction patterns change
- **Training Time**: Initial training and retraining cycles
- **Data Quality**: Requires high-quality labeled routing examples

### **When to fine-tune**
- You have enough high-quality labeled routing examples (1000+ recommended)
- Transaction patterns are relatively stable
- You want consistent, fast routing decisions
- You want to eliminate external API dependencies

### **Operational guidance**
- **Training**: Use PspRouter.Trainer service for automated training workflow
- **Versioning**: Track fine-tune versions and rollout gradually (shadow/A-B)
- **Observability**: Log model latency, token usage, decision accuracy
- **Retraining**: Schedule periodic retraining with new transaction data

### **Evaluation checklist**
- Authorization rate (primary), fee impact, decision accuracy
- Decision time (P50/P95), schema validity, reasoning quality
- A/B or shadow tests before rollout
- Model performance on holdout test data

### **Security & privacy**
- Avoid PII; send only needed aggregates (e.g., BIN prefix, currency IDs, ranges)
- Redact merchant and cardholder identifiers; follow data residency policies
- Review provider data retention settings (ephemeral vs stored)
- Use de-identified training data with proper data governance

## 🎓 **A.11 Training Data: The Foundation of Fine-Tuned Models**

### **What is Training Data?**
Training data is the **labeled examples** that teach the fine-tuned model how to make routing decisions. Each example shows a transaction input and the optimal routing decision, allowing the model to learn patterns and relationships.

### **How Training Data Works**
```
Historical Transaction → Optimal Decision → Training Example
"Visa $150, US merchant" → "Route to Stripe" → Model learns this pattern
```

### **Training Data Format**

#### **JSONL (JSON Lines) Format**
Each line is a separate training example:
```json
{"messages": [{"role": "system", "content": "You are a payment routing expert..."}, {"role": "user", "content": "Route this transaction: {...}"}, {"role": "assistant", "content": "{\"candidate\":\"Stripe\",\"reasoning\":\"...\"}"}]}
{"messages": [{"role": "system", "content": "You are a payment routing expert..."}, {"role": "user", "content": "Route this transaction: {...}"}, {"role": "assistant", "content": "{\"candidate\":\"Adyen\",\"reasoning\":\"...\"}"}]}
```

### **Data Collection Process**

#### **1. Historical Transaction Analysis**
```sql
-- Extract successful routing decisions from PaymentTransactions table
SELECT 
    MerchantId, Amount, CurrencyId, PaymentMethodId, PaymentCardBin,
    PspName, AuthorizationResult, ProcessingTimeMs, FeeAmount
FROM PaymentTransactions 
WHERE AuthorizationResult = 'Authorized'
  AND ProcessingTimeMs < 5000  -- Only fast transactions
ORDER BY ProcessingTimeMs ASC
```

#### **2. Data Preparation**
- **De-identify**: Remove PII, use BIN prefixes instead of full card numbers
- **Normalize**: Standardize currency IDs, payment method IDs
- **Label**: Create optimal routing decisions based on outcomes
- **Validate**: Ensure data quality and consistency

#### **3. Training Example Creation**
```csharp
// Convert historical transaction to training example
var trainingExample = new {
    messages = new[] {
        new { role = "system", content = systemPrompt },
        new { role = "user", content = $"Route this transaction: {JsonSerializer.Serialize(transaction)}" },
        new { role = "assistant", content = JsonSerializer.Serialize(optimalDecision) }
    }
};
```

### **Quality Requirements**

#### **Data Volume**
- **Minimum**: 1,000 high-quality examples
- **Recommended**: 10,000+ examples for robust performance
- **Optimal**: 50,000+ examples covering diverse scenarios

#### **Data Diversity**
- **Payment Methods**: Card, PayPal, Klarna, etc.
- **Currencies**: USD, EUR, GBP, etc.
- **Amounts**: Low-value to high-value transactions
- **Regions**: Different countries and regulatory environments
- **Risk Levels**: Low-risk to high-risk transactions

#### **Data Quality**
- **Accuracy**: Only use successful routing decisions
- **Consistency**: Standardized decision format and reasoning
- **Completeness**: All required fields present and valid
- **Freshness**: Recent data reflecting current PSP performance

### **Training Process**

#### **1. Data Upload**
```csharp
// Upload training data to OpenAI
var fileId = await trainingService.UploadFileToOpenAIAsync(trainingDataJsonl);
```

#### **2. Fine-Tuning Job Creation**
```csharp
// Create fine-tuning job
var jobId = await trainingService.CreateFineTuningJobViaHttpAsync(fileId);
```

#### **3. Training Monitoring**
```csharp
// Monitor training progress
var jobStatus = await trainingService.GetFineTuningJobDetailsAsync(jobId);
// Status: "validating_files" → "queued" → "running" → "succeeded"
```

#### **4. Model Deployment**
```csharp
// Use fine-tuned model
var modelId = "ft:gpt-3.5-turbo:your-org:psp-router:abc123";
var chatClient = new OpenAIChatClient(apiKey, model: modelId);
```

### **Benefits of Training Data**

1. **🎯 Domain Expertise**: Model learns payment routing patterns
2. **📊 Pattern Recognition**: Identifies successful routing strategies
3. **🔧 Consistency**: Standardized decision-making process
4. **🚀 Performance**: Optimized for payment routing scenarios
5. **📈 Scalability**: Handles diverse transaction types

### **In Summary**
Training data is the **foundation** that transforms a general-purpose LLM into a specialized payment routing expert. By learning from historical transaction outcomes, the fine-tuned model becomes capable of making intelligent routing decisions that optimize for authorization rates, fees, and compliance! 🎓✨

---

## 🎯 **Summary**

This document has covered the key AI/ML concepts used in the PSP Router:

1. **🧠 Fine-Tuned Model**: The AI brain that makes intelligent routing decisions using learned patterns
2. **🧠 Fine-Tuned Model's Role**: Detailed explanation of how the model serves as the intelligent decision engine
3. **⚙️ Fine-Tuned Model Approach**: Comprehensive guide to the fine-tuning methodology
4. **🎓 Training Data**: The foundation that teaches the model payment routing expertise

These concepts work together to create an intelligent payment routing system that uses pre-trained patterns from historical transaction data to make optimal routing decisions! 🚀

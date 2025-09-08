# 📚 AI/ML Concepts Explained for Programmers (Fine-Tuned Model variant)

## **A.0 Overview**
This document explains the key AI/ML concepts used in the PSP Router in programmer-friendly terms. No advanced mathematics required - just practical understanding of how these concepts work in our payment routing system.


## 🧠 **A.7 LLM (Large Language Model): The AI Brain**

### **What is an LLM?**
A Large Language Model is an AI system trained on vast amounts of text data that can understand and generate human-like text. In our case, we use GPT-4 to make intelligent routing decisions.

### **How It Works**
```
Input: Transaction context + PSP candidates
↓
LLM Processing: Analyzes context, considers options, applies reasoning
↓
Output: Structured decision with reasoning
```

### **Why It's Powerful**
- **Context Understanding**: Considers complex relationships
- **Reasoning**: Can explain its decisions
- **Flexibility**: Handles edge cases and new scenarios
- **Learning**: Improves with more context and examples

### **In PSP Router**
```csharp
// LLM receives comprehensive context
var context = {
    Transaction: currentTx,
    Candidates: validCandidates,
    MerchantPreferences: preferences
};

// LLM makes intelligent decision
var decision = await llm.CompleteJsonAsync(systemPrompt, userInstruction, context);
// Result: {"candidate": "Stripe", "reasoning": "Best for high-risk SCA transactions"}
```

---

## 🧠 **A.9 LLM's Role in PSP Routing: The Intelligent Decision Engine**

### **Primary Function: Intelligent Decision Engine**

The LLM serves as the **brain** of the PSP Router, acting as an expert payment routing consultant that makes complex, context-aware decisions. It's the primary decision maker that considers multiple factors simultaneously and provides reasoning for its choices.

### **Decision Flow**

```
Transaction Request
        ↓
    [Guardrails] ← Compliance checks, health status
        ↓
    [LLM Decision] ← Primary intelligent routing
        ↓
    [Fallback] ← Deterministic scoring if LLM fails
        ↓
    [Final Decision]
```

### **LLM Routing Algorithm (Step-by-step)**

1. **Inputs prepared**
   - Valid PSP candidates after guardrails (capabilities, health, SCA) are applied
   - Transaction context and merchant preferences

2. **Context built for the LLM**
   - Serialize a structured object with: `Transaction`, `Candidates`, `MerchantPreferences`

3. **Prompting**
   - Construct a strict system prompt with rules and response schema
   - Provide a short user instruction: “Route this payment…”

4. **Inference**
   - Call the chat model to produce a JSON decision; tools may be available for health/fee lookups

5. **Validation**
   - Parse JSON to a `RouteDecision`
   - Ensure the chosen `Candidate` is one of the valid PSPs

6. **Return or Fallback**
   - If valid → return the decision
   - If missing/invalid → return null, triggering deterministic fallback in the caller

7. **Output**
   - A structured `RouteDecision` with `Candidate`, `Alternates`, `Reasoning`, `Guardrail`, `Constraints`, and `Features_Used`

#### Code entry points
- `PspRouter.DecideAsync(...)` orchestrates LLM-first with fallback
- `PspRouter.DecideWithLLMAsync(...)` builds context, prompts, calls the model, validates
- `PspRouter.BuildSystemPrompt()` defines the strict schema and rules

### **What the LLM Receives**

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
  }
}
```

### **LLM's Decision Process**

#### **1. Context Analysis**
- Transaction characteristics (amount, risk, currency, method)
- PSP capabilities and real-time health status
- Merchant preferences and business rules

#### **2. Business Logic Application**
- **Compliance Enforcement**: SCA/3DS requirements, payment method support
- **Health Checks**: Only routes to healthy PSPs
- **Preference Handling**: Considers merchant fee vs auth rate preferences
- **Risk Management**: Applies risk-based routing rules

#### **3. Intelligent Reasoning**
The LLM can handle complex scenarios like:
- "High-risk transaction needs 3DS support"
- "Merchant prefers low fees, but auth rate is more important for this amount"
- "Weekend transactions have different patterns"

#### **4. Structured Decision Output**
```json
{
  "Candidate": "Stripe",
  "Reasoning": "Chose Stripe over Adyen because: 1) Transaction requires SCA and Stripe has better 3DS performance, 2) Fee difference is minimal for this amount",
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

#### **Complex Scenario Handling**
The LLM excels at handling edge cases:
- **Compliance Conflicts**: "Merchant has 3D Secure preference but transaction is low-risk"
- **Trade-off Analysis**: "Fee difference is $0.50 but auth rate difference is 5%"
- **Temporal Patterns**: "Historical data shows weekend transactions perform differently"
- **Multi-factor Decisions**: Balancing auth rates, fees, compliance, and preferences

### **Summary: LLM's Value**

The LLM transforms the PSP Router into an **intelligent payment expert** that:

1. **🧠 Thinks**: Analyzes complex scenarios with multiple factors
2. **📋 Applies Rules**: Enforces compliance and business logic
3. **🔧 Adapts**: Handles edge cases and complex scenarios
4. **📊 Explains**: Provides clear reasoning for decisions
5. **🛡️ Ensures Safety**: Deterministic fallback when needed

This makes the system capable of handling real-world payment routing scenarios that require expert-level decision making.

## ⚙️ **A.10 Fine-Tuned Model Approach **

### **What “fine-tuned” means here**
- Starts from a foundation LLM (e.g., GPT-4) and adapts it with your labeled data
- Decision quality comes from learned weights + strict JSON schema + runtime context
- Still stateless at runtime; deterministic fallback covers failures/timeouts

### **Why this approach**
- **Consistency**: Higher schema adherence and decision consistency
- **Efficiency**: Potentially lower tokens and latency at scale
- **Control**: Encodes domain style and format directly in weights

### **Trade-offs**
- **Data/MLOps**: Requires curated dataset, training, evaluation, versioning
- **Drift**: Periodic re-training when rules or distributions change
- **SLA**: Still subject to provider limits; keep deterministic fallback

### **When to fine-tune**
- You have enough high-quality labeled routing examples
- Rules are relatively stable; require strong schema compliance
- You want lower latency/cost at volume

### **Operational guidance**
- **Timeouts/SLA**: Set aggressive timeouts and fallback threshold (e.g., 800–1200 ms)
- **Versioning**: Track fine-tune versions and rollout gradually (shadow/A-B)
- **Observability**: Log model latency, token usage, fallback rates, schema errors

### **Evaluation checklist**
- Authorization rate (primary), fee impact, error rates
- Decision time (P50/P95), fallback frequency, schema validity
- A/B or shadow tests before rollout

### **Security & privacy**
- Avoid PII; send only needed aggregates (e.g., BIN prefix, scheme, ranges)
- Redact merchant and cardholder identifiers; follow data residency policies
- Review provider data retention settings (ephemeral vs stored)

## 🧠 **A.11 OpenAI Embeddings: The Magic Behind Semantic Search**

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

---

## 🎯 **Summary**

This document has covered the key AI/ML concepts used in the PSP Router:

1. **🧠 LLM**: The AI brain that makes intelligent routing decisions
2. **🧠 LLM's Role**: Detailed explanation of how LLM serves as the intelligent decision engine

These concepts work together to create an intelligent payment routing system that learns from experience and makes increasingly better decisions over time! 🚀

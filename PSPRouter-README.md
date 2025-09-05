# PSP Router — Decision System for Payment Transactions

## 🎯 Purpose

Modern e-commerce systems must decide **which Payment Service Provider (PSP)** — such as **Adyen, Stripe, Klarna, or PayPal** — should process each transaction.  
The right choice impacts:

- ✅ **Authorization rates** (maximize successful transactions)  
- 💸 **Processing fees** (reduce costs)  
- 🛡 **Risk & compliance** (3-D Secure, SCA mandates, sanctions)  
- ⚡ **Latency & reliability** (avoid outages, prefer healthy providers)  
- 🧑‍🤝‍🧑 **Merchant preferences** (custom rules, A/B tests, promo agreements)  

This system implements an **intelligent router** that evaluates multiple signals and selects the optimal PSP for every payment attempt. It is designed to **learn from outcomes** over time and improve automatically.

---

## 🏗 Solution Overview

The PSP Router combines **deterministic guardrails**, a **decision engine powered by LLMs**, and a **learning loop**:

### 1. Inputs (per transaction)

- **Transaction context**: merchant, buyer country, currency, amount, method (Card, PayPal, Klarna), scheme (Visa, Mastercard, etc.), BIN prefix.  
- **PSP capabilities**: which payment methods/countries/currencies each PSP supports.  
- **Health signals**: status (green/yellow/red), latency, error spikes.  
- **Performance metrics**: rolling authorization/success rates per segment.  
- **Fee models**: % (bps) and fixed fees.  
- **Merchant preferences**: allow/deny lists, “prefer low fees,” etc.

### 2. Guardrails (code, not AI)

Certain rules are enforced **deterministically**, before any model is consulted:

- ❌ PSP that doesn’t support the required method/currency → excluded  
- ❌ PSP in `red` health → excluded  
- ❌ If SCA required but PSP lacks 3-DS → excluded  
- ❌ Merchant blocklist → excluded  

This ensures **safety and compliance**.

### 3. Decision Engine

- Uses **OpenAI (or Azure OpenAI)** via a C# client to evaluate valid candidates.  
- The LLM sees a **compact context JSON** (PSP auth rates, fees, health, prefs).  
- It proposes **one PSP candidate**, with alternates and reasoning.  
- If the LLM fails or is unavailable, a **deterministic scoring fallback** selects the best PSP.

### 4. Output

A structured JSON decision:

```json
{
  "schema_version": "1.0",
  "decision_id": "uuid",
  "candidate": "Adyen",
  "alternates": ["Stripe", "PayPal"],
  "reasoning": "Adyen has higher auth rate and lower fee in USD.",
  "constraints": {
    "must_use_3ds": true,
    "retry_window_ms": 8000,
    "max_retries": 1
  },
  "features_used": ["auth=0.89", "fee_bps=240", "health=green"],
  "guardrail": "none"
}
```

This ensures **machine-readability** and **auditability**.

### 5. Learning Loop

- Logs every decision with outcome (authorized / declined / fee paid).  
- Computes a **reward function**:  
  `reward = success ? +1 : -1 − fee − risk_penalty`.  
- Maintains **segment stats** (e.g., Visa|USD|IL → Adyen success 89%).  
- Optional **bandit algorithm** (ε-greedy, Thompson sampling) for exploration.  
- Stores **“lessons learned”** in a vector database (pgvector), retrievable into prompts.

### 6. Extensibility

- ✅ Add more PSPs (Checkout.com, Worldpay, etc.) by extending capability/fee providers.  
- ✅ Add new signals (fraud scores, device fingerprint, FX cost).  
- ✅ Add human approval for high-value transactions.  
- ✅ Periodically fine-tune a smaller router model for low-latency inference.

---

## ⚙ Components in the C# Project

- `Program.cs` → Demo entry point with dummy providers  
- `Router.cs` → Decision engine (LLM + fallback)  
- `DTOs.cs` → Contracts: `RouteInput`, `PspSnapshot`, `RouteContext`, `RouteDecision`  
- `Interfaces.cs` → Abstractions for health, fees, chat, and tools  
- `Dummies.cs` → Dummy implementations for local testing  
- `PspRouter.csproj` → .NET 8.0 project definition  

---

## 🚀 How to Run

```bash
cd src/PspRouter
dotnet run
```

This will execute a sample transaction and print a **decision JSON**.  
Later, replace `DummyHealthProvider`, `DummyFeeProvider`, and `DummyChatClient` with your real integrations.

---

## 📈 Benefits

- **Higher success rates** by dynamically preferring the best PSP.  
- **Lower costs** by considering real fees & FX impact.  
- **Resiliency** via health checks and alternates.  
- **Auditability** with structured JSON logs.  
- **Continuous learning** from transaction outcomes.  

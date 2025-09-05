# PSP Router — Decision System for Payment Transactions

## 🎯 Purpose
Decide the optimal PSP (Adyen / Stripe / Klarna / PayPal) per transaction to maximize auth success, minimize fees, and maintain compliance & reliability.

## 🏗 Solution Overview
- Deterministic **guardrails** (capabilities, SCA/3DS, health).
- **LLM decision engine** with tool calling.
- **Bandit learning** (ε-greedy / Thompson).
- **pgvector memory** for “lessons learned.”
- Deterministic **fallback** scoring when LLM is unavailable.

## 📦 Project Layout
- `Program.cs` – minimal runnable example wiring **OpenAIChatClient**, tools, **PgVectorMemory**, and **OpenAIEmbeddings**.
- `Router.cs` – decision engine (LLM first, fallback on parse/errors).
- `DTOs.cs` – contracts.
- `Interfaces.cs` – abstractions for clients/providers/tools.
- `Tools.cs` – `get_psp_health`, `get_fee_quote` tool wrappers.
- `Bandit.cs` – `IBandit`, `EpsilonGreedyBandit`, `ThompsonSamplingBandit`.
- `MemoryPgVector.cs` – `PgVectorMemory` (ensure schema, add/search).
- `OpenAIChatClient.cs` – chat wrapper with `response_format=json_object` and tool-calling loop.
- `EmbeddingsHelper.cs` – `OpenAIEmbeddings` helper (HTTP) for embeddings.
- `CapabilityMatrix.cs` – method→PSP gating.
- `Dummies.cs` – dummy providers for local testing.
- `PspRouter.csproj` – .NET 8, refs: `Npgsql`, `Pgvector`.

## 🧩 Environment
Set these before running:
```bash
export OPENAI_API_KEY=sk-...
export PGVECTOR_CONNSTR="Host=localhost;Username=postgres;Password=postgres;Database=psp_router"
```
> Ensure Postgres has the `vector` extension and that the table can be created: `CREATE EXTENSION IF NOT EXISTS vector;`

## 🚀 Run
```bash
cd src/PspRouter
dotnet run
```
You’ll see:
1) A JSON **decision** (LLM if available; else deterministic).  
2) A **pgvector** demo where a “lesson” is embedded and stored, then a top-3 semantic search prints results.

## 🧠 Learning Loop (where to plug rewards)
- After you receive gateway outcomes, compute a reward (e.g., `+1` for auth success minus fee/fx/risk penalties).
- Update a bandit (`IBandit.Update(segmentKey, psp, reward)`).
- Periodically summarize and `AddAsync` a short lesson via `OpenAIEmbeddings` + `PgVectorMemory`.

## 🔒 Safety & Compliance
- Do NOT send PAN/PII to the LLM (only BIN/scheme/aggregates).
- Enforce SCA/3DS in code.
- Keep an audit trail: features → decision → outcome.

## 🛠 Replace Dummies
- Implement `IHealthProvider` and `IFeeQuoteProvider` with your metrics/config.
- Swap in your own stats for `AuthRate30d` and pass them in `RouteContext`.

## 🔧 Tuning
- Adjust embedding model & `vector(N)` dimension in `MemoryPgVector.cs` to match your chosen model.
- Configure model name in `OpenAIChatClient` (default: `gpt-4.1`).

---

Happy routing!

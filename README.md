# 📚 Readify — AI-Powered Book Store

ASP.NET Core MVC e-commerce application extended with a production-grade AI service layer using Azure OpenAI and Microsoft Semantic Kernel.

🌐 **Live Demo:** [readify-eph9gsh4exanaafg.canadacentral-01.azurewebsites.net](https://readify-eph9gsh4exanaafg.canadacentral-01.azurewebsites.net)

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Azure OpenAI](https://img.shields.io/badge/Azure%20OpenAI-GPT--4o-0078D4?logo=microsoft-azure)
![Semantic Kernel](https://img.shields.io/badge/Semantic%20Kernel-1.75.0-5C2D91)
![Azure AI Search](https://img.shields.io/badge/Azure%20AI%20Search-Vector%20%2B%20Hybrid-0078D4?logo=microsoft-azure)
![ASP.NET Core Identity](https://img.shields.io/badge/Identity-Role%20Based%20Auth-green)
![Stripe](https://img.shields.io/badge/Payments-Stripe-635BFF?logo=stripe)

---

## What This Project Is

Readify started as a full-featured MVC book store (CRUD, Identity auth, Stripe payments, role-based access). The AI extension — built on top without touching the existing N-tier structure — adds two production AI features: a **tone-aware product description generator** (Week 1) and a **RAG hybrid semantic search engine** (Week 2) that understands user intent rather than just keywords.

The goal isn't just the features. It's the architecture that makes each layer testable, swappable, and defensible in interviews.

---

## Architecture — The Core Idea

Every AI call follows one non-negotiable flow:

```
Controller → IProductAIService → BookAIService → IAIService → AzureOpenAIService → Azure OpenAI
```

The controller never knows Azure OpenAI exists. `BookAIService` never knows what HTTP looks like. Each layer has exactly one job.

```
ProjectCore/
├── Controllers/
│   ├── ProductController.cs        ← existing, unchanged
│   └── AIController.cs             ← thin: validate → call service → return result
│
├── Services/AI/
│   ├── IAIService.cs               ← low-level contract: text in → text out
│   ├── IProductAIService.cs        ← domain contract: product data in → typed result out
│   ├── BookAIService.cs            ← knows books & tones, delegates AI calls to IAIService
│   ├── AzureOpenAIService.cs       ← knows Azure & Semantic Kernel, nothing else
│   ├── IEmbeddingService.cs        ← embedding contract: product → float vector
│   ├── AzureEmbeddingService.cs    ← uses IEmbeddingGenerator<string, Embedding<float>>
│   ├── ISearchService.cs           ← search contract: query → typed results
│   ├── ProductSearchService.cs     ← hybrid search, confidence logic, query expansion
│   ├── IAzureSearchIndexService.cs ← index management contract
│   ├── AzureSearchIndexService.cs  ← HNSW index auto-creation, batch upload
│   ├── IRagEvaluationService.cs    ← evaluation contract: faithfulness scoring
│   └── RagEvaluationService.cs     ← LLM-as-judge: 1–5 faithfulness score per retrieval
│
├── Models/AI/
│   ├── AIResponse.cs               ← generic result wrapper: AIResponse<T>
│   ├── AISettings.cs               ← strongly-typed configuration via IOptions<T>
│   ├── ProductDescriptionRequest.cs
│   └── ProductDescriptionResult.cs
│
├── ViewModels/
│   └── ProductAIViewModel.cs
│
├── Views/Product/
│   └── _AIDescriptionPartial.cshtml ← admin UI: tone selector + generate button
│
└── Configuration/
    └── AIServiceExtensions.cs      ← all DI wiring in one extension method
```

> `Bulky.DataAccess`, `Bulky.Models`, and `Bulky.Utility` are untouched. The AI layer lives entirely inside the web project.

---

## Features

**Core store (pre-existing)**

- Product and category management (admin)
- Shopping cart and order management
- Role-based access with ASP.NET Core Identity (Admin, Employee, Customer, Company)
- Stripe payment integration with webhook support
- Entity Framework Core with SQL Server

**AI layer — Week 1**

- Tone-aware product description generator (Professional / Casual / Playful / Academic)
- In-memory response caching — identical requests skip the API entirely
- Graceful failure — UI shows an error message, app never crashes
- All AI activity logged with product name and token usage
- Admin-only via `[Authorize(Roles = "Admin")]`

**AI layer — Week 2**

- Hybrid semantic search — understands intent, not just keywords ("cozy weekend read" finds mystery novels)
- Dual vector store: Azure SQL Vector (in-process cosine) + Azure AI Search (ANN/HNSW), both paths built and benchmarked
- Composite confidence logic — three conditions, not a single threshold (see below)
- Opt-in query expansion via GPT — expands query into genre/mood/theme language before re-embedding
- "Search Harder" UX — surfaces automatically when confidence is low
- LLM-as-judge faithfulness scoring (1–5) logged after every retrieval
- Resilience retry with keyword fallback — search never throws to the user
- Rate limiting on search endpoints — fixed window, protects embedding API costs
- Input validation — 3–200 character range enforced before any API call
- Auto-creates Azure AI Search index with HNSW config if missing — no manual portal setup

---

## Key Design Decisions

### Two interfaces, two jobs

`IAIService` handles raw text generation — it knows about prompts, tokens, and Semantic Kernel. `IProductAIService` handles domain logic — it knows about books, tones, and what a good description looks like. A controller test can mock `IProductAIService` without any real HTTP calls.

### `AIResponse<T>` — typed result wrapper

Every AI operation returns `AIResponse<T>` instead of throwing exceptions at service boundaries. The envelope carries `Success`, `ErrorMessage`, `FromCache`, and `TokensUsed`. The payload (`T`) carries only what the feature needs.

```csharp
// Controller reads result, never catches exceptions from services
var result = await _productAI.GenerateDescriptionAsync(request, ct);
if (!result.Success)
    return StatusCode(503, new { error = result.ErrorMessage });
```

### Composite confidence logic

A single cosine threshold misses the case where the top two results score nearly the same — the ranking is essentially random. Three conditions are checked:

```csharp
private const float LowConfidenceThreshold = 0.4f;

bool lowConfidence =
    topScore < LowConfidenceThreshold ||          // best match too weak
    (topScore < 0.50f && scoreGap < 0.10f) ||    // mediocre + indistinct
    (topScore < 0.60f && scoreGap < 0.05f);      // decent + coin-flip
```

### Hybrid search merge order

Keyword search runs first (safe — never throws). Semantic runs second. If semantic fails, keyword results are returned with `LowConfidence = true`. If both succeed, semantic results appear first; keyword fills remaining slots up to `topK`:

```csharp
semantic.Items.Union(keywordResult, ProductIdComparer.Instance).Take(topK)
```

Keyword uses word-splitting (`ExtractWordsFromPhrase`), so "cozy mystery" matches products containing "cozy" OR "mystery" — meaningfully smarter than a single phrase match.

### Azure SQL Vector vs Azure AI Search

Both paths are built and exposed via a `CompareSearch` admin endpoint. SQL Vector keeps the whole stack inside SQL Server — same EF Core provider, same migrations, no second service. In-process cosine is fine up to roughly 5,000 products (~120ms on test queries). Azure AI Search uses ANN/HNSW indexing — ~40ms on the same queries and scales horizontally without application code changes.

### Secrets never in source

`appsettings.json` holds only structure — no values. Locally, secrets go in .NET User Secrets. In production, they come from Azure Key Vault injected into `IConfiguration` at startup. Application code always reads from `IConfiguration` and never changes between environments.

### One extension method in `Program.cs`

```csharp
builder.Services.AddAIServices(builder.Configuration);
```

All Semantic Kernel wiring, `IOptions<T>` binding, and service registration lives in `AIServiceExtensions.cs`. `Program.cs` stays clean.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core MVC / .NET 8 |
| AI Orchestration | Microsoft Semantic Kernel 1.75.0 |
| AI Provider | Azure OpenAI (GPT-4o-mini) |
| Embeddings | Azure OpenAI text-embedding-3-small (1,536 dims) |
| Vector Store (SQL) | Azure SQL Vector — in-process cosine similarity |
| Vector Store (Search) | Azure AI Search — HNSW ANN indexing |
| Azure SDK | Azure.AI.OpenAI 2.9.0-beta.1 |
| Embedding API | Microsoft.Extensions.AI — IEmbeddingGenerator |
| ORM | Entity Framework Core |
| Auth | ASP.NET Core Identity |
| Payments | Stripe |
| Caching | Microsoft.Extensions.Caching.Memory |
| Resilience | Microsoft.Extensions.Http.Resilience |
| Rate Limiting | ASP.NET Core RateLimiter (fixed window) |
| Secrets (prod) | Azure Key Vault via Azure.Extensions.AspNetCore.Configuration.Secrets |

---

## Key Numbers

| Metric | Value |
|---|---|
| Embedding dimensions | 1,536 (text-embedding-3-small) |
| Low confidence floor | cosine < 0.4 |
| Low confidence — mediocre + indistinct | topScore < 0.50 AND gap < 0.10 |
| Low confidence — decent + coin-flip | topScore < 0.60 AND gap < 0.05 |
| Strong match threshold | > 0.85 |
| Faithfulness score range | 1–5 (sentinel: –1 for unparseable) |
| Azure AI Search latency (test queries) | ~40ms |
| SQL Vector in-process latency | ~120ms |
| Cost per search query (embedding only) | ~$0.00002 |
| Cost per faithfulness evaluation | ~$0.000015 |
| Search input valid range | 3–200 characters |
| Rate limit — public search | 20 requests / minute |
| Rate limit — CompareSearch (admin) | 5 requests / minute |

---

## Local Setup

### Prerequisites

- .NET 8 SDK
- SQL Server (LocalDB is fine)
- An Azure OpenAI resource with a `gpt-4o-mini` and `text-embedding-3-small` deployment
- Azure AI Search resource (for the Azure Search path)
- Visual Studio 2022 or VS Code

### Steps

**1. Clone the repo**

```bash
git clone https://github.com/sharad99kr/Bulky_MVC.git
cd Bulky_MVC
```

**2. Set up the database**

```bash
# From the Package Manager Console in Visual Studio
Update-Database
```

**3. Configure secrets (never edit appsettings.json for keys)**

```bash
cd ProjectCore
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:Endpoint"   "https://YOUR-RESOURCE.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey"     "your-api-key-here"
dotnet user-secrets set "AzureSearch:Endpoint"   "https://YOUR-SEARCH.search.windows.net"
dotnet user-secrets set "AzureSearch:ApiKey"     "your-search-api-key"
dotnet user-secrets set "Stripe:SecretKey"       "sk_test_..."
dotnet user-secrets set "Stripe:PublishableKey"  "pk_test_..."
```

**4. Run**

```bash
dotnet run --project ProjectCore
```

**5. Seed embeddings (one-time, admin only)**

After running, navigate to the admin area and call:

- `POST /AI/SeedEmbeddings` — generates and stores SQL Vector embeddings for all products
- `POST /AI/SeedAzureSearch` — auto-creates the HNSW index and uploads all product vectors

The AI description generator is available under any product's edit page. Semantic search is available at `/AI/Search`.

---

## AI Layer NuGet Packages

```powershell
# Run in ProjectCore
Install-Package Azure.AI.OpenAI
Install-Package Microsoft.SemanticKernel
Install-Package Microsoft.SemanticKernel.Connectors.OpenAI
Install-Package Microsoft.Extensions.AI
Install-Package Microsoft.Extensions.Caching.Memory
Install-Package Microsoft.Extensions.Http.Resilience
Install-Package Azure.Search.Documents
Install-Package Azure.Extensions.AspNetCore.Configuration.Secrets
```

> `Azure.AI.OpenAI 2.9.0-beta.1` is a beta package — this is by design. Semantic Kernel consistently depends on the latest beta of this SDK across versions. It is production-safe.

---

## Roadmap

- [x] Week 1 — AI service layer + tone-aware description generator
- [x] Week 2 — RAG hybrid semantic search (SQL Vector + Azure AI Search)
- [ ] Week 3 — TBD

---

## Project Structure (Multi-Project Solution)

```
Bulky.sln
├── Bulky.DataAccess     ← EF Core DbContext, repositories, migrations
├── Bulky.Models         ← Domain entities (Product, Category, Order, etc.)
├── Bulky.Utility        ← Constants, email service, Stripe helpers
└── ProjectCore          ← MVC web project (controllers, views, AI layer)
```

---

## License

MIT

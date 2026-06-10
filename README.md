# Readify — AI-Powered Book Store

ASP.NET Core MVC e-commerce application extended with a production-grade AI service layer using Azure OpenAI and Microsoft Semantic Kernel.

**Live Demo:** https://readify-eph9gsh4exanaafg.canadacentral-01.azurewebsites.net

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Azure OpenAI](https://img.shields.io/badge/Azure%20OpenAI-GPT--4o-0078D4?logo=microsoft-azure)
![Semantic Kernel](https://img.shields.io/badge/Semantic%20Kernel-1.75.0-5C2D91)
![Azure AI Search](https://img.shields.io/badge/Azure%20AI%20Search-Vector%20%2B%20Hybrid-0078D4?logo=microsoft-azure)
![MediatR](https://img.shields.io/badge/MediatR-CQRS-512BD4)
![xUnit](https://img.shields.io/badge/Tests-xUnit%20%2B%20Moq-green)
![ASP.NET Core Identity](https://img.shields.io/badge/Identity-Role%20Based%20Auth-green)
![Stripe](https://img.shields.io/badge/Payments-Stripe-635BFF?logo=stripe)

---

## What This Project Is

Readify started as a full-featured MVC book store (CRUD, Identity auth, Stripe payments, role-based access). The AI extension — built on top without touching the existing N-tier structure — adds production AI features week by week: a tone-aware product description generator (Week 1), a RAG hybrid semantic search engine (Week 2) that understands user intent rather than just keywords, and SK plugins with MediatR/CQRS dispatch and a full unit test suite (Week 3).

The goal is not just the features. It is the architecture that makes each layer testable, swappable, and defensible in interviews.

---

## Architecture — The Core Idea

Every AI call follows one non-negotiable flow:

```
Controller -> IMediator.Send() -> Handler -> IProductAIService -> BookAIService -> IAIService -> AzureOpenAIService -> Azure OpenAI
```

The controller never knows Azure OpenAI exists. `BookAIService` never knows what HTTP looks like. Each layer has exactly one job. After Week 3, controllers have a single dependency: `IMediator`.

Plugin calls flow through a separate path:

```
ChatController -> IMediator.Send() -> Handler -> IKernelPluginFactory -> Kernel (cloned per request)
  -> OrderPlugin.[KernelFunction]   -> IUnitOfWork -> DB
  -> ProductPlugin.[KernelFunction] -> ISearchService
  -> AIFunctionInvocationFilter     (wraps every call — validates inputs + structured logging)
```

```
ProjectCore/
├── Controllers/
│   ├── ProductController.cs          <- existing, unchanged
│   └── AIController.cs               <- thin: validate -> IMediator.Send() -> return result
│
├── Plugins/
│   ├── OrderPlugin.cs                <- [KernelFunction] wrapping OrderRepository
│   └── ProductPlugin.cs              <- [KernelFunction] wrapping ISearchService
│
├── Filters/
│   └── AIFunctionInvocationFilter.cs <- input validation + structured logging on every plugin call
│
├── CQRS/
│   ├── Commands/
│   │   ├── GenerateDescriptionCommand.cs
│   │   ├── SeedEmbeddingsCommand.cs
│   │   └── TriggerInventoryCheckCommand.cs  <- placeholder for Week 5 agent
│   ├── Queries/
│   │   ├── SearchProductsQuery.cs
│   │   └── GetInventoryStatusQuery.cs
│   └── Handlers/
│       ├── GenerateDescriptionCommandHandler.cs
│       ├── SeedEmbeddingsCommandHandler.cs
│       ├── SearchProductsQueryHandler.cs
│       └── GetInventoryStatusQueryHandler.cs
│
├── Services/AI/
│   ├── IAIService.cs                 <- low-level contract: text in -> text out
│   ├── IProductAIService.cs          <- domain contract: product data in -> typed result out
│   ├── BookAIService.cs              <- knows books and tones, delegates to IAIService
│   ├── AzureOpenAIService.cs         <- knows Azure and Semantic Kernel, nothing else
│   ├── IEmbeddingService.cs          <- embedding contract: product -> float vector
│   ├── AzureEmbeddingService.cs      <- uses IEmbeddingGenerator<string, Embedding<float>>
│   ├── ISearchService.cs             <- search contract: query -> typed results
│   ├── ProductSearchService.cs       <- hybrid search, confidence logic, query expansion
│   ├── IAzureSearchIndexService.cs   <- index management contract
│   ├── AzureSearchIndexService.cs    <- HNSW index auto-creation, batch upload
│   ├── IKernelPluginFactory.cs       <- factory contract: clones Kernel with plugins per request
│   ├── KernelPluginFactory.cs        <- Scoped factory: attaches OrderPlugin + ProductPlugin
│   ├── IRagEvaluationService.cs      <- evaluation contract: faithfulness scoring
│   └── RagEvaluationService.cs       <- LLM-as-judge: 1-5 faithfulness score per retrieval
│
├── Models/AI/
│   ├── AIResponse.cs                 <- generic result wrapper: AIResponse<T>
│   ├── AISettings.cs                 <- strongly-typed config via IOptions<T>
│   ├── ProductDescriptionRequest.cs
│   └── ProductDescriptionResult.cs
│
├── ViewModels/
│   └── ProductAIViewModel.cs
│
├── Views/Product/
│   └── _AIDescriptionPartial.cshtml  <- admin UI: tone selector + generate button
│
└── Configuration/
    └── AIServiceExtensions.cs        <- all DI wiring in one extension method

Bulky.Tests/                          <- xUnit project, separate from web project
└── Tests/
    ├── Handlers/
    │   ├── SearchProductsQueryHandlerTests.cs
    │   ├── GenerateDescriptionCommandHandlerTests.cs
    │   └── GetInventoryStatusQueryHandlerTests.cs
    └── Plugins/
        └── OrderPluginTests.cs
```

> `Bulky.DataAccess`, `Bulky.Models`, and `Bulky.Utility` are untouched. The AI layer lives entirely inside the web project.

---

## Features

### Core store (pre-existing)

- Product and category management (admin)
- Shopping cart and order management
- Role-based access with ASP.NET Core Identity (Admin, Employee, Customer, Company)
- Stripe payment integration with webhook support
- Entity Framework Core with SQL Server

### AI layer — Week 1

- Tone-aware product description generator (Professional / Casual / Playful / Academic)
- In-memory response caching — identical requests skip the API entirely
- Graceful failure — UI shows an error message, app never crashes
- All AI activity logged with product name and token usage
- Admin-only via `[Authorize(Roles = "Admin")]`

### AI layer — Week 2

- Hybrid semantic search — understands intent, not just keywords ("cozy weekend read" finds mystery novels)
- Dual vector store: Azure SQL Vector (in-process cosine) + Azure AI Search (ANN/HNSW), both paths built and benchmarked
- Composite confidence logic — three conditions, not a single threshold (see below)
- Opt-in query expansion via GPT — expands query into genre/mood/theme language before embedding
- "Search Harder" UX — surfaces automatically when confidence is low
- LLM-as-judge faithfulness scoring (1-5) logged after every retrieval
- Resilience retry with keyword fallback — search never throws to the user
- Rate limiting on search endpoints — fixed window, protects embedding API costs
- Input validation — 3-200 character range enforced before any API call
- Auto-creates Azure AI Search index with HNSW config if missing — no manual portal setup

### AI layer — Week 3

- SK OrderPlugin and ProductPlugin — LLM decides at runtime which tool to call based on user intent; order lookups only fire when the user asks about their order, keeping token costs minimal and data always live from the real database
- FunctionInvocationFilter — every plugin call is validated for prompt injection and SQL injection patterns before any DB access; suspicious input is aborted and logged at warning level, the database is never touched
- MediatR/CQRS — controllers dispatch exclusively via `IMediator.Send()`; no controller holds a direct service dependency; changing the entire search implementation requires zero controller changes
- Commands defined: `GenerateDescriptionCommand`, `SeedEmbeddingsCommand`, `TriggerInventoryCheckCommand` (placeholder for Week 5 agent)
- Queries defined: `SearchProductsQuery`, `GetInventoryStatusQuery`
- `IKernelPluginFactory` — Scoped factory clones the Singleton Kernel per request and attaches fresh plugins backed by the request's `IUnitOfWork` and `ISearchService`; Kernel lifetime is never violated
- `VolatileMemoryStore` — in-process session memory registered in DI, ready for the Week 4 chatbot
- Bulky.Tests xUnit project — 10+ unit tests covering all handlers and plugins; all external dependencies mocked via Moq; no real Azure credentials required in CI
- GitHub Actions CI updated — `dotnet test` runs on every push; a failing test blocks deployment

---

## Key Design Decisions

### Two interfaces, two jobs

`IAIService` handles raw text generation — it knows about prompts, tokens, and Semantic Kernel. `IProductAIService` handles domain logic — it knows about books, tones, and what a good description looks like. A controller test can mock `IProductAIService` without any real HTTP calls.

### `AIResponse<T>` — typed result wrapper

Every AI operation returns `AIResponse<T>` instead of throwing exceptions at service boundaries. The envelope carries `Success`, `ErrorMessage`, `FromCache`, and `TokensUsed`. The payload (`T`) carries only what the feature needs.

```csharp
var result = await _productAI.GenerateDescriptionAsync(request, ct);
if (!result.Success)
    return StatusCode(503, new { error = result.ErrorMessage });
```

### Composite confidence logic

A single cosine threshold misses the case where the top two results score nearly the same — the ranking is essentially random. Three conditions are checked:

```csharp
private const float LowConfidenceThreshold = 0.4f;

bool lowConfidence =
    topScore < LowConfidenceThreshold ||
    (topScore < 0.50f && scoreGap < 0.10f) ||
    (topScore < 0.60f && scoreGap < 0.05f);
```

### Hybrid search merge order

Keyword search runs first (safe — never throws). Semantic runs second. If semantic fails, keyword results are returned with `LowConfidence = true`. If both succeed, semantic results appear first; keyword fills remaining slots up to `topK`. Keyword uses word-splitting (`ExtractWordsFromPhrase`), so "cozy mystery" matches products containing "cozy" OR "mystery" — meaningfully smarter than a single phrase match.

### SK plugin pattern over direct service calls

A chatbot without plugins must embed all possible data in the prompt upfront — expensive, stale, and token-heavy. With plugins, SK decides at inference time which function to call. `OrderPlugin.GetOrderStatus` only fires when the user asks about their order, not on every message.

### MediatR as the controller boundary

Controllers have exactly one dependency after Week 3: `IMediator`. This means the entire AI implementation can be replaced without touching a single controller. It also makes unit testing trivial — handlers are tested directly with mocked service dependencies, with no need to spin up a controller or HTTP context.

### SK container bridge pattern

`kernelBuilder.AddAzureOpenAIChatCompletion()` registers `IChatCompletionService` inside the Kernel's own internal `IServiceProvider`, not the app container. Any Scoped service that needs it must have an explicit bridge:

```csharp
services.AddSingleton<IChatCompletionService>(sp =>
    sp.GetRequiredService<Kernel>()
      .GetRequiredService<IChatCompletionService>());
```

Bridges are Singleton — both interfaces are stateless HTTP clients.

### Azure SQL Vector vs Azure AI Search

Both paths are built and exposed via a `CompareSearch` admin endpoint. SQL Vector keeps the whole stack inside SQL Server — same EF Core provider, same migrations, no second service. In-process cosine is fine up to roughly 5,000 products (~120ms on test queries). Azure AI Search uses ANN/HNSW indexing — ~40ms on the same queries and scales horizontally without application code changes.

### Secrets never in source

`appsettings.json` holds only structure — no values. Locally, secrets go in .NET User Secrets. In production, they come from Azure Key Vault injected into `IConfiguration` at startup.

### One extension method in `Program.cs`

```csharp
builder.Services.AddAIServices(builder.Configuration);
```

All Semantic Kernel wiring, bridge registrations, `IOptions<T>` binding, MediatR, and service registration live in `AIServiceExtensions.cs`. `Program.cs` stays clean.

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
| CQRS Dispatch | MediatR 14.x |
| Unit Testing | xUnit + Moq |
| ORM | Entity Framework Core |
| Auth | ASP.NET Core Identity |
| Payments | Stripe |
| Caching | Microsoft.Extensions.Caching.Memory |
| Resilience | Microsoft.Extensions.Http.Resilience |
| Rate Limiting | ASP.NET Core RateLimiter (fixed window) |
| CI/CD | GitHub Actions (build + test + deploy) |
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
| Faithfulness score range | 1-5 (sentinel: -1 for unparseable) |
| Azure AI Search latency (test queries) | ~40ms |
| SQL Vector in-process latency | ~120ms |
| Cost per search query (embedding only) | ~$0.00002 |
| Cost per faithfulness evaluation | ~$0.000015 |
| Search input valid range | 3-200 characters |
| Rate limit — public search | 20 requests / minute |
| Rate limit — CompareSearch (admin) | 5 requests / minute |
| Plugin hard cap (orders returned) | 5 |
| xUnit tests (Week 3) | 10+ |

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
Install-Package MediatR

# Run in Bulky.Tests
Install-Package xunit
Install-Package Moq
Install-Package FluentAssertions
```

> `Azure.AI.OpenAI 2.9.0-beta.1` is a beta package — this is by design. Semantic Kernel consistently depends on the latest beta of this SDK across versions. It is production-safe.
>
> `MediatR.Extensions.Microsoft.DependencyInjection` is **not** needed for MediatR 14.x — do not install it.

---

## Roadmap

- [x] Week 1 — AI service layer + tone-aware description generator
- [x] Week 2 — RAG hybrid semantic search (SQL Vector + Azure AI Search)
- [x] Week 3 — SK plugins (OrderPlugin, ProductPlugin) + MediatR/CQRS + xUnit/Bulky.Tests + GitHub Actions CI
- [ ] Week 4 — Agentic support chatbot (RAG-grounded, real order lookup, guardrails)
- [ ] Week 5-6 — Multi-agent inventory system (Azure Service Bus, MassTransit, SignalR, MCP)
- [ ] Week 7 — Admin AI insights dashboard (Chart.js, OpenTelemetry, Azure App Insights)
- [ ] Week 8 — Portfolio polish (ADR files, DDD bounded contexts, Mermaid architecture diagram)

---

## Project Structure (Multi-Project Solution)

```
Bulky.sln
├── Bulky.DataAccess     <- EF Core DbContext, repositories, migrations
├── Bulky.Models         <- Domain entities (Product, Category, Order, etc.)
├── Bulky.Utility        <- Constants, email service, Stripe helpers
├── ProjectCore          <- MVC web project (controllers, views, AI layer)
└── Bulky.Tests          <- xUnit unit test project
```

---

## License

MIT

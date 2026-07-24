# Readify — AI-Powered Book Store

ASP.NET Core MVC e-commerce application extended with a production-grade AI service layer: a RAG product-search and support chatbot on Semantic Kernel, and a multi-agent inventory reconciliation system on Microsoft Agent Framework, wired together with MassTransit/RabbitMQ, SignalR, and an MCP server.

**Live Demo:** https://readify-eph9gsh4exanaafg.canadacentral-01.azurewebsites.net

![.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)
![Azure OpenAI](https://img.shields.io/badge/Azure%20OpenAI-GPT--4.1--mini-0078D4?logo=microsoft-azure)
![Semantic Kernel](https://img.shields.io/badge/Semantic%20Kernel-1.75.0-5C2D91)
![Agent Framework](https://img.shields.io/badge/Microsoft%20Agent%20Framework-1.10.0-5C2D91)
![Azure AI Search](https://img.shields.io/badge/Azure%20AI%20Search-Vector%20%2B%20Hybrid-0078D4?logo=microsoft-azure)
![MassTransit](https://img.shields.io/badge/MassTransit-RabbitMQ-FF6600?logo=rabbitmq)
![SignalR](https://img.shields.io/badge/SignalR-Realtime-512BD4)
![MCP](https://img.shields.io/badge/MCP-Server-000000)
![MediatR](https://img.shields.io/badge/MediatR-CQRS-512BD4)
![xUnit](https://img.shields.io/badge/Tests-xUnit%20%2B%20Moq-green)
![ASP.NET Core Identity](https://img.shields.io/badge/Identity-Role%20Based%20Auth-green)
![Stripe](https://img.shields.io/badge/Payments-Stripe-635BFF?logo=stripe)

---

## What This Project Is

Readify started as a full-featured MVC book store (CRUD, Identity auth, Stripe payments, role-based access). On top of that, without touching the existing N-tier structure, it grew a production AI layer in stages:

1. A tone-aware product description generator (admin tool).
2. A RAG hybrid semantic search engine that understands intent, not just keywords.
3. Semantic Kernel plugins dispatched through MediatR/CQRS, backed by a full unit test suite.
4. A RAG-grounded customer support chatbot, with real order lookups and persisted conversation history.
5. A multi-agent inventory reconciliation system: SQL stock is compared against a warehouse Excel export, discrepancies are published as events over RabbitMQ, pushed live to an admin dashboard via SignalR, narrated in plain English by a group of LLM agents, and escalated by email — all triggered either on-demand or hourly by a separate Azure Function.
6. A standalone MCP server that exposes live inventory data as callable tools for any external AI agent (including whatever's reading this file for you right now).

The goal is not just the features — it's an architecture where each layer is testable, swappable, and defensible in an interview: deterministic business logic stays deterministic, and the LLM is only ever added where it's adding communication or reasoning value on top of a decision that's already been made correctly without it.

---

## Architecture — The Core Ideas

### 1. Product description generation — a clean, boring service chain

```
Controller -> IMediator.Send() -> Handler -> IProductAIService -> BookAIService -> IAIService -> AzureOpenAIService -> Azure OpenAI
```

The controller never knows Azure OpenAI exists. `BookAIService` never knows what HTTP looks like. Each layer has exactly one job.

### 2. RAG chatbot — plugins, not a hardcoded prompt

```
ChatController -> IMediator.Send() -> SendMessageCommandHandler -> ChatService
  -> ChatKernelFactory              (clones the singleton Kernel per request)
  -> OrderPlugin.[KernelFunction]   -> IUnitOfWork -> DB
  -> ProductPlugin.[KernelFunction] -> ISearchService (same hybrid search used by /AI/Search)
  -> AIFunctionInvocationFilter     (wraps every call — validates inputs + structured logging)
  -> ChatMessageRepository          (persists both turns, keyed by ConversationId)
```

The LLM decides at inference time whether a user's message needs an order lookup or a product search — neither plugin fires on every message, which keeps token cost proportional to what's actually being asked.

### 3. Inventory reconciliation — deterministic decision, LLM narration

```
Admin "Run Check" button  ─┐
                            ├─> TriggerInventoryCheckCommand -> InventoryOrchestrationService
Azure Function (hourly)   ─┘
        │
        ├─ 1. IWarehouseReader reads warehouse_stock.xlsx from Azure Blob (tolerates missing blob)
        ├─ 2. IInventoryReader compares SQL StockQuantity vs warehouse quantity — DETERMINISTIC
        │        > 40% mismatch  -> StockDiscrepancyDetected (published via MassTransit)
        │        <= 5 units left -> LowStockDetected          (published via MassTransit)
        ├─ 3. Round-robin group chat (SqlAgent, ExcelAgent, ReconciliationAgent) narrates
        │        a plain-English briefing of the scan that already happened — no agent
        │        in this step can change what was already decided
        └─ 4. If any Urgent discrepancy exists: a sequential EmailAgent workflow calls
                 send_alert_email -> EmailAlertService -> SMTP (MailKit)

RabbitMQ (CloudAMQP)
        ├─ low-stock-queue        -> NotificationConsumer  -> SignalR -> /hubs/inventory-alerts
        ├─ discrepancy-queue      -> DiscrepancyConsumer    -> SignalR -> /hubs/inventory-alerts
        ├─ (exponential retry: 3 attempts, 1s -> 30s)
        ├─ discrepancy-fault-queue -> DeadLetterConsumer<StockDiscrepancyDetected>  (logs, never silent)
        └─ low-stock-fault-queue   -> DeadLetterConsumer<LowStockDetected>
```

If the LLM briefing step fails for any reason, the orchestrator falls back to a hardcoded `DeterministicReconciliationSummary` — a broken model call never blocks the actual alert. This is the one architectural rule worth repeating in an interview: **the LLM never owns the decision, only the explanation.**

### 4. Three processes, one shared core

```
Bulky.Models  <-  Bulky.Utility
      ^
      |
Bulky.DataAccess   (EF Core Repository/UnitOfWork  +  the inventory AI domain logic)
      ^                       ^                              ^
      |                       |                              |
ProjectCore          Bulky.AzureFunction              Bulky.McpServer
(the web app,        (timer-triggered,                (stdio MCP server,
 hosts every          publish-only —                   exposes inventory
 MassTransit          fires the same                   data as callable
 consumer, since       pipeline hourly)                 tools for external
 SignalR only                                            AI agents)
 means something
 with a connected
 browser)
```

`ProjectCore`, `Bulky.AzureFunction`, and `Bulky.McpServer` are three independently deployable processes that all share `Bulky.DataAccess` so the inventory pipeline's business logic exists exactly once.

---

## Project Structure

```
Bulky.sln
├── Bulky.Models             <- domain entities (Product, Category, Order, ChatMessage, ApplicationUser...)
├── Bulky.Utility             <- SD.cs constants, EmailSender stub, StripeSettings
│
├── Bulky.DataAccess
│   ├── Data/                 <- ApplicationDbContext, Migrations (13, incl. AddProductEmbeddings, AddChatMessages, AddProductStockQuantity)
│   ├── Repository/           <- generic Repository<T> + per-entity repos + UnitOfWork
│   ├── DbInitializer/        <- role/seed data on startup
│   └── AI/
│       ├── CQRS/             <- TriggerInventoryCheckCommand + Handler
│       └── Inventory/
│           ├── Services/     <- InventoryReader, InventoryOrchestrationService,
│           │                    InventoryAgentFactory, EmailAlertService, ExcelWarehouseReader
│           ├── Interfaces/   <- IInventoryReader, IInventoryOrchestrator, IEmailAlertService, IWarehouseReader...
│           ├── Messages/     <- LowStockDetected, StockDiscrepancyDetected (MassTransit contracts)
│           └── Models/       <- ReconciliationResult, InventoryStatusResult
│
├── ProjectCore                            <- the deployable web app
│   ├── Areas/
│   │   ├── Admin/            <- Category, Company, Inventory, Order, Product, User controllers
│   │   ├── Customer/         <- Cart, Home (default area)
│   │   └── Identity/         <- scaffolded ASP.NET Identity Razor Pages
│   ├── Controllers/
│   │   ├── AIController.cs   <- GenerateDescription, SeedEmbeddings, SeedAzureSearch, Search, CompareSearch
│   │   └── ChatController.cs <- Send, History (RAG chatbot)
│   ├── Plugins/               <- OrderPlugin, ProductPlugin, ChatKernelFactory
│   ├── Filters/                <- AIFunctionInvocationFilter
│   ├── Services/AI/            <- ChatService, BookAIService, AzureOpenAIService,
│   │                              ProductSearchService, AzureEmbeddingService,
│   │                              AzureSearchIndexService, RagEvaluationService
│   ├── CQRS/                   <- Commands/Queries/Handlers for description gen, search,
│   │                              embeddings, chat, inventory seed/stock
│   ├── Consumers/              <- NotificationConsumer, DiscrepancyConsumer, DeadLetterConsumer<T>
│   ├── Hubs/                   <- InventoryAlertHub (SignalR)
│   ├── Configuration/          <- AIServiceExtensions.cs — all AI DI wiring in one place
│   ├── Program.cs              <- MVC, Identity, MassTransit/RabbitMQ topology, SignalR, rate limiting
│   └── wwwroot/js/             <- chat-widget.js, inventory-alerts.js
│
├── Bulky.AzureFunction        <- isolated-worker Functions app (net8.0)
│   └── InventoryCheckFunction.cs   <- [TimerTrigger("%InventoryCheckSchedule%")], publish-only bus
│
├── Bulky.McpServer            <- standalone stdio MCP server (net9.0)
│   └── InventoryTools.cs      <- get_low_inventory_products, get_product_stock, get_stock_discrepancies
│
└── Bulky.Tests                <- xUnit + Moq + FluentAssertions + MassTransit.TestFramework
```

---

## Features

### Core store (pre-existing)

- Product and category management (admin)
- Shopping cart and order management
- Role-based access with ASP.NET Core Identity (Admin, Employee, Customer, Company)
- Stripe payment integration
- Entity Framework Core with SQL Server

### AI-generated product descriptions

- Tone-aware generator (Professional / Casual / Playful / Academic)
- In-memory response caching — identical requests skip the API entirely
- Graceful failure — UI shows an error message, app never crashes
- Admin-only via `[Authorize(Roles = "Admin")]`

### Hybrid semantic search

- Understands intent, not just keywords ("cozy weekend read" finds mystery novels)
- Embeddings stored two ways: inline in SQL (`Product.SearchEmbeddingData`, a `varbinary(max)` column) and in an Azure AI Search HNSW index
- Composite confidence logic — three conditions, not a single threshold (see below)
- Opt-in query expansion via GPT before embedding
- LLM-as-judge faithfulness scoring (1-5), fired asynchronously after every retrieval
- Resilience retry with keyword fallback — search never throws to the user
- Rate limiting on `search` and `chat` endpoints (fixed window) to protect API cost
- Input validation — 3-200 character range enforced before any API call
- `/AI/CompareSearch` (SQL vector vs. Azure AI Search side-by-side) exists but is currently short-circuited/disabled pending further work

### RAG support chatbot

- Floating chat widget, persists conversation ID and open/minimized state in `localStorage`
- `OrderPlugin` (`get_order_status`, `get_recent_orders` — hard-capped at 5) and `ProductPlugin` (`search_products`) as Semantic Kernel functions the model invokes only when relevant
- Full conversation history persisted per user (`ChatMessage` table, indexed on `ConversationId, CreatedAtUtc`)
- Every plugin call passes through `AIFunctionInvocationFilter`, which validates for prompt/SQL-injection patterns before any DB access

### Multi-agent inventory reconciliation

- Deterministic scan (`InventoryReader`): flags any product ≤ 5 units as low stock, and any SQL-vs-warehouse mismatch over 40% as an urgent discrepancy
- Five `ChatClientAgent`s (SQL, Excel, Reconciliation, Notification, Email) built by `InventoryAgentFactory`, orchestrated as a round-robin group chat (briefing) plus a sequential workflow (email)
- Email alerts sent over plain SMTP via MailKit — a separate mechanism from ASP.NET Identity's own `IEmailSender` stub, which is unrelated and unused
- Events published over MassTransit/RabbitMQ, consumed by SignalR-pushing consumers with exponential retry (3 attempts, 1s → 30s) and dedicated dead-letter consumers so failures are logged, not lost
- Admin dashboard at `/Admin/Inventory/Alerts` — manual "Run Check" and "Seed Stock" buttons, live-updating via SignalR
- Runs automatically once an hour via `Bulky.AzureFunction`'s timer trigger (schedule configurable through the `InventoryCheckSchedule` app setting, no redeploy needed to change cadence)
- A standalone MCP server (`Bulky.McpServer`) independently exposes the same inventory data (`get_low_inventory_products`, `get_product_stock`, `get_stock_discrepancies`) as callable tools for any MCP-speaking AI agent

---

## Key Design Decisions

### The LLM narrates, deterministic code decides

Every "decision" in the inventory pipeline — is this product low on stock, does this discrepancy cross the urgent threshold — is a plain percentage comparison in C#, computed before any agent is invoked. The group-chat agents only produce a human-readable summary of a scan that already happened; if that LLM step fails or times out, a hardcoded deterministic summary is used instead. The AI adds explainability, never correctness risk.

### Two AI orchestration frameworks, chosen per problem shape

The chatbot uses **Semantic Kernel** — a single agent, cloned per request, auto-invoking plugins in response to one user message at a time. The inventory system uses **Microsoft Agent Framework** (`Microsoft.Agents.AI` / `.Workflows`) — five named agents running a round-robin group chat plus a sequential workflow, because that's a multi-role, multi-step process, not a single conversational turn. Using Semantic Kernel for the inventory system would mean hand-rolling a workflow graph it isn't built for; using Agent Framework for the chatbot would mean far more ceremony than one plugin-calling agent needs.

### Two interfaces, two jobs (description generation)

`IAIService` handles raw text generation — prompts, tokens, Semantic Kernel. `IProductAIService` handles domain logic — books, tones, what a good description looks like. A controller test can mock `IProductAIService` without any real HTTP calls.

### `AIResponse<T>` — typed result wrapper

Every AI operation returns `AIResponse<T>` instead of throwing exceptions at service boundaries, carrying `Success`, `ErrorMessage`, `FromCache`, and `TokensUsed`.

```csharp
var result = await _productAI.GenerateDescriptionAsync(request, ct);
if (!result.Success)
    return StatusCode(503, new { error = result.ErrorMessage });
```

### Composite confidence logic

A single cosine threshold misses the case where the top two results score nearly the same — the ranking is essentially random:

```csharp
private const float LowConfidenceThreshold = 0.4f;

bool lowConfidence =
    topScore < LowConfidenceThreshold ||
    (topScore < 0.50f && scoreGap < 0.10f) ||
    (topScore < 0.60f && scoreGap < 0.05f);
```

### Hybrid search merge order

Keyword search runs first (safe — never throws). Semantic runs second. If semantic fails, keyword results are returned with `LowConfidence = true`. If both succeed, semantic results appear first; keyword fills remaining slots up to `topK`.

### MediatR as the controller boundary

Controllers dispatch exclusively via `IMediator.Send()`. The entire AI implementation can be replaced without touching a controller, and handlers are unit tested directly with mocked dependencies — no HTTP context required.

### MassTransit over RabbitMQ, not Azure Service Bus

Hosted on CloudAMQP's free tier — far cheaper to run than Service Bus for a project at this scale — with the transport abstracted behind MassTransit, so switching later is a configuration change (`UsingRabbitMq` → `UsingAzureServiceBus`), not a rewrite. Explicit dead-letter consumers (`DeadLetterConsumer<TEvent> : IConsumer<Fault<TEvent>>`) log every fault, rather than relying on MassTransit's default `_error` queue convention, which nobody would think to check without knowing to look.

### Scoped services behind a Singleton host, twice

Both `Bulky.AzureFunction` and `Bulky.McpServer` face the same problem: their host container is effectively Singleton-scoped, but `IUnitOfWork`/`ApplicationDbContext` are Scoped. The Function resolves this with `IServiceScopeFactory` (a fresh scope per timer fire); the MCP server resolves it with `IDbContextFactory<ApplicationDbContext>` (a fresh context per tool call). Same captive-dependency problem, two idiomatic fixes for two different hosting models.

### Secrets never in source

`appsettings.json` holds only structure — no values. Locally, secrets go in .NET User Secrets; in production, they're set directly as Azure App Service / Function App configuration.

### One extension method in `Program.cs`

```csharp
builder.Services.AddAIServices(builder.Configuration);
```

All Semantic Kernel wiring, `IOptions<T>` binding, and AI service registration live in `AIServiceExtensions.cs`. `Program.cs` stays legible.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Framework | ASP.NET Core MVC / .NET 8 (Bulky.McpServer targets .NET 9) |
| Conversational AI | Microsoft Semantic Kernel 1.75.0 |
| Multi-agent workflows | Microsoft Agent Framework — `Microsoft.Agents.AI` / `.Workflows` 1.10.0 |
| AI Provider | Azure OpenAI (`gpt-4.1-mini`) |
| Embeddings | Azure OpenAI `text-embedding-3-small` (1,536 dims) |
| Vector Store (SQL) | Inline `varbinary(max)` column, in-process cosine similarity |
| Vector Store (Search) | Azure AI Search — HNSW ANN indexing |
| Azure SDK | Azure.AI.OpenAI 2.9.0-beta.1 |
| Messaging | MassTransit 8.0.13 over RabbitMQ (CloudAMQP), in-memory transport for tests/CI |
| Real-time push | SignalR (`InventoryAlertHub`) |
| Scheduled trigger | Azure Functions, isolated worker, timer trigger |
| External tool access | MCP server (`ModelContextProtocol`, stdio transport) |
| Blob storage | Azure.Storage.Blobs — warehouse Excel export |
| Excel parsing | ClosedXML |
| Email (inventory alerts) | MailKit / MimeKit — plain SMTP |
| CQRS Dispatch | MediatR 14.1.0 |
| Unit Testing | xUnit + Moq + FluentAssertions + MassTransit.TestFramework |
| ORM | Entity Framework Core 8.0.18 |
| Auth | ASP.NET Core Identity + Facebook OAuth |
| Payments | Stripe |
| Caching | Microsoft.Extensions.Caching.Memory |
| Resilience | Microsoft.Extensions.Http.Resilience |
| Rate Limiting | ASP.NET Core RateLimiter (fixed window: search, CompareSearch, chat) |
| CI/CD | GitHub Actions — separate workflows for the web app and the Azure Function |

---

## Key Numbers

| Metric | Value |
|---|---|
| Embedding dimensions | 1,536 (text-embedding-3-small) |
| Low confidence floor | cosine < 0.4 |
| Low confidence — mediocre + indistinct | topScore < 0.50 AND gap < 0.10 |
| Low confidence — decent + coin-flip | topScore < 0.60 AND gap < 0.05 |
| Low-stock threshold | ≤ 5 units |
| Urgent discrepancy threshold | > 40% mismatch between SQL and warehouse quantity |
| Inventory check schedule | hourly, NCRONTAB via `InventoryCheckSchedule` app setting |
| Message retry policy | exponential, 3 attempts, 1s → 30s |
| Search input valid range | 3-200 characters |
| Rate limit — public search | 10 requests / 30s |
| Rate limit — chat | 20 requests / minute (queue limit 2) |
| Chat message length cap | 500 characters |
| Plugin hard cap (orders returned) | 5 |

---

## Local Setup

### Prerequisites

- .NET 8 SDK (and .NET 9 SDK if you want to run `Bulky.McpServer`)
- SQL Server (LocalDB is fine)
- An Azure OpenAI resource with a chat deployment and `text-embedding-3-small`
- An Azure AI Search resource
- An Azure Storage account with a blob container for `warehouse_stock.xlsx`
- A RabbitMQ instance (CloudAMQP's free tier works) — or leave `RabbitMQ:Host` empty to fall back to an in-memory bus for local dev
- An SMTP account (e.g. Gmail with an app password) for inventory email alerts
- Visual Studio 2022 or VS Code

### Steps

**1. Clone the repo**

```bash
git clone https://github.com/sharad99kr/Readify.git
cd Readify
```

**2. Set up the database**

```bash
# From the Package Manager Console in Visual Studio, targeting Bulky.DataAccess
Update-Database
```

**3. Configure secrets (never edit appsettings.json for real values)**

```bash
cd ProjectCore
dotnet user-secrets init
dotnet user-secrets set "AzureOpenAI:Endpoint"    "https://YOUR-RESOURCE.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey"      "your-api-key-here"
dotnet user-secrets set "AzureSearch:Endpoint"    "https://YOUR-SEARCH.search.windows.net"
dotnet user-secrets set "AzureSearch:ApiKey"      "your-search-api-key"
dotnet user-secrets set "Stripe:SecretKey"        "sk_test_..."
dotnet user-secrets set "Stripe:PublishableKey"   "pk_test_..."
dotnet user-secrets set "RabbitMQ:Host"           "your-host.cloudamqp.com"
dotnet user-secrets set "RabbitMQ:VHost"          "your-vhost"
dotnet user-secrets set "RabbitMQ:Username"       "your-username"
dotnet user-secrets set "RabbitMQ:Password"       "your-password"
dotnet user-secrets set "Email:SmtpHost"          "smtp.gmail.com"
dotnet user-secrets set "Email:FromAddress"       "you@example.com"
dotnet user-secrets set "Email:AdminAddress"      "admin@example.com"
dotnet user-secrets set "Email:AppPassword"       "your-app-password"
dotnet user-secrets set "ConnectionStrings:AzureBlobStorage" "your-blob-connection-string"
```

Also move the hardcoded Facebook OAuth `AppId`/`AppSecret` in `Program.cs` into User Secrets before running — they should not stay inline.

**4. Run the web app**

```bash
dotnet run --project ProjectCore
```

**5. Seed data (one-time, admin only)**

- `POST /AI/SeedEmbeddings` — generates and stores in-SQL embeddings for all products
- `POST /AI/SeedAzureSearch` — auto-creates the HNSW index and uploads all product vectors
- `POST /Admin/Inventory/SeedStock` — randomizes `StockQuantity` so a check has something to find

The AI description generator is on any product's edit page. Semantic search is at `/AI/Search`. The chat widget is available site-wide once logged in. The inventory dashboard is at `/Admin/Inventory/Alerts`.

**6. (Optional) Run the inventory check locally without the Function**

Click "Run Check" on the admin Inventory dashboard — it calls the exact same `TriggerInventoryCheckCommand` the Azure Function calls on its hourly schedule.

**7. (Optional) Run the MCP server**

```bash
dotnet run --project Bulky.McpServer
```

Point any MCP-compatible client at the built executable over stdio to query `get_low_inventory_products`, `get_product_stock`, and `get_stock_discrepancies` directly.

---

## Roadmap

- [x] AI service layer + tone-aware description generator
- [x] RAG hybrid semantic search (in-SQL vectors + Azure AI Search)
- [x] Semantic Kernel plugins (OrderPlugin, ProductPlugin) + MediatR/CQRS + xUnit
- [x] Agentic support chatbot (RAG-grounded, real order lookup, guardrails, persisted history)
- [x] Multi-agent inventory system — MassTransit/RabbitMQ, SignalR, dead-letter handling, Azure Function scheduler, MCP server
- [ ] Admin AI insights dashboard (charting, OpenTelemetry, Azure App Insights)
- [ ] Share the >40%-discrepancy calculation between `InventoryReader` and `Bulky.McpServer.InventoryTools` instead of duplicating it
- [ ] Gate the web app's deploy pipeline on `dotnet test` passing
- [ ] Portfolio polish — ADR files, Mermaid architecture diagram

---

## License

MIT

# 📚 Readify — AI-Powered Book Store

> ASP.NET Core MVC e-commerce application extended with a production-grade AI service layer using Azure OpenAI and Microsoft Semantic Kernel.
🌐 \*\*Live Demo:\*\* \[Readify](https://readify-eph9gsh4exanaafg.canadacentral-01.azurewebsites.net/)

[!\[.NET](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[!\[Azure OpenAI](https://img.shields.io/badge/Azure\_OpenAI-GPT--4o-0078D4?logo=microsoft-azure)](https://azure.microsoft.com/en-us/products/ai-services/openai-service)
[!\[Semantic Kernel](https://img.shields.io/badge/Semantic\_Kernel-1.75.0-5C2D91)](https://learn.microsoft.com/en-us/semantic-kernel/)
[!\[ASP.NET Core Identity](https://img.shields.io/badge/Identity-Role--Based\_Auth-green)](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity)
[!\[Stripe](https://img.shields.io/badge/Payments-Stripe-635BFF?logo=stripe)](https://stripe.com/)

\---

## What This Project Is

Readify started as a full-featured MVC book store (CRUD, Identity auth, Stripe payments, role-based access). The AI extension — built on top without touching the existing N-tier structure — adds a **tone-aware product description generator** that admins can use directly from the product edit page.

The goal isn't just the feature. It's the architecture that makes the feature testable, swappable, and defensible in interviews.

\---

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
│   ├── BookAIService.cs            ← knows books \& tones, delegates AI calls to IAIService
│   └── AzureOpenAIService.cs       ← knows Azure \& Semantic Kernel, nothing else
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
│   └── \_AIDescriptionPartial.cshtml ← admin UI: tone selector + generate button
│
└── Configuration/
    └── AIServiceExtensions.cs      ← all DI wiring in one extension method
```

> `Bulky.DataAccess`, `Bulky.Models`, and `Bulky.Utility` are untouched. The AI layer lives entirely inside the web project.

\---

## Features

**Core store (pre-existing)**

* Product and category management (admin)
* Shopping cart and order management
* Role-based access with ASP.NET Core Identity (Admin, Employee, Customer, Company)
* Stripe payment integration with webhook support
* Entity Framework Core with SQL Server

**AI layer (Week 1)**

* Tone-aware product description generator (Professional / Casual / Playful / Academic)
* In-memory response caching — identical requests skip the API entirely
* Graceful failure — UI shows an error message, app never crashes
* All AI activity logged with product name and token usage
* Admin-only via `\[Authorize(Roles = "Admin")]`

\---

## Key Design Decisions

### Two interfaces, two jobs

`IAIService` handles raw text generation — it knows about prompts, tokens, and Semantic Kernel. `IProductAIService` handles domain logic — it knows about books, tones, and what a good description looks like. A controller test can mock `IProductAIService` without any real HTTP calls.

### `AIResponse<T>` — typed result wrapper

Every AI operation returns `AIResponse<T>` instead of throwing exceptions at service boundaries. The envelope carries `Success`, `ErrorMessage`, `FromCache`, and `TokensUsed`. The payload (`T`) carries only what the feature needs.

```csharp
// Controller reads result, never catches exceptions from services
var result = await \_productAI.GenerateDescriptionAsync(request, ct);
if (!result.Success)
    return StatusCode(503, new { error = result.ErrorMessage });
```

### Secrets never in source

`appsettings.json` holds only structure — no values. Locally, secrets go in .NET User Secrets. In production, they come from Azure Key Vault injected into `IConfiguration` at startup. Application code always reads from `IConfiguration` and never changes between environments.

### One extension method in `Program.cs`

```csharp
builder.Services.AddAIServices(builder.Configuration);
```

All Semantic Kernel wiring, `IOptions<T>` binding, and service registration lives in `AIServiceExtensions.cs`. `Program.cs` stays clean.

\---

## Tech Stack

|Layer|Technology|
|-|-|
|Framework|ASP.NET Core MVC / .NET 8|
|AI Orchestration|Microsoft Semantic Kernel 1.75.0|
|AI Provider|Azure OpenAI (GPT-4o)|
|Azure SDK|Azure.AI.OpenAI 2.9.0-beta.1|
|ORM|Entity Framework Core|
|Auth|ASP.NET Core Identity|
|Payments|Stripe|
|Caching|Microsoft.Extensions.Caching.Memory|
|Resilience|Microsoft.Extensions.Http.Resilience|
|Secrets (prod)|Azure Key Vault via Azure.Extensions.AspNetCore.Configuration.Secrets|

\---

## Local Setup

### Prerequisites

* .NET 8 SDK
* SQL Server (LocalDB is fine)
* An Azure OpenAI resource with a `gpt-4o` deployment
* Visual Studio 2022 or VS Code

### Steps

**1. Clone the repo**

```bash
git clone https://github.com/sharad99kr/Bulky\_MVC.git
cd Bulky
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
dotnet user-secrets set "AzureOpenAI:Endpoint" "https://YOUR-RESOURCE.openai.azure.com/"
dotnet user-secrets set "AzureOpenAI:ApiKey"   "your-api-key-here"
dotnet user-secrets set "Stripe:SecretKey"     "sk\_test\_..."
dotnet user-secrets set "Stripe:PublishableKey" "pk\_test\_..."
```

**4. Run**

```bash
dotnet run --project ProjectCore
```

The AI description generator is available in the admin area under any product's edit page.

\---

## AI Layer NuGet Packages

```powershell
# Run in ProjectCore
Install-Package Azure.AI.OpenAI
Install-Package Microsoft.SemanticKernel
Install-Package Microsoft.SemanticKernel.Connectors.OpenAI
Install-Package Microsoft.Extensions.Caching.Memory
Install-Package Microsoft.Extensions.Http.Resilience
Install-Package Azure.Extensions.AspNetCore.Configuration.Secrets
```

> `Azure.AI.OpenAI 2.9.0-beta.1` is a beta package — this is by design. Semantic Kernel consistently depends on the latest beta of this SDK across versions. It is production-safe.

\---

## Roadmap

* \[x] Week 1 — AI service layer + tone-aware description generator
* \[ ] Week 2 — RAG semantic search implementation
* \[ ] Week 3 — TBD

\---

## Project Structure (Multi-Project Solution)

```
Bulky.sln
├── Bulky.DataAccess     ← EF Core DbContext, repositories, migrations
├── Bulky.Models         ← Domain entities (Product, Category, Order, etc.)
├── Bulky.Utility        ← Constants, email service, Stripe helpers
└── ProjectCore          ← MVC web project (controllers, views, AI layer)
```

\---

## License

MIT


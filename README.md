# CloudBank

**An event-driven banking platform demonstrating production-grade microservices architecture on Azure.**

CloudBank simulates a digital banking platform — customers can create accounts, transfer money, and view transaction history, with a machine-learning fraud-detection layer scoring every transaction in real time. It is not a real bank; it's an architectural demonstration of how a real bank's backend would actually be built: independently-deployable services, asynchronous event communication, distributed transaction handling, caching, security, and observability, running on real cloud infrastructure rather than a single local process.

---

## Why This Project Exists

Financial systems demand strong architecture — consistency guarantees, auditability, security. Building one (even a simulation) forces genuinely solid design decisions rather than a simple CRUD app. This project exists to demonstrate:

- Designing a **distributed system**, not just a single API
- **Data consistency without shared databases** (Saga pattern)
- Comfort with **asynchronous, event-driven design**
- Practical **cloud deployment** experience (containers, orchestration, CI/CD)
- **Security fundamentals** (auth, secrets management, RBAC)
- Integrating **ML into a production-style system**, including cross-language service communication

---

## Architecture

```
                        ┌─────────────────┐
                        │   API Gateway    │  (YARP)
                        │  + JWT Auth       │
                        └────────┬─────────┘
              ┌──────────────────┼──────────────────┐
              ▼                  ▼                    ▼
      ┌───────────────┐ ┌────────────────┐  ┌──────────────────┐
      │ Accounts Svc  │ │ Transactions Svc│  │ Redis Cache        │
      │ + Postgres DB │ │ + Postgres DB   │  │ (balance reads)    │
      └───────┬───────┘ └────────┬────────┘  └──────────────────┘
              │                  │ publishes events
              │                  ▼
              │          ┌──────────────────┐
              │          │  Azure Service Bus │
              │          └─────────┬──────────┘
              │      ┌─────────────┼─────────────────┐
              │      ▼             ▼                  ▼
              │ ┌──────────┐ ┌───────────┐   ┌─────────────────┐
              │ │Audit Svc │ │Notif. Svc │   │ Fraud Detection  │
              │ │+ Postgres│ │           │   │ Svc (Python/     │
              │ └──────────┘ └───────────┘   │ FastAPI + model) │
              │                              └────────┬─────────┘
              └────────────────────────────────────────┘

React Dashboard (Azure Static Web Apps) → API Gateway
```

### Services

| Service | Language | Owns | Responsibility |
|---|---|---|---|
| Accounts | C# / .NET | `accounts_db` (Postgres) | User accounts, balances |
| Transactions | C# / .NET | `transactions_db` (Postgres) | Transfers, saga orchestration |
| Audit | C# / .NET | `audit_db` (Postgres) | Immutable event log |
| Notifications | C# / .NET | stateless | Consumes events, alerts users |
| Fraud Detection | Python / FastAPI | stateless | ML-based transaction risk scoring |
| API Gateway | C# / YARP | — | Single entry point, routing, JWT validation |

**Database-per-service**: each service owns its own database — no service reaches into another's tables directly.

### Key Architectural Patterns

- **Clean Architecture** per service — Domain → Application → Infrastructure → Api, with dependencies pointing strictly inward
- **CQRS** — commands (writes) and queries (reads) handled through separate code paths via MediatR
- **Event-driven communication** — services publish events to Azure Service Bus rather than calling each other synchronously
- **Saga pattern** — distributed transactions across service boundaries via compensating actions, not cross-service DB transactions
- **Cache-aside pattern** — Redis for balance reads, invalidated (not updated) on writes
- **Idempotency** — client-generated idempotency keys prevent duplicate transactions on retry

---

## Implementation Status

This project is being built incrementally and documented as a learning log. This section reflects what's **actually implemented**, not the end-state vision above.

### ✅ Done
- **Accounts service** — Clean Architecture skeleton (Domain / Application / Infrastructure / Api)
- Domain layer: `Account` entity as a rich domain model (private setters, business rules enforced in methods)
- Infrastructure: EF Core + Npgsql, `AccountsDbContext` with Fluent API mapping, first migration applied
- Local PostgreSQL via Docker Compose
- Full CQRS flow: `CreateAccountCommand` (+ Handler), `GetAccountByIdQuery` (+ Handler), wired through MediatR
- FluentValidation with an automatic MediatR pipeline behavior (validation runs before every handler, no per-handler boilerplate)
- Centralized exception-handling middleware (`ValidationException` → 400, `KeyNotFoundException` → 404, unhandled → 500)
- Local secrets managed via .NET User Secrets (kept out of source control)

### 🔜 Planned (in build order)
- Unit tests (xUnit) for handlers and validators
- Transactions service + Saga pattern for account-to-account transfers
- Azure Service Bus integration (event publishing/consuming)
- Audit service (event log) and Notifications service
- Redis caching (cache-aside) for balance reads
- Fraud Detection service (Python/FastAPI + scikit-learn Random Forest classifier)
- Azure AD B2C authentication + role-based access control
- API Gateway (YARP)
- React frontend
- Docker + AKS + GitHub Actions CI/CD
- Application Insights (distributed tracing, monitoring)

### Known Limitations (by design)
- Fraud model, once built, will be trained on a public dataset, not real transaction patterns — a stated, intentional limitation
- No real payment rails/banking integration — this is an architectural simulation
- Single-region deployment — no multi-region failover
- Local-first development strategy: full local development in Docker, with real Azure deployment reserved for milestone demos (then torn down) to stay within free-tier/trial budget — see [Cost Notes](#cost-notes) below

---

## Tech Stack

- **Backend**: C# / .NET 10, ASP.NET Core Web API
- **Data access**: EF Core, Npgsql (PostgreSQL provider)
- **CQRS/Mediator**: MediatR
- **Validation**: FluentValidation
- **Database**: PostgreSQL (containerized via Docker Compose locally)
- **Planned**: Redis, Azure Service Bus, Python/FastAPI, Azure AD B2C, Azure Kubernetes Service, GitHub Actions, Application Insights, React

---

## Getting Started (Accounts Service)

### Prerequisites
- .NET 10 SDK
- Docker Desktop (with WSL2 backend on Windows)
- Git

### Run Locally

1. Clone the repo:
   ```bash
   git clone https://github.com/suchit3k/CloudBank.git
   cd CloudBank
   ```

2. Start PostgreSQL:
   ```bash
   docker compose up -d
   ```

3. Configure the connection string via User Secrets (from `src/Services/Accounts/Accounts.Api`):
   ```bash
   dotnet user-secrets set "ConnectionStrings:AccountsDb" "Host=localhost;Port=5432;Database=accounts_db;Username=cloudbank;Password=cloudbank_dev_pw"
   ```

4. Apply migrations:
   ```bash
   cd src/Services/Accounts/Accounts.Infrastructure
   $env:ASPNETCORE_ENVIRONMENT="Development"
   dotnet ef database update --startup-project ../Accounts.Api
   ```

5. Run the API:
   ```bash
   cd ../Accounts.Api
   dotnet run
   ```

6. Open Swagger at `https://localhost:<port>/swagger` and try the `Accounts` endpoints.

---

## Cost Notes

CloudBank's full architecture does not run entirely free on Azure long-term — the main cost drivers are AKS worker nodes (cluster management is free, but VMs are not) and multiple PostgreSQL Flexible Server instances. Development happens entirely locally via Docker Compose at zero cost; Azure resources are deployed only for milestone demos, then torn down to avoid ongoing billing.

---

## Project Structure

```
CloudBank/
├── docker-compose.yml
├── src/
│   └── Services/
│       └── Accounts/
│           ├── Accounts.Domain/          # Entities, business rules — no framework dependencies
│           ├── Accounts.Application/     # Commands, Queries, Handlers, Validators, interfaces
│           ├── Accounts.Infrastructure/  # EF Core, repositories, migrations
│           └── Accounts.Api/             # Controllers, Program.cs, middleware
```

Additional services (`Transactions`, `Audit`, `Notifications`, `FraudDetection`, `Gateway`) will follow the same per-service Clean Architecture structure as they're built.

---

## Development Log

This project is being built and documented as a structured learning journey — architecture decisions, debugging notes, and concepts covered along the way are tracked session by session. That log is maintained separately and available on request.

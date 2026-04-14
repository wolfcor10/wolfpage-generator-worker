# WolfPage Generator Worker

A **.NET Worker Service** that consumes messages from **RabbitMQ** to generate web pages from **database-stored templates**, persisting the final result to **SQL Server** via Entity Framework Core.

## Overview

The worker receives a page generation request, fetches the corresponding template version, renders its HTML/CSS/JS content with the data provided in the message, and saves the generated page to the database. Failures are tracked and the request status is updated accordingly.

## Flow

```
Producer → RabbitMQ (site.generate) → Worker
                                         │
                                         ├─ Register PageGenerationRequest (Processing)
                                         ├─ Fetch TemplateVersion
                                         ├─ Render HTML / CSS / JS
                                         ├─ Save Page
                                         └─ Update request → Completed | Failed
```

## Tech Stack

| Technology | Role |
|---|---|
| .NET 10 Worker Service | Host and message consumption |
| C# | Implementation language |
| RabbitMQ / RabbitMQ.Client 6.x | Message broker |
| Entity Framework Core 9 | ORM and migrations |
| SQL Server | Persistence |

## Solution Structure

```text
src/
├─ WolfPage.Generator.Domain/
│  ├─ Entities/
│  │  ├─ Tenant.cs
│  │  ├─ Template.cs
│  │  ├─ TemplateVersion.cs
│  │  ├─ PageGenerationRequest.cs
│  │  ├─ Page.cs
│  │  ├─ PageAsset.cs
│  │  └─ DomainBinding.cs
│  └─ Enums/
│     ├─ RequestStatus.cs
│     └─ PageStatus.cs
│
├─ WolfPage.Generator.Application/
│  ├─ Messages/
│  │  └─ CreatePageRequestedMessage.cs
│  ├─ Persistence/
│  │  └─ IPageGenerationRepository.cs
│  ├─ Services/
│  │  ├─ IPageGenerationService.cs
│  │  └─ PageGenerationService.cs
│  └─ Rendering/
│     ├─ ITemplateRenderer.cs
│     └─ SimpleTemplateRenderer.cs
│
├─ WolfPage.Generator.Infrastructure/
│  ├─ Persistence/
│  │  ├─ AppDbContext.cs
│  │  └─ PageGenerationRepository.cs
│  └─ Options/
│     └─ RabbitMqOptions.cs
│
└─ WolfPage.Generator.Worker/
   ├─ Consumers/
   │  └─ RabbitMqPageConsumerService.cs
   ├─ Program.cs
   └─ appsettings.json
```

## Architecture

Dependency flow follows clean architecture principles:

```
Domain  ←  Application  ←  Infrastructure  ←  Worker
```

- **Domain** — entities and enums, no external dependencies
- **Application** — use cases, service contracts, repository interface (`IPageGenerationRepository`)
- **Infrastructure** — EF Core, `AppDbContext`, repository implementation (`PageGenerationRepository`)
- **Worker** — host, DI wiring, RabbitMQ consumer

Application depends on Domain only. Infrastructure implements the contracts defined in Application. The Worker composes everything together.

## Database

SQL Server tables (snake_case naming):

| Table | Description |
|---|---|
| `tenant` | Client or site owner |
| `template` | Base template definition |
| `template_version` | Versioned template with HTML, CSS, JS and schema |
| `page_generation_request` | Audit record of each processing attempt |
| `page` | Final generated page |
| `page_asset` | Assets associated to a page |
| `domain_binding` | Domain or subdomain linked to a page |

**Recommended SQL Server types:** `uniqueidentifier` for IDs · `nvarchar` for text · `bit` for booleans · `datetime2` for dates

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (local or Docker)
- RabbitMQ (local or Docker)

### Run RabbitMQ with Docker

```bash
docker run -d --name rabbitmq \
  -p 5672:5672 \
  -p 15672:15672 \
  rabbitmq:3-management
```

Management UI available at `http://localhost:15672` (guest / guest).

### Configuration

Edit `src/WolfPage.Generator.Worker/appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=WolfPageGeneratorDb;Trusted_Connection=True;TrustServerCertificate=True"
  },
  "RabbitMq": {
    "HostName": "localhost",
    "Port": 5672,
    "UserName": "guest",
    "Password": "guest",
    "VirtualHost": "/",
    "QueueName": "site.generate"
  }
}
```

### Migrations

```bash
# Create the initial migration
dotnet ef migrations add InitialCreate \
  --project src/WolfPage.Generator.Infrastructure \
  --startup-project src/WolfPage.Generator.Worker

# Apply to the database
dotnet ef database update \
  --project src/WolfPage.Generator.Infrastructure \
  --startup-project src/WolfPage.Generator.Worker
```

### Run

```bash
dotnet run --project src/WolfPage.Generator.Worker
```

## Sample RabbitMQ Message

Publish to the `site.generate` queue with the following payload:

```json
{
  "correlationId": "REQ-0001",
  "tenantId": "11111111-1111-1111-1111-111111111111",
  "templateVersionId": "22222222-2222-2222-2222-222222222222",
  "pageName": "My Barbershop",
  "slug": "my-barbershop",
  "content": {
    "title": "My Barbershop",
    "heroText": "Modern and classic cuts",
    "phone": "3001234567",
    "address": "123 Main Street",
    "primaryColor": "#111111"
  }
}
```

Template placeholders use `{{key}}` syntax (e.g. `{{title}}`, `{{heroText}}`).

## Roadmap

- [ ] Idempotency check by `correlationId`
- [ ] Dead-letter queue for failed messages
- [ ] `schema_json` validation before rendering
- [ ] Page publishing to storage or filesystem
- [ ] Success/failure domain events
- [ ] Unit and integration tests

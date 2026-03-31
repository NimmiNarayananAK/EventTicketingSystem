# Event Ticketing System API

A clean, RESTful API for managing events and ticket sales, built with .NET 8 and Entity Framework Core.

## 📋 Table of Contents

- [Overview](#overview)
- [Architecture & Design Decisions](#architecture--design-decisions)
- [Technology Stack](#technology-stack)
- [Prerequisites](#prerequisites)
- [Setup Instructions](#setup-instructions)
- [Running the Application](#running-the-application)
- [API Endpoints](#api-endpoints)
- [Testing](#testing)
- [Trade-offs & Considerations](#trade-offs--considerations)
- [Scalability Considerations](#scalability-considerations)
- [Future Enhancements](#future-enhancements)
- [AI Tool Usage](#ai-tool-usage)

---

## 🎯 Overview

This Event Ticketing System provides a comprehensive solution for managing events with multiple pricing tiers, handling ticket purchases with concurrency control, and generating sales reports.

### Core Features

✅ **Event Management**
- Create, read, update, cancel, and delete events
- Support for multiple pricing tiers per event
- Event status management (active, canceled) with business rules

✅ **Ticket Management**
- Purchase tickets, check availability, cancel tickets, and prevent overselling
- Prevent overselling (at both pricing tier and event level) with transaction-based concurrency control
- Cancellation of tickets with inventory restoration

✅ **Reporting**
- Sales summaries with revenue and tier-level breakdown

✅ **Robust Error Handling**
- Global exception middleware
- Comprehensive validation
- Detailed error messages with appropriate HTTP status codes
- Handling of edge cases (e.g., overselling, capacity mismatches, concurrent purchases)

**Notes & Assumptions**
-User authentication is not implemented to keep the focus on core ticketing logic. Email notifications are out of scope for this exercise.

---

## Architecture & Design Decisions

### Architectural Pattern: **Pragmatic 2-Layer Architecture**

**The solution is structured into separate API, Infrastructure & Tests layers to maintain a clear separation of concerns.The API layer is responsible for HTTP handling, while the Infrastructure layer contains business logic and data access.:**
- **API Layer**
  - Controllers
  - DTOs
  - Validators
  - Middleware
- **Infrastructure Layer**
  - Entities
  - DbContext
  - Services (business logic)
- **Tests**
  - Unit Tests (service-level)
  - Integration Tests (API-level)

**Direct use of EF Core instead of Repository Pattern**
-EF Core's `DbContext` already implements the repository and unit of work patterns. Adding an additional abstraction layer would introduce
unnecessary complexity for the scope of this task. This approach keeps the design simple while remaining testable via the EF Core in-memory provider.

**Validation Strategy**
-FluentValidation is used for request validation (e.g., required fields, basic constraints). Validators are registered in DI and called explicitly in controllers with `ValidateAsync()`.
-Business rules (e.g., ticket availability, event status) are enforced in the service layer. Each business rule violation throws a specific exception type and the global middleware maps these to HTTP responses. 
This keeps validation concerns cleanly separated.

**Concurrency & Data Integrity**
- Ticket purchases are wrapped in transactions
- Inventory is updated atomically to prevent overselling

**DTO Separation**
Request and response models are kept separate from domain entities.

**Global Exception Middleware**
Rather than try/catch in every controller, exceptions are handled centrally and the middleware maps them.

**Database Choice - SQLite**
- Easy setup (no external dependencies), sufficient for the scope of this task, and supports transactions for concurrency control.
- Portability for reviewers

---

## 🛠️ Technology Stack

| Technology | Purpose |
|------------|---------|
| **.NET 8 / ASP.NET Core** | Framework & Web API |
| **Entity Framework Core** | ORM & migrations |
| **SQLite (used for simplicity and portability)** | Database |
| **FluentValidation** | Request validation |
| **xUnit + FluentAssertion** | Unit & integration testing |
| **Swagger / OpenAPI** | Documentation |

---

## Prerequisites

- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **IDE**:
  - Visual Studio 2022 (v17.8 or later)
- **Git** (for cloning the repository)

---

## Setup Instructions

### 1. Clone the Repository

git clone https://github.com/NimmiNarayananAK/EventTicketingSystem.git <localdir>

Open the project in Visual Studio

### 2. Restore NuGet Packages

dotnet restore

### 3. Create the database

Open **Package Manager Console** in Visual Studio:

Set the default Project in the console as EventTicketing.Infrastructure and run:
Update-Database -Project EventTicketing.Infrastructure -StartupProject EventTicketing.API

This will create the eventticketing.db (SQLite database) in the API project directory and seed it with initial data.

### 4. Build the Solution

dotnet build

---

## Running the Application

1. Open `EventTicketing.sln`
2. Set `EventTicketing.API` as the startup project
3. Press `F5`

### Access Points

- **Swagger UI**: https://localhost:{port}}/swagger
- **API Base URL**: https://localhost:{port}/api

- The port is shown in the console output on startup.

---

## API Endpoints

### Events

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/events` | List all events |
| `GET` | `/api/events/{id}` | Get event by ID |
| `POST` | `/api/events` | Create a new event |
| `PUT` | `/api/events/{id}` | Update event details and pricing tiers |
| `PATCH` | `/api/events/{id}/status` | Update event status |
| `DELETE` | `/api/events/{id}` | Delete a draft event |

### Tickets

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/events/{eventId}/tickets/availability` | Check ticket availability |
| `GET` | `/api/events/{eventId}/tickets` | List tickets for an event |
| `POST` | `/api/events/{eventId}/tickets/purchase` | Purchase tickets |
| `POST` | `/api/events/{eventId}/tickets/{ticketId}/cancel` | Cancel a ticket |

### Reports

| Method | Endpoint | Description |
|---|---|---|
| `GET` | `/api/reports/sales` | Sales summary for all events |
| `GET` | `/api/reports/sales/{eventId}` | Sales summary for a specific event |


### API Testing

You can test the API using:

Swagger (available at /swagger)

Sample request payloads for key endpoints (Create/Update Event & Purchase) are included in separate files for convenience. For Update & Purchase payloads, make sure to use the correct event IDs.

---

## Unit & Integration Testing

### Run All Tests

dotnet test

### Run Specific Test Projects

Unit tests only
dotnet test EventTicketing.UnitTests

Integration tests only
dotnet test EventTicketing.IntegrationTests


### What is tested

**Unit Tests** — service logic using an in-memory database:
- Event creation, update, status transitions, and deletion rules
- Overselling prevention at both tier and event level
- Quantity limit and Early Bird expiry enforcement
- Ticket cancellation and inventory restoration
- Sales report accuracy

**Integration Tests** — full HTTP flow via `WebApplicationFactory`:
- All CRUD endpoints return correct HTTP status codes
- Purchase reduces availability correctly
- Sequential purchases never oversell
- Validation errors return `400` with field-level details

### Edge Cases Handled

| Scenario | Handling |
|----------|----------|
| **Overselling** | Transactional updates prevent inconsistent inventory |
| **Capacity Mismatch** | Validation ensures tier capacities match event capacity |
| **Concurrent Purchases** | Transactions ensure atomic updates |
| **Delete Event with Tickets** | Business rule prevents deletion |
| **Invalid Tier Selection** | Returns clear error message |
| **Negative Quantities** | Rejected via validation |

---

## Trade-offs & Considerations

| Decision | Chosen | Alternative | Reason for choice |
|---|---|---|---|
| Architecture | 2-layer (API + Infrastructure) | Clean Architecture | Right fit for scope, less overhead |
| EF Core instead / Repository Pattern | EF Core | Repository Pattern | Right fit for scope, EF Core's DB Context already implements Reporsitory + Unit of Work, less overhead |
| Database | SQLite | PostgreSQL / SQL Server | Zero config, portable; easy swap in the future |
| Validation | FluentValidation (explicit) | Data Annotations | Richer rules, testable in isolation |
| Mapping | Manual | AutoMapper | Transparent |
| Concurrency | DB transactions | Optimistic concurrency (RowVersion) | Simpler, sufficient for this scale |

---

## Scalability Considerations

The API is stateless and horizontally scalable. Potential bottlenecks and future improvements include:

| Bottleneck | Solution |
|---|---|
| Ticket purchase contention | Introduce queue-based processing (e.g. Azure Service Bus) |
| Wasted compute on cancelled requests | Propagate CancellationToken through all async operations |
| SQLite single-writer limit | Migrate to PostgreSQL or SQL Server |
| High read traffic | Add caching (e.g., Redis) with proper invalidation |
| Reporting performance | Use pre-aggregated read models (CQRS) |
| API protection           | Add rate limiting and API gateway support (Azure API Management) |

---

## Future Improvements

1. **Authentication & user management** — JWT with role-based access (Organiser vs Attendee)
2. **Introduce pagination & filtering for large datasets** — `GET /api/events?page=1&pageSize=20`
3. **Email Notifications** — Ticket confirmation on purchase
4. **Idempotency Keys** — Prevent duplicate purchases on network retry
5. **Structured Logging** — Introduce Serilog with correlation IDs for traceability
6. **Rate Limiting** — Protect purchase endpoints from abuse
7. **Payment Integration**
---

## AI Tool Usage

**Tool: GitHub Copilot in Visual Studio 2022**

Copilot was used to accelerate repetitive and boilerplate tasks, including:
- Generate the initial boilerplate code based on the provided requirements 
- Creating initial test cases
- Drafting README tables and sections

All generated code was reviewed, refined, and adjusted to ensure correctness, proper handling of edge cases, and alignment with the intended design. Architectural decisions, trade-offs, and final implementations were driven manually.


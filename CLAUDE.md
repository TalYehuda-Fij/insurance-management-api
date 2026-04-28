# CLAUDE.md — Insurance Management System API

## Overview

Backend API for an insurance agent to manage customers and policies. Take-home challenge (~5 hours).

## Stack

- .NET 8 / C# 12 — Clean Architecture — SQLite via EF Core — xUnit for unit tests
- **No heavy libraries** (no MediatR, AutoMapper, FluentValidation). Use built-in .NET features only.

## Solution Structure

```
src/
  InsuranceApi.Domain/            # Entities, enums, exceptions. ZERO dependencies.
  InsuranceApi.Application/       # Services, interfaces, DTOs. References Domain only.
  InsuranceApi.Infrastructure/    # EF Core, repositories. References Application + Domain.
  InsuranceApi.Api/              # Controllers, middleware, DI setup. Thin layer.
tests/
  InsuranceApi.UnitTests/         # Tests for services and domain logic.
```

## Why Clean Architecture (know this for the interview)

The idea is simple: **dependencies point inward**. The core business logic (Domain + Application) knows nothing about the database, the web framework, or any external tool. The outer layers (Infrastructure, Api) depend on the core — never the other way around.

- **Domain** is the center — pure C# classes, no NuGet packages, no `using Microsoft.EntityFrameworkCore`. It defines *what* a Customer and Policy are.
- **Application** defines *what the system does* — service interfaces, DTOs, business rules. It says "I need a repository that can save a customer" via an interface (`ICustomerRepository`), but doesn't know or care if it's SQLite, Postgres, or a text file.
- **Infrastructure** is *how it's actually done* — it implements `ICustomerRepository` using EF Core. This is the only layer that knows about the database.
- **Api** is the entry point — receives HTTP requests, calls the Application services, returns responses.

**Why this matters:** If tomorrow you swap SQLite for Postgres, you change one project (Infrastructure). Business rules don't change. Controllers don't change. Tests don't break. That's the whole point.

## Entities

**Customer:** Id (Guid), FirstName, LastName, IdNumber, DateOfBirth (DateOnly), Email?, Phone, CreatedAt, Policies (nav).

**Policy:** Id (Guid), PolicyNumber (unique, auto-generated), Type (enum: Car/Health/Life/Home), StartDate (DateOnly), EndDate (DateOnly), PremiumAmount (decimal), Status (enum: Active/Cancelled/Expired), CustomerId (FK), CreatedAt.

## Endpoints

**Customers:** POST `/api/customers` | GET `/api/customers` | GET `/api/customers/{id}` | PUT `/api/customers/{id}`

**Policies:** POST `/api/customers/{customerId}/policies` | GET `/api/policies?type=&status=` | GET `/api/policies/{id}` | PUT `/api/policies/{id}` | PATCH `/api/policies/{id}/cancel`

## Business Rules (3 Required)

1. **No duplicate active policies** — a customer cannot have two Active policies of the same type.
2. **Premium within type-specific range** — each PolicyType has a Min/Max premium (e.g., Car 500–50,000, Life 100–100,000).
3. **Duration within type-specific range** — each PolicyType has a Min/Max duration in days (e.g., Car 180–731, Life 3,650–10,958).

## Code Style & SOLID Principles

- **Follow SOLID strictly** — single responsibility per class, depend on abstractions (interfaces), keep classes open for extension, use small focused interfaces, inject dependencies via constructor.
- File-scoped namespaces. One class per file. Use `var` when type is obvious.
- **Records** for DTOs: `CreateCustomerRequest`, `CustomerResponse`, etc.
- **PascalCase** for public members. **_camelCase** for private fields. Async methods end with `Async`.
- **Thin controllers** — no logic, just call service and return result. Services own all business logic. Repos handle data access only.
- Return DTOs from controllers, never entities. Use proper HTTP status codes (201, 400, 404, 409).
- Custom exceptions (`BusinessRuleException`, `NotFoundException`) + global exception middleware.
- EF Core: code-first, `IEntityTypeConfiguration<T>` for entity config (no data annotations).

## Testing

- xUnit, mock repositories with NSubstitute or manual fakes.
- Test all 3 business rules + happy paths.
- Naming: `MethodName_ShouldResult_WhenCondition`.

## Git

- Conventional commits: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`

## README Must Include

Setup instructions, architecture explanation, business assumptions, and future-proofing (scaling to 1M+ policies: swap to Postgres, add indexes, pagination, caching).

## Out of Scope

Auth, Docker, CI/CD, frontend, advanced logging.

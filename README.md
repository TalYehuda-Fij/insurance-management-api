# Insurance Management System API

A .NET 8 REST API for managing insurance customers and policies.

---

## Setup

**Prerequisites:** .NET 8 SDK

```bash
cd InsuranceApi
dotnet restore
dotnet run --project src/InsuranceApi.Api
```

The API starts on `https://localhost:7xxx` / `http://localhost:5xxx`.  
Swagger UI is available at `/swagger` in Development mode.  
The SQLite database (`insurance.db`) is auto-created on first run.

**Run tests:**
```bash
dotnet test
```

---

## Architecture

Clean Architecture — dependencies point inward.

```
Domain ← Application ← Infrastructure
                     ← Api
```

- **Domain** — `Customer`, `Policy` entities, `PolicyType`/`PolicyStatus` enums, `BusinessRuleException`, `NotFoundException`. Zero external dependencies.
- **Application** — `CustomerService`, `PolicyService`, DTOs (records), repository interfaces. Knows only about Domain.
- **Infrastructure** — EF Core + SQLite implementation of repositories, `AppDbContext`, entity configurations (`IEntityTypeConfiguration<T>`).
- **Api** — Controllers (thin — call service, return result), exception middleware, DI wiring, `Program.cs`.

**Why this matters:** swapping SQLite for Postgres means touching only Infrastructure. Business rules and controllers remain unchanged.

---

## Endpoints

### Customers
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/customers` | Create customer |
| GET | `/api/customers` | List all customers |
| GET | `/api/customers/{id}` | Get customer by ID |
| PUT | `/api/customers/{id}` | Update customer |

### Policies
| Method | Path | Description |
|--------|------|-------------|
| POST | `/api/customers/{customerId}/policies` | Create policy for customer |
| GET | `/api/policies?type=Car&status=Active` | List policies (optional filters) |
| GET | `/api/policies/{id}` | Get policy by ID |
| PUT | `/api/policies/{id}` | Update policy |
| PATCH | `/api/policies/{id}/cancel` | Cancel policy |

---

## Business Rules

1. **No duplicate active policies of the same type** — a customer cannot hold two Active policies of the same type (Car/Health/Life/Home). Returns `409 Conflict`.
2. **Premium within type-specific range** — each `PolicyType` has its own valid premium range. Defined centrally in `Domain/Rules/PolicyTypeRules.cs`. Returns `409 Conflict` when violated.
   | Type | Min | Max |
   |------|-----|-----|
   | Car | 500 | 50,000 |
   | Health | 200 | 30,000 |
   | Life | 100 | 100,000 |
   | Home | 1,000 | 50,000 |
3. **Duration within type-specific range** — each `PolicyType` has its own valid duration window (in days). Short-term contracts make sense for Car/Health/Home; Life is multi-year. Returns `409 Conflict` when violated.
   | Type | Min days | Max days |
   |------|----------|----------|
   | Car | 180 | 731 |
   | Health | 180 | 731 |
   | Life | 3,650 | 10,958 |
   | Home | 180 | 731 |

Additional guard: `EndDate` must be on or after `StartDate`.

---

## Business Assumptions

- `IdNumber` is unique per customer (national ID / passport).
- Email is optional; Phone is required.
- Policy type is immutable only in spirit — PUT allows updating it, but the duplicate-active-policy rule re-applies.
- Expired policies are not managed automatically (no background job); status stays `Active` until explicitly cancelled or updated.
- `CreatedAt` is always UTC.

---

## Scaling to 1M+ Policies

| Concern | Current | At Scale |
|---------|---------|----------|
| Database | SQLite (file, single writer) | PostgreSQL with connection pooling |
| Indexes | Unique on `IdNumber`, `PolicyNumber` | Add composite index on `(CustomerId, Type, Status)` for the duplicate-policy check |
| Pagination | Not implemented | Cursor-based or offset pagination on list endpoints |
| Caching | None | Redis for hot customer reads; cache invalidate on update |
| Policy expiry | Manual | Background job (Hangfire / Quartz.NET) to flip `Active → Expired` nightly |
| Read/Write separation | None | CQRS split — query handlers hit a read replica |

## Known Trade-offs

- **`EnsureCreated` instead of EF migrations** — the schema is created on first run. If the schema changes (e.g. a new index is added), an existing `insurance.db` will not be updated. For any real deployment, replace with `dotnet ef migrations add <Name> && dotnet ef database update`. Delete the local `insurance.db` and let it recreate when the schema changes during development.
- **No integration tests** — all 20 tests mock the repositories. A real integration test suite would exercise EF Core mappings against an in-memory or file-based SQLite database.
- **Structured logging** — the exception middleware uses `ILogger`, but there is no request-level logging. `app.UseHttpLogging()` or Serilog would be added before production.

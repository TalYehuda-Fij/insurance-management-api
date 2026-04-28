# Architecture & Application Flows

This document explains how the Insurance Management System API is structured, why each layer exists, and how a request flows through the system from HTTP to database and back.

---

## 1. Solution Layout

```
InsuranceApi.sln
├── src/
│   ├── InsuranceApi.Domain/          ← The center. Pure C#. Zero dependencies.
│   ├── InsuranceApi.Application/     ← Business logic, DTOs, interfaces.
│   ├── InsuranceApi.Infrastructure/  ← EF Core, SQLite, repositories.
│   └── InsuranceApi.Api/             ← Controllers, middleware, DI.
└── tests/
    └── InsuranceApi.UnitTests/       ← xUnit + NSubstitute.
```

### Dependency direction

Dependencies always point **inward** toward the Domain. The arrows below mean "depends on":

```
Api ──────────► Application ──────► Domain
                     ▲                ▲
                     │                │
              Infrastructure ─────────┘
```

- **Domain** depends on nothing. It is the most stable thing in the system.
- **Application** depends only on Domain. It defines *what the system does* through service classes and *what it needs* through repository interfaces.
- **Infrastructure** depends on Application and Domain. It supplies concrete implementations (e.g., EF Core repositories) for the interfaces that Application defines.
- **Api** depends on Application (to call services) and Infrastructure (only for DI registration in `Program.cs`).

This means: if SQLite is replaced with PostgreSQL tomorrow, only Infrastructure changes. The business rules, DTOs, and controllers are untouched.

---

## 2. What Lives in Each Layer

### Domain (`InsuranceApi.Domain`)

The vocabulary of the business — what a Customer and Policy *are*.

| File | Purpose |
|------|---------|
| `Entities/Customer.cs` | Customer entity with `Id`, name, `IdNumber`, `DateOfBirth`, contact info, `Policies` navigation. |
| `Entities/Policy.cs` | Policy entity with `PolicyNumber`, `Type`, dates, `PremiumAmount`, `Status`, `CustomerId` FK. |
| `Enums/PolicyType.cs` | `Car`, `Health`, `Life`, `Home`. |
| `Enums/PolicyStatus.cs` | `Active`, `Cancelled`, `Expired`. |
| `Exceptions/BusinessRuleException.cs` | Thrown when a domain rule is violated. Maps to HTTP 409. |
| `Exceptions/NotFoundException.cs` | Thrown when an entity is not found. Maps to HTTP 404. |

**No EF Core attributes, no `using Microsoft.*`** — this layer would still compile if you removed every NuGet package in the solution.

### Application (`InsuranceApi.Application`)

Defines *what the system does* and *what it needs from the outside world*.

| File | Purpose |
|------|---------|
| `DTOs/Customer/*.cs` | `CreateCustomerRequest`, `UpdateCustomerRequest`, `CustomerResponse` records. |
| `DTOs/Policy/*.cs` | `CreatePolicyRequest`, `UpdatePolicyRequest`, `PolicyResponse` records. |
| `Interfaces/ICustomerRepository.cs` | Contract for customer persistence — Application says "I need this", Infrastructure provides it. |
| `Interfaces/IPolicyRepository.cs` | Contract for policy persistence. |
| `Services/CustomerService.cs` | Customer use cases: create, get, list, update. Enforces the unique `IdNumber` rule. |
| `Services/PolicyService.cs` | Policy use cases: create, list with filters, get, update, cancel. Enforces all 3 business rules. |

DTOs are **records** — immutable, value-equality, perfect for transport. Entities never leave this layer.

### Infrastructure (`InsuranceApi.Infrastructure`)

The "how it actually happens" layer.

| File | Purpose |
|------|---------|
| `Persistence/AppDbContext.cs` | EF Core `DbContext` exposing `Customers` and `Policies`. |
| `Persistence/Configurations/CustomerConfiguration.cs` | `IEntityTypeConfiguration<Customer>` — keys, max lengths, unique index on `IdNumber`. |
| `Persistence/Configurations/PolicyConfiguration.cs` | `IEntityTypeConfiguration<Policy>` — `PolicyNumber` unique, decimal precision, enums-as-strings. |
| `Repositories/CustomerRepository.cs` | Implements `ICustomerRepository` against EF Core. |
| `Repositories/PolicyRepository.cs` | Implements `IPolicyRepository` with optional filtering on type/status. |

Entity config is done via `IEntityTypeConfiguration<T>` (no data annotations on entities) so the Domain stays free of EF.

### Api (`InsuranceApi.Api`)

The HTTP entry point.

| File | Purpose |
|------|---------|
| `Controllers/CustomersController.cs` | Routes for `/api/customers`. Thin — accepts DTOs, calls service, returns result. |
| `Controllers/PoliciesController.cs` | Routes for `/api/customers/{id}/policies` and `/api/policies/...`. |
| `Middleware/ExceptionMiddleware.cs` | Catches `NotFoundException` → 404, `BusinessRuleException` → 409, anything else → 500. |
| `Program.cs` | DI registration, EF Core SQLite setup, `EnsureCreated()`, middleware pipeline. |

Controllers contain **no logic**. They translate HTTP ↔ service calls.

---

## 3. Dependency Injection Wiring

`Program.cs` is the composition root. It wires interfaces to implementations:

```csharp
builder.Services.AddDbContext<AppDbContext>(opts =>
    opts.UseSqlite(connStr));

builder.Services.AddScoped<ICustomerRepository, CustomerRepository>();
builder.Services.AddScoped<IPolicyRepository, PolicyRepository>();
builder.Services.AddScoped<CustomerService>();
builder.Services.AddScoped<PolicyService>();
```

Lifetimes are all **Scoped** — one instance per HTTP request, matching the `DbContext` lifetime so a request shares one transactional unit of work.

---

## 4. Request Flow — Walkthrough

### Example A: Create a customer

`POST /api/customers` with body:
```json
{
  "firstName": "Alice",
  "lastName": "Cohen",
  "idNumber": "123456789",
  "dateOfBirth": "1990-04-15",
  "email": "alice@example.com",
  "phone": "0501234567"
}
```

1. **HTTP layer** — Kestrel receives the request and routes it to `CustomersController.Create`.
2. **Model binding** — ASP.NET Core deserializes the JSON body into a `CreateCustomerRequest` record.
3. **Controller** — `CustomersController.Create` calls `_customerService.CreateAsync(request)`. No business logic here.
4. **Service** — `CustomerService.CreateAsync`:
   - Calls `_customerRepository.IdNumberExistsAsync(...)`. If true, throws `BusinessRuleException`.
   - Constructs a `Customer` entity (new `Guid`, `CreatedAt = UtcNow`).
   - Calls `_customerRepository.AddAsync(customer)`.
   - Maps the entity to `CustomerResponse` and returns it.
5. **Repository** — `CustomerRepository.AddAsync` calls `_db.Customers.Add(customer)` and `SaveChangesAsync()`. EF Core generates the SQL `INSERT`.
6. **Database** — SQLite writes the row.
7. **Response** — Controller wraps the response in `CreatedAtAction(...)` → HTTP 201 with `Location: /api/customers/{id}`.

If `IdNumber` already exists, `BusinessRuleException` bubbles up to the middleware, which writes:
```
HTTP 409 Conflict
{ "error": "A customer with ID number '123456789' already exists." }
```

### Example B: Create a policy (the rich path)

`POST /api/customers/{customerId}/policies` with body:
```json
{
  "type": "Car",
  "startDate": "2026-05-01",
  "endDate": "2027-05-01",
  "premiumAmount": 1500.00
}
```

1. Routed to `PoliciesController.Create(customerId, request)`.
2. Calls `_policyService.CreateAsync(customerId, request)`.
3. **Service runs three checks before persisting**:
   - **Lookup customer** via `ICustomerRepository.GetByIdAsync`. If `null` → `NotFoundException` → HTTP 404.
   - **Rule 3 (age 18+):** computes age from `customer.DateOfBirth` vs today. If `< 18` → `BusinessRuleException` → HTTP 409.
   - **Rule 2 (no past start):** if `request.StartDate < today` → `BusinessRuleException` → HTTP 409.
   - **Rule 1 (no duplicate active):** calls `_policyRepository.HasActiveOfTypeAsync(customerId, request.Type)`. If true → `BusinessRuleException` → HTTP 409.
4. Generates a `PolicyNumber` like `POL-20260428-A1B2C3D4`.
5. Constructs `Policy` with `Status = Active` and saves via `IPolicyRepository.AddAsync`.
6. Returns `PolicyResponse` → HTTP 201.

### Example C: Cancel a policy

`PATCH /api/policies/{id}/cancel`

1. `PoliciesController.Cancel` → `PolicyService.CancelAsync(id)`.
2. Service loads the policy. If missing → 404.
3. If `Status == Cancelled` → 409 (already cancelled).
4. Otherwise sets `Status = Cancelled`, saves via `UpdateAsync`, returns the updated `PolicyResponse`.

### Example D: List policies with filters

`GET /api/policies?type=Car&status=Active`

1. ASP.NET Core binds the query string to nullable `PolicyType?` and `PolicyStatus?` parameters.
2. `PolicyService.GetAllAsync(type, status)` calls `IPolicyRepository.GetAllAsync(type, status)`.
3. Repository builds a `IQueryable<Policy>`, conditionally appending `Where(...)` clauses for non-null filters, then calls `ToListAsync()`. Filtering is pushed down to SQL.
4. Service maps each entity to `PolicyResponse` and returns.

---

## 5. Error Handling Flow

All exceptions surface through `ExceptionMiddleware` (the outermost piece of the pipeline that wraps everything below it):

```
[ Request ]
    ↓
[ ExceptionMiddleware ] ← catches all
    ↓
[ HttpsRedirection ]
    ↓
[ Routing → Controller → Service → Repository → DbContext ]
    ↓
[ Response ]
```

| Exception | HTTP Status | Used For |
|-----------|-------------|----------|
| `NotFoundException` | 404 | Entity ID not in DB. |
| `BusinessRuleException` | 409 | Domain rule violated (duplicate, past date, under 18, already cancelled, duplicate ID). |
| Any other `Exception` | 500 | Unexpected — logged to stderr, generic message to client. |

The middleware writes a small JSON body: `{ "error": "<message>" }`.

---

## 6. Persistence Flow

- **DbContext lifetime** — scoped per request, so a single request = one unit of work.
- **Schema creation** — on startup, `Program.cs` calls `db.Database.EnsureCreated()` so the SQLite file (`insurance.db`) is created with the right tables on first run. For production, swap this for migrations (`dotnet ef migrations add <Name>`).
- **Indexes** — unique on `Customer.IdNumber` and `Policy.PolicyNumber` (defined in entity configurations).
- **Enum storage** — `PolicyType` and `PolicyStatus` are stored as strings (`"Car"`, `"Active"`) for human-readable rows and forward-compatibility on enum reordering.

---

## 7. Testing Flow

`tests/InsuranceApi.UnitTests` validates the **Application layer** in isolation:

- `NSubstitute` creates fake repositories (`Substitute.For<IPolicyRepository>()`).
- The service is constructed with the fakes, the test arranges return values (`_repo.Method(...).Returns(...)`), acts on the service, and asserts the outcome.
- No database, no controllers — pure logic verification.

The 12 tests cover:
- All 3 business rules (positive and negative cases).
- Customer creation with duplicate `IdNumber`.
- `NotFoundException` paths for both Customer and Policy.
- The cancel flow including the "already cancelled" guard.

---

## 8. End-to-End Flow Summary (one diagram)

```
Client (HTTP/JSON)
   │
   ▼
┌──────────────────────────────┐
│ ExceptionMiddleware          │   ← maps exceptions to 4xx/5xx
└──────────────────────────────┘
   │
   ▼
┌──────────────────────────────┐
│ Controller (thin)            │   ← model binding, returns Ok/Created/etc.
└──────────────────────────────┘
   │ DTO
   ▼
┌──────────────────────────────┐
│ Service (Application)        │   ← business rules live here
└──────────────────────────────┘
   │ via interface
   ▼
┌──────────────────────────────┐
│ Repository (Infrastructure)  │   ← only this layer knows EF Core
└──────────────────────────────┘
   │ EF Core
   ▼
┌──────────────────────────────┐
│ SQLite (insurance.db)        │
└──────────────────────────────┘
```

Each arrow crosses a boundary that exists for a reason: the controller doesn't know about EF, the service doesn't know about HTTP, the repository doesn't know about business rules. Every layer can be tested or replaced in isolation.

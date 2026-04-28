# Insurance Management API

A .NET 8 REST API for managing insurance customers and their policies.

## Running locally

Make sure you have the .NET 8 SDK installed.

```bash
cd src/InsuranceApi.Api
dotnet run
```

The app will start on `https://localhost:7xxx` (or `http://localhost:5xxx`). 
You can view the Swagger UI at `/swagger` to test out the endpoints. 

*Note: It uses SQLite by default. The database (insurance.db) is automatically created on the first run.*

To run the tests:
```bash
dotnet test
```

## Architecture

This is built using Clean Architecture. The dependencies point inwards:

- **Domain:** Where the core models (Customer, Policy) and rules live. It has zero external dependencies.
- **Application:** The service layer (e.g., CustomerService, PolicyService) and DTOs.
- **Infrastructure:** EF Core and SQLite repository implementations.
- **Api:** Thin controllers that just pass data to the services.

The nice thing about this setup is that if we ever want to move from SQLite to Postgres, we only need to touch the Infrastructure project. The core business logic remains untouched.

## Endpoints

The API is built around two main entities:

### Customers

We assume the IdNumber (like a passport or national ID) is unique for each customer. Email is optional, but a phone number is required.

| Method | Path | Description |
|--------|------|-------------|
| POST | /api/customers | Create customer |
| GET | /api/customers | List all customers |
| GET | /api/customers/{id} | Get customer by ID |
| PUT | /api/customers/{id} | Update customer |

### Policies

| Method | Path | Description |
|--------|------|-------------|
| POST | /api/customers/{customerId}/policies | Create policy for customer |
| GET | /api/policies?type=Car&status=Active | List policies (optional filters) |
| GET | /api/policies/{id} | Get policy by ID |
| PUT | /api/policies/{id} | Update policy |
| PATCH | /api/policies/{id}/cancel | Cancel policy |

## Core Business Rules

We enforce a few key business rules:
- **No duplicate active policies:** A customer can't have two active policies of the same type (Car, Health, Life, Home).
- **Premium limits:** Each policy type has an allowed premium range (defined in PolicyTypeRules.cs).
- **Duration limits:** Valid durations depend on the policy type. For example, Life insurance is multi-year, while Car insurance is shorter.

*A quick note on policy expiration: There's no background job running to auto-expire policies right now. They'll stay Active until you explicitly update or cancel them.*

## Future Improvements

If we were to scale this up for production traffic (e.g., 1M+ policies), here are a few things we'd tackle next:

- **Database:** Swap out SQLite for PostgreSQL.
- **Indexing:** Add a composite index on (CustomerId, Type, Status) to optimize the duplicate policy check.
- **Caching:** Add Redis to cache frequent customer reads and invalidate it on updates.
- **Background Jobs:** Bring in something like Hangfire to automatically flip policies from Active to Expired every night.
- **Pagination:** Add cursor or offset pagination to the list endpoints before the tables get too large.

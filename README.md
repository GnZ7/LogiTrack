# LogiTrack

LogiTrack is an ASP.NET Core Web API for inventory and order management. It
provides a small REST API for registering users, authenticating with JWT bearer
tokens, managing inventory, and placing orders that reserve available stock.

The project targets **.NET 10** and uses **ASP.NET Core Minimal APIs**,
**Entity Framework Core**, **SQLite**, and **ASP.NET Core Identity**. This
repository is a learning/project implementation; review the deployment notes
before using it as a production service.

## Contents

- [Features](#features)
- [Architecture](#architecture)
- [Design and implementation patterns](#design-and-implementation-patterns)
- [Prerequisites and local setup](#prerequisites-and-local-setup)
- [Run the API](#run-the-api)
- [Authentication and authorization](#authentication-and-authorization)
- [API reference](#api-reference)
- [Try the API with Swagger](#try-the-api-with-swagger)
- [Try the API with the HTTP request file](#try-the-api-with-the-http-request-file)
- [Build and tests](#build-and-tests)
- [Database and migrations](#database-and-migrations)
- [Configuration and deployment notes](#configuration-and-deployment-notes)

## Features

- Register operator accounts and issue signed JWT access tokens.
- Protect inventory and order endpoints with role-based authorization.
- Create, retrieve, list, update, and delete inventory items.
- Paginate and sort inventory results by item name.
- Cache each inventory list page in process memory for 30 seconds; cache
  hit/miss events are logged.
- Place orders using inventory item IDs and quantities.
- Check and decrement stock as part of order creation; reject requests when
  stock is insufficient.
- Update orders and their item reservations transactionally.
- Retrieve orders with the item details captured when the order was placed.
- Delete orders and unreferenced inventory items as an administrator.
- Generate an OpenAPI document and Swagger UI for Development.

## Architecture

The solution contains one ASP.NET Core Web API project:

```text
LogiTrack.slnx
└── LogiTrack/
    ├── Program.cs
    ├── Data/
    │   ├── LogiTrackContext.cs
    │   ├── Configurations/
    │   ├── Migrations/
    │   └── Seeding/
    ├── Features/
    │   ├── Auth/
    │   ├── Items/
    │   └── Orders/
    ├── Models/
    └── Shared/
        ├── ErrorHandling/
        └── OpenApi/
```

- **`Program.cs`** configures dependency injection, SQLite, Identity, JWT
  authentication, authorization policies, validation, memory caching, and the
  HTTP pipeline. It maps each feature's endpoints.
- **`Features/`** groups endpoint handlers and their request/response DTOs by
  capability. Authentication, items, and orders are kept in separate feature
  areas.
- **`Data/`** contains the EF Core `DbContext`, entity configuration,
  migrations, and startup data/identity seeding.
- **`Models/`** contains the persistence/domain entities: `Item`, `Order`, and
  `OrderItem`.
- **`Shared/`** contains cross-cutting exception handling and OpenAPI/Swagger
  setup.

The SQLite database is configured at
`LogiTrack/Data/Db/logitrack.db`, relative to the API project's content root.
The relationship between orders and order items cascades when an order is
deleted. An order item also references its inventory item, and inventory items
referenced by orders cannot be deleted. Order items retain a copy of the
item's name and location as order-line details.

## Design and implementation patterns

- **Feature-oriented organization (vertical slices):** related endpoint
  mappings, handlers, and contracts live together under `Features/Auth`,
  `Features/Items`, and `Features/Orders`.
- **Minimal API endpoint mapping:** static feature classes expose mapping
  extension methods. `Program.cs` composes those feature routes rather than
  placing all handler logic in the startup file.
- **Dependency injection:** handlers receive `LogiTrackContext`, Identity
  services, cache, logger, and cancellation tokens through ASP.NET Core
  dependency injection.
- **Request/response DTOs:** API contracts are separated from EF Core
  entities. Data annotations and explicit checks validate request values.
- **Factory methods:** `Item.Create`, `Order.Create`, and `OrderItem.Create`
  centralize creation and initialization of domain objects.
- **EF Core entity configuration and migrations:** mappings are defined in
  `Data/Configurations` and schema changes are tracked in migrations.
- **Read/write query choices:** read-only entity queries use
  `AsNoTracking()`. The order read uses `Include()` to load its order items
  with one query. Deletion queries remain tracked because the entities are
  removed and saved.
- **Transactional stock reservation:** order creation uses a database
  transaction and a conditional update that only decrements stock when enough
  quantity is available. Order updates restore the previous reservations and
  reserve the replacement lines in the same transaction. If the requested
  quantity cannot be reserved, the transaction is rolled back and the API
  returns a conflict.
- **Cache-aside for inventory pages:** `GET /api/items` caches each page and
  page-size combination for 30 seconds using `IMemoryCache`. It is an
  in-process cache, so entries are not shared across instances and can be
  briefly stale after inventory/order changes.
- **Identity and policy-based authorization:** ASP.NET Core Identity stores
  users and roles; JWT claims carry roles used by named authorization
  policies.
- **Problem Details for errors:** the application registers a global exception
  handler and Problem Details services.

The API uses EF Core directly through `LogiTrackContext`; it does not
implement a separate repository layer.

## Prerequisites and local setup

- .NET 10 SDK
- Git
- An HTTP client for manual testing:
  - Swagger UI is included for Development.
  - To run `LogiTrack/LogiTrack.http` inside VS Code, install the **REST
    Client** extension.

Clone the repository and move into the API project directory:

```powershell
git clone https://github.com/GnZ7/LogiTrack.git
cd .\LogiTrack\LogiTrack
```

The solution file is one level above the project directory. Restore and build
from the repository root with:

```powershell
cd ..
dotnet restore .\LogiTrack.slnx
dotnet build .\LogiTrack.slnx --no-restore
```

### Configure local secrets

The API requires `Authentication:Jwt:Key`. It must contain at least 32 UTF-8
bytes. Generate a local random key and store it with .NET User Secrets (run
these commands from the `LogiTrack` project directory):

```powershell
cd .\LogiTrack
$jwtKey = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
dotnet user-secrets set "Authentication:Jwt:Key" $jwtKey
```

User Secrets are stored in your local user profile, not in this repository.
Do not commit signing keys, passwords, or real access tokens.

Administrator provisioning is optional. Configure both values to create an
administrator at startup; configure neither to skip administrator creation.
The password must satisfy the Identity password policy described below.

```powershell
dotnet user-secrets set "Authentication:Admin:Email" "admin@example.com"
dotnet user-secrets set "Authentication:Admin:Password" "<your-local-admin-password>"
```

If only one administrator setting is configured, startup fails. Public
registration creates only operator accounts and cannot grant administrator
privileges.

## Run the API

From the `LogiTrack` project directory:

```powershell
dotnet restore
dotnet run --launch-profile https
```

The HTTPS launch profile uses `https://localhost:7123` and also listens on
`http://localhost:5025`. If your machine does not trust the local HTTPS
development certificate, create and trust one with:

```powershell
dotnet dev-certs https --trust
```

In the `Development` environment, startup applies pending EF Core migrations,
seeds sample inventory if the inventory table is empty, and exposes Swagger UI
at `https://localhost:7123/`. The API creates the SQLite database directory
if it does not exist.

## Authentication and authorization

### Roles

- **Operator:** created by public registration. Can use inventory and order
  endpoints.
- **Administrator:** provisioned only when both administrator User Secrets
  are configured. Can use operator endpoints and delete orders/items.

The API creates the `Operator` and `Administrator` roles at startup. The
password policy requires at least 8 characters, including an uppercase letter,
a lowercase letter, a digit, and a non-alphanumeric character. Failed login
attempts use Identity lockout settings (five failed attempts and a 15-minute
lockout).

### Login flow

1. Register an account with `POST /api/auth/register`, or use the configured
   administrator account.
2. Log in with `POST /api/auth/login`.
3. Use the `accessToken` from the response as a bearer token:
   `Authorization: Bearer <accessToken>`.
4. The token is valid for 60 minutes and uses the configured JWT issuer,
   audience, and signing key.

Registration and login are anonymous endpoints. All item and order endpoints
require an authenticated `Operator` or `Administrator`. Delete endpoints
require `Administrator`.

## API reference

Unless stated otherwise, protected routes require
`Authorization: Bearer <accessToken>`.

| Method | Route | Access | Description | Typical responses |
|---|---|---|---|---|
| `POST` | `/api/auth/register` | Anonymous | Register an operator account. | `200`, `400` validation problem |
| `POST` | `/api/auth/login` | Anonymous | Authenticate and return a JWT. | `200`, `400`, `401` |
| `GET` | `/api/items?pageNumber=1&pageSize=10` | Operator, Administrator | List inventory ordered by name. Both query parameters must be at least 1. | `200`, `400` validation problem |
| `GET` | `/api/items/{id}` | Operator, Administrator | Get one item by GUID. | `200`, `404` |
| `POST` | `/api/items` | Operator, Administrator | Create an inventory item. Name is required (max 120), quantity must be non-negative, and location is required (max 200). | `201`, `400` validation problem |
| `PUT` | `/api/items/{id}` | Operator, Administrator | Replace an inventory item's name, available quantity, and location. The same field limits as item creation apply. | `200`, `400` validation problem, `404` |
| `DELETE` | `/api/items/{id}` | Administrator | Delete an item if no order references it. | `204`, `404`, `409` if referenced |
| `POST` | `/api/orders` | Operator, Administrator | Create an order and reserve inventory. Customer name is required (max 120), at least one item is required, and quantities must be positive. Item IDs must exist and be unique within the request. | `201`, `400` validation problem, `409` insufficient inventory |
| `GET` | `/api/orders/{id}` | Operator, Administrator | Get an order, including its order items. | `200`, `404` |
| `PUT` | `/api/orders/{id}` | Operator, Administrator | Replace the customer name and complete set of order lines. Existing reservations are restored before the new quantities are reserved, atomically. | `200`, `400` validation problem, `404`, `409` insufficient inventory |
| `DELETE` | `/api/orders/{id}` | Administrator | Delete an order. | `204`, `404` |

`PUT /api/items/{id}` sets `quantity` to the new available stock value; it does
not adjust existing order lines. `PUT /api/orders/{id}` replaces all order
lines, requires at least one line, and does not change the order ID or original
placement date. Each requested item ID must be unique, exist in inventory, and
have sufficient available stock after the order's previous reservations are
returned. A failed reservation returns `409 Conflict` and leaves the original
order and stock unchanged.

Example full-update request bodies:

```json
{
  "name": "Packing boxes",
  "quantity": 12,
  "location": "Warehouse B"
}
```

```json
{
  "customerName": "Acme Logistics",
  "items": [
    {
      "itemId": "replace-with-an-inventory-item-guid",
      "quantity": 3
    }
  ]
}
```

`GET /api/items` returns an `Items` collection and `TotalPages`. Each
page/page-size combination is cached for up to 30 seconds. When testing
inventory changes, a list response can therefore reflect the previously
cached values until that entry expires.

## Try the API with Swagger

1. Start the API in Development with `dotnet run --launch-profile https`.
2. Open `https://localhost:7123/`.
3. Expand `POST /api/auth/register`, enter an email and a password that meets
   the password policy, and execute it. Registration returns `200` on success.
   Skip this step if you are testing with the configured administrator.
4. Expand `POST /api/auth/login`, submit those credentials, and execute it.
   Copy the `accessToken` from the response.
5. Select **Authorize** in Swagger UI and provide the token for the HTTP Bearer
   scheme. Swagger will send it as a bearer authorization header.
6. Exercise a normal workflow:
   - `POST /api/items` to create an item.
   - `GET /api/items` or `GET /api/items/{id}` to inspect inventory.
   - `PUT /api/items/{id}` to replace the item's name, available quantity, and
     location.
   - `POST /api/orders` using the created item's `itemId`.
   - `PUT /api/orders/{id}` using the returned `orderId` to replace the
     customer name or reserved order lines.
   - `GET /api/orders/{id}` to verify the updated order.
7. To test administrator-only deletion, log in with the configured
   administrator account, authorize with that token, delete the order, then
   delete the now-unreferenced item. An operator token receives `403` for
   delete endpoints.

Swagger UI and the OpenAPI document are enabled only in Development.

## Try the API with the HTTP request file

The request collection is [`LogiTrack/LogiTrack.http`](./LogiTrack/LogiTrack.http).
Open it in VS Code with the REST Client extension and keep the API running
using the HTTPS profile.

1. Send **Register an operator account** once. If that email already exists,
   use a different email in the request or log in with the existing account.
2. Send **Log in**. REST Client stores the named response as `login`; the
   subsequent requests read its `accessToken` automatically.
3. Send requests in order. The order and item IDs are interpolated from
   previous named responses.
4. To run administrator-only requests, configure admin User Secrets, restart
   the API if needed, replace the example credentials in **Log in as
   administrator** with the configured credentials, and send that request.
   The deletion requests use that response's token.
5. To observe list caching, send **List inventory** twice within 30 seconds
   and inspect the API logs for the cache hit/miss entries.

The example credentials in the `.http` file are local test values only. Replace
them for your own environment; never use them in a deployed environment.

## Build and tests

Build the solution from the repository root:

```powershell
dotnet build .\LogiTrack.slnx
```

There is no automated test project in the solution currently. Use Swagger UI
or the `.http` request collection for manual endpoint testing.

## Database and migrations

The application uses SQLite at `LogiTrack/Data/Db/logitrack.db`. In
Development, it applies EF Core migrations automatically at startup and seeds
one sample inventory item only when the inventory table is empty. Identity
roles are created at startup in every environment; the configured administrator
account is also provisioned when both administrator settings are present.

For deployments outside Development, migrations are **not** applied
automatically by this application. Apply reviewed schema migrations as an
explicit deployment step before starting the new application version. Ensure
the application has the required access to its database file and directory.

## Configuration and deployment notes

- Store `Authentication:Jwt:Key`, administrator credentials, and any
  environment-specific settings in a secure configuration provider (for
  example, a deployment secret store), not in source control or committed
  `appsettings` files.
- Use a unique, high-entropy signing key per environment and serve the API
  over HTTPS.
- Swagger/OpenAPI is only mapped in Development; exception handling uses the
  production exception handler outside Development.
- SQLite is suitable for local development and smaller deployments. Plan
  database persistence, backups, locking/concurrency behavior, and migration
  operations for the expected workload before production use.
- `IMemoryCache` is local to one application process. In a multi-instance
  deployment, instances do not share entries. Inventory list values may be
  stale for up to 30 seconds after a mutation.
- Startup seeding expects a reachable, migrated database because Identity
  roles are ensured at startup, including outside Development.

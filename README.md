# LogiTrack

LogiTrack is an ASP.NET Core Web API targeting .NET 10. The API uses SQLite,
ASP.NET Core Identity, JWT bearer authentication, and role-based authorization.

## Local development

### Prerequisites

- .NET 10 SDK

### Configure authentication secrets

The API requires a JWT signing key of at least 32 bytes. Administrator
credentials are optional; without them, no administrator account is created.
Use .NET User Secrets to keep local credentials out of source control.

From the repository root, run:

```powershell
cd .\LogiTrack
dotnet user-secrets set "Authentication:Jwt:Key" "LogiTrack-Local-Only-Development-Key-Not-For-Production-2026!"
dotnet user-secrets set "Authentication:Admin:Email" "admin@example.com"
dotnet user-secrets set "Authentication:Admin:Password" "<choose a strong development-only password>"
```

The JWT key above is a public example for local development only. Replace it
with a random secret for your own environment, and never use the example key or
local admin password outside local development.

The project has a `UserSecretsId` configured, so `dotnet user-secrets init` is
not needed. User Secrets are stored in your local user profile, not in this
repository.

### Run the API

From the `LogiTrack` project directory:

```powershell
dotnet restore
dotnet run --launch-profile https
```

In Development, the API applies pending database migrations and serves its
OpenAPI UI at the application root. The HTTPS profile listens at
`https://localhost:7123`; the `.http` examples use this URL to avoid an HTTP
redirect stripping the bearer Authorization header.

## Authentication and authorization

- `POST /api/auth/register` creates an `Operator` account. Public registration
  does not accept a role, so it cannot be used to create administrators.
- `POST /api/auth/login` returns a JWT bearer access token.
- Item and order endpoints require the `Operator` or `Administrator` role.
- Delete endpoints require the `Administrator` role.
- The configured administrator is provisioned at startup using
  `Authentication:Admin:Email` and `Authentication:Admin:Password`.

The `LogiTrack/LogiTrack.http` file contains sample requests for registering,
logging in, and calling the API.

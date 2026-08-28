# Room Booking

Meeting-room booking assistant built with .NET 8 and PostgreSQL. The backend is
organized as a modular monolith: Domain owns the booking rules, Application
orchestrates use cases, Infrastructure implements persistence and authentication,
and Api exposes the HTTP boundary.

## Prerequisites

- .NET SDK 8.0.401 or a compatible later patch release. The expected version is
  defined in [global.json](global.json).
- Docker Desktop or another Docker-compatible runtime.

## Run locally

Start PostgreSQL:

```powershell
docker compose up -d
```

Store the development connection string outside the committed configuration:

```powershell
dotnet user-secrets set --project src/RoomBooking.Api "ConnectionStrings:RoomBooking" "Host=localhost;Port=5432;Database=room_booking;Username=room_booking;Password=room_booking"
```

Restore packages. Generate migrations manually whenever the persistence model
changes, then apply them and run the API:

```powershell
dotnet restore RoomBooking.sln --locked-mode
dotnet ef migrations add InitialBookingSchema --project src/RoomBooking.Infrastructure --startup-project src/RoomBooking.Api --output-dir Persistence/Migrations
dotnet ef database update --project src/RoomBooking.Infrastructure --startup-project src/RoomBooking.Api
dotnet run --project src/RoomBooking.Api
```

The API starts at `http://localhost:5226` and exposes `GET /health`. Sample
requests are available in `src/RoomBooking.Api/RoomBooking.Api.http`.

## Booking behavior

The five rooms are seeded as system data with deterministic capacities:

| Room | Capacity |
| --- | ---: |
| A | 4 |
| B | 6 |
| C | 8 |
| D | 10 |
| E | 12 |

The capacities were not specified by the challenge, so they are explicit,
replaceable assumptions isolated in the persistence seed. Bookings use half-open
time ranges (`[start, end)`), 30-minute boundaries, and a maximum duration of
three hours. Application validation gives useful errors, while a PostgreSQL
exclusion constraint provides the final defense against concurrent double
bookings.

Authenticated endpoints support listing rooms, checking availability, viewing a
room schedule, creating a booking, listing the current user's upcoming bookings,
and cancelling a booking owned by that user. Dates exchanged by the API are UTC;
the configured business timezone is `America/Montevideo`.

## Authentication

The challenge users are `User1` and `User2`; both use the password supplied in
the challenge. Only salted password hashes are committed, and every value under
`Authentication:Users` can be overridden with user secrets or environment
variables.

Authentication uses an HttpOnly same-origin cookie. The available endpoints are:

- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET /api/auth/csrf`
- `POST /api/auth/logout`

Authenticated state-changing requests require the token returned by
`GET /api/auth/csrf` in the `X-CSRF-TOKEN` header. The browser retains the
authentication and antiforgery cookies returned by the API.

## Verify the solution

```powershell
dotnet build RoomBooking.sln --configuration Release --no-restore
dotnet test RoomBooking.sln --configuration Release --no-build --no-restore
```

PostgreSQL integration tests use Testcontainers and therefore require Docker.
When Docker is unavailable locally they are reported as inconclusive; in CI they
remain mandatory and fail if PostgreSQL cannot be started. The GitHub Actions
workflow runs restore, build, and test for pull requests and pushes targeting
`main`.

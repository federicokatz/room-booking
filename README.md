# Room Booking

Room Booking is a meeting-room booking assistant built for the Promtior technical challenge. It uses .NET 8 and PostgreSQL and exposes a conversational API that manages room bookings through controlled LLM tool calling.

The assistant can only work with room bookings. It interprets a user's message, requests a typed tool when needed, and the tool delegates to the same Application use cases used by the HTTP booking endpoints. The model never accesses PostgreSQL, decides authorization, or receives a user identifier.

## Architecture

The backend is a modular monolith:

```text
Api → Application → Domain
          ↑
   Infrastructure
```

- **Domain** contains booking and room rules.
- **Application** contains use cases, chat orchestration, tool contracts, and ports.
- **Infrastructure** implements PostgreSQL, authentication credential verification, and Groq integration.
- **Api** exposes HTTP endpoints, authentication, and antiforgery protection.

See the [component diagram](doc/component-diagram.md) and [architecture decisions](doc/architecture-decisions.md) for the complete request flow.

## Prerequisites

- .NET SDK 8.0.401 or a compatible later patch release. The expected version is defined in [global.json](global.json).
- Docker Desktop or another Docker-compatible runtime for PostgreSQL and integration tests.
- A Groq API key to run the conversational assistant. Tests do not need a key.

## Run locally

Start PostgreSQL:

```powershell
docker compose up -d
```

Store local secrets outside committed configuration:

```powershell
dotnet user-secrets set --project src/RoomBooking.Api "ConnectionStrings:RoomBooking" "Host=localhost;Port=5432;Database=room_booking;Username=room_booking;Password=room_booking"
dotnet user-secrets set --project src/RoomBooking.Api "AI:ApiKey" "YOUR_GROQ_API_KEY"
```

The default provider configuration is committed because it contains no secret:

```text
AI__Endpoint=https://api.groq.com/openai/v1
AI__Model=openai/gpt-oss-20b
```

`AI__ApiKey` is required only when using the chat endpoint. In a deployed environment, set all `AI__...` values as environment variables.

Restore, apply the existing migrations, and run the API:

```powershell
dotnet restore RoomBooking.sln --locked-mode
dotnet ef database update --project src/RoomBooking.Infrastructure --startup-project src/RoomBooking.Api
dotnet run --project src/RoomBooking.Api
```

The API starts at `http://localhost:5226` and exposes `GET /health`. Generate a new migration manually only when the persistence model changes:

```powershell
dotnet ef migrations add <MigrationName> --project src/RoomBooking.Infrastructure --startup-project src/RoomBooking.Api --output-dir Persistence/Migrations
```

### React client

In a second terminal, start the React client after the API is running:

```powershell
cd frontend/RoomBooking.Web
npm install
npm run dev
```

Vite serves the client at `http://localhost:5173` and proxies `/api` requests to the local ASP.NET Core API. This preserves the same-origin cookie and antiforgery flow during development. The browser UI displays rooms and the current user's bookings, while all booking creation and cancellation continue to happen through the conversational assistant.

## Booking behavior

The five rooms are seeded as system data with deterministic capacities:

| Room | Capacity |
| --- | ---: |
| A | 4 |
| B | 6 |
| C | 8 |
| D | 10 |
| E | 12 |

The challenge does not specify capacities, so these are documented, replaceable assumptions isolated in the persistence seed.

Bookings use half-open ranges (`[start, end)`), 30-minute boundaries, and a maximum duration of three hours. Application validation returns useful errors, while a PostgreSQL exclusion constraint is the final defense against concurrent double bookings.

Dates exchanged by the API are UTC. Natural-language dates are interpreted using the configurable business timezone, which defaults to `America/Montevideo`.

## Authentication and CSRF

The challenge users are `User1` and `User2`; both use the password supplied in the challenge. Password hashes are committed instead of plaintext credentials.

Authentication uses an HttpOnly same-origin cookie with `SameSite=Lax`; it is marked `Secure` outside Development. API authentication failures return `401` or `403`, never an HTML redirect.

Authentication endpoints:

- `POST /api/auth/login`
- `GET /api/auth/me`
- `GET /api/auth/csrf`
- `POST /api/auth/logout`

Authenticated requests that change state require the token from `GET /api/auth/csrf` in the `X-CSRF-TOKEN` header. This includes booking, logout, and chat-session mutation endpoints.

## Conversational assistant

Groq is used through its OpenAI-compatible Chat Completions API. The provider is isolated behind the Application `IChatModel` contract and receives a typed set of five tools:

| Tool | Purpose |
| --- | --- |
| `create_booking` | Creates a booking for the authenticated user. |
| `list_available_rooms` | Lists rooms available for a requested range and attendee count. |
| `get_room_schedule` | Returns occupied and free slots for a room. |
| `list_my_bookings` | Lists upcoming bookings for the authenticated user. |
| `cancel_booking` | Cancels a booking owned by the authenticated user. |

The model may request a tool, but every tool delegates to an Application use case. Tools do not access EF Core or PostgreSQL directly and do not accept `userId` or `ownerId` arguments. `ICurrentUser` supplies the identity from the authenticated server request.

Each chat session is kept in memory, belongs to one authenticated user, expires after 30 minutes of inactivity, and retains at most 20 history messages. A request is bounded to five model iterations and five total tool calls. Tool calls run sequentially; an incomplete tool-call block is never persisted in conversation history.

Chat endpoints:

- `POST /api/chat/sessions`
- `POST /api/chat/sessions/{sessionId}/messages`
- `DELETE /api/chat/sessions/{sessionId}`

The message endpoint returns an assistant message and, when appropriate, an `effects` collection such as `booking_created` or `booking_cancelled`. It deliberately does not expose internal tool results as an HTTP contract.

## Verify the solution

```powershell
dotnet build RoomBooking.sln --configuration Release --no-restore
dotnet test RoomBooking.sln --configuration Release --no-build --no-restore

cd frontend/RoomBooking.Web
npm test
npm run build
```

Backend tests use MSTest and FluentAssertions; frontend tests use Vitest and React Testing Library. The agent tests use a deterministic `FakeChatModel`, so CI never calls Groq, requires an API key, or depends on Internet access.

PostgreSQL integration tests use Testcontainers and therefore require Docker. When Docker is unavailable locally they are reported as inconclusive; in CI they remain mandatory and fail if PostgreSQL cannot be started. The GitHub Actions workflow restores locked .NET and npm dependencies, builds both applications, and runs all tests for pull requests and pushes targeting `main`.

## Documentation

- [Implementation notes](doc/implementation-notes.md)
- [Architecture decisions](doc/architecture-decisions.md)
- [Component diagram](doc/component-diagram.md)
- [Technical notebook](doc/technical-challenge-notebook.ipynb)

# Room Booking

Meeting-room booking assistant built for the Promtior technical challenge. Authenticated users manage room bookings through a conversational interface with controlled LLM tool calling.

## What it does

- Authenticates User1 and User2 using the credentials defined by the challenge.
- Lists available rooms and room schedules.
- Creates and cancels bookings through chat.
- Enforces capacity, 30-minute slots, a three-hour maximum duration, ownership, and no-overlap rules.

## Stack

- React + TypeScript + Vite
- .NET 8 / ASP.NET Core minimal API
- PostgreSQL + EF Core
- Groq OpenAI-compatible tool calling
- Docker, MSTest, FluentAssertions, Testcontainers, and Vitest

## Architecture

The backend is a modular monolith:

```text
React → API → Application → Domain
                  ↑
           Infrastructure
             ↙          ↘
       PostgreSQL       Groq
```

The LLM interprets user language, but it does not access the database, decide booking rules, or receive a user identifier.

### Booking flow

```text
User → React → POST /api/chat/.../messages → ChatAgentService
     → LLM → create_booking tool → CreateBookingUseCase → PostgreSQL
     → tool result → LLM → assistant response → React
```

See the complete [project overview](doc/project-overview.md) and [component diagram](doc/component-diagram.md) for the full flow and technical decisions.

## Run locally

Prerequisites: .NET SDK 8, Node.js 24, and Docker Desktop.

```powershell
docker compose up -d

dotnet user-secrets set --project src/RoomBooking.Api "ConnectionStrings:RoomBooking" "Host=localhost;Port=5432;Database=room_booking;Username=room_booking;Password=room_booking"
dotnet user-secrets set --project src/RoomBooking.Api "AI:ApiKey" "YOUR_GROQ_API_KEY"

dotnet restore RoomBooking.sln --locked-mode
dotnet ef database update --project src/RoomBooking.Infrastructure --startup-project src/RoomBooking.Api
dotnet run --project src/RoomBooking.Api
```

In a second terminal:

```powershell
cd frontend/RoomBooking.Web
npm ci
npm run dev
```

The React development server runs at `http://localhost:5173` and proxies API calls to ASP.NET Core at `http://localhost:5226`. If your local PostgreSQL port differs, update the connection string accordingly. `.env.example` lists local Compose and deployment variable names; it never contains secrets.

## Verify

```powershell
dotnet test RoomBooking.sln --configuration Release --no-restore

cd frontend/RoomBooking.Web
npm test
npm run build

cd ../..
docker build --tag room-booking:local .
```

## Deploy

The root [Dockerfile](Dockerfile) builds React and serves it from ASP.NET Core, so production is one application service plus PostgreSQL. For Railway, configure `ConnectionStrings__RoomBooking` and `AI__ApiKey` in the platform variable store, apply EF Core migrations manually, and deploy the root Dockerfile. Migrations never run automatically at startup.

## Documentation

The required challenge documentation is in `/doc`:

- [Project overview](doc/project-overview.md)
- [Component diagram](doc/component-diagram.md)
- [Technical notebook](doc/technical-challenge-notebook.ipynb)

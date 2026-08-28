# Project overview

## Purpose

This project solves the Promtior challenge with a meeting-room booking assistant. Users sign in as `User1` or `User2` and use chat to find rooms, view a room schedule, create bookings, list their bookings, and cancel their own bookings.

The solution treats booking as the source of truth and chat as an interaction channel. The model understands the request and chooses a tool, and the application decides whether the operation is allowed.

## Solution in brief

- **Frontend:** React and TypeScript.
- **Backend:** .NET 8 minimal API, organized as Domain, Application, Infrastructure, and API projects.
- **Database:** PostgreSQL with EF Core.
- **Assistant:** Groq through its OpenAI-compatible tool-calling API.
- **Authentication:** HttpOnly same-origin cookie with antiforgery protection.
- **Deployment:** one Docker image that serves both React and the API.

The browser sends a message to the API. The API resolves the authenticated user, asks the model for a response or a tool call, and executes each requested tool through an existing use case. The tool never accesses PostgreSQL directly and never receives a user identifier. PostgreSQL persists the final result.

## Agent tools

The assistant exposes five tools:

| Tool | Type | Purpose |
| --- | --- | --- |
| `create_booking` | Mutation | Creates a booking for the authenticated user. |
| `list_available_rooms` | Read | Finds rooms available for a time range and attendee count. |
| `get_room_schedule` | Read | Returns occupied and available 30-minute slots for a room. |
| `list_my_bookings` | Read | Lists the authenticated user's upcoming bookings. |
| `cancel_booking` | Mutation | Cancels a booking owned by the authenticated user. |

All tools delegate to Application use cases. No tool accesses the database directly.

## Booking rules

- Five fixed rooms: A, B, C, D, and E.
- Room capacities: 4, 6, 8, 10, and 12 attendees. The challenge does not specify room capacities, so concrete capacities were defined as replaceable seed data rather than hard-coded into the domain rules.
- Start and end times must be 30-minute boundaries.
- A booking can last up to three hours.
- A room cannot have overlapping active bookings.
- A user can cancel only their own booking.

Bookings use half-open periods: a booking ending at 11:30 does not conflict with one starting at 11:30. The application checks availability to return a clear error. PostgreSQL also uses an exclusion constraint, so two simultaneous requests cannot create a double booking.

## Security boundaries

Authentication and authorization remain server-side.

- The authenticated user is resolved from the HTTP request.
- The LLM never receives `userId` or `ownerId`.
- Mutation tools do not accept user identity as an argument.
- The Application layer validates cancellation ownership.
- Chat sessions belong to the authenticated user.
- State-changing API requests require an antiforgery token.
- Tool results are not exposed directly as the HTTP API contract.

## Development approach

1. Created the .NET solution, tests, locked dependencies, and CI.
2. Modeled rooms, booking periods, validation rules, and stable business errors.
3. Added cookie authentication, `ICurrentUser`, and CSRF protection.
4. Added PostgreSQL persistence, room seed data, booking use cases, and concurrency protection.
5. Added typed LLM tools and a bounded chat agent that reuses the same booking use cases.
6. Added the React workspace for login, rooms, personal bookings, and chat.
7. Packaged the frontend and API in Docker and documented Railway deployment.

## Main decisions and challenges

| Topic | Decision |
| --- | --- |
| LLM authority | The model requests tools but does not implement rules, access the database, or choose the user. |
| User identity | `ICurrentUser` reads the authenticated server request. Tools do not accept an owner or user argument. |
| Time zone | The configurable business zone defaults to `America/Montevideo`. For time-based tools, the model provides business-local times without an offset. The server converts them to UTC before invoking the Application use case. Persistence remains UTC-based. |
| Concurrency | Application validation gives friendly feedback. PostgreSQL provides the final no-overlap guarantee. |
| Testing | Unit, integration, API, agent, and frontend tests cover the important boundaries. A fake chat model keeps CI deterministic and does not call Groq. |
| Deployment | React is built into the API image, keeping one origin for the cookie and antiforgery flow. Migrations are applied manually, never at startup. |

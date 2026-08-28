# Architecture decisions

## Modular monolith

The application is one deployable ASP.NET Core process with explicit module boundaries:

```text
Api ──────────────► Application ─────► Domain
 │                       ▲
 └────► Infrastructure ──┘
              │
              └──────────────────────► Domain
```

- **Domain** owns room and booking state, value objects, invariants, and stable errors.
- **Application** owns booking use cases, chat orchestration, tool contracts, and ports.
- **Infrastructure** implements PostgreSQL, credential verification, time-zone configuration, and the Groq model adapter.
- **Api** is the composition root and HTTP boundary.

This keeps business rules independent without introducing services, message brokers, or distributed transactions that the challenge does not need.

## Booking consistency

Bookings use half-open intervals `[start, end)`. Two periods overlap when:

```text
first.start < second.end && second.start < first.end
```

This permits an appointment ending at 11:30 and another starting at 11:30. Domain validation requires UTC timestamps, exact 30-minute boundaries, positive duration, and a maximum duration of three hours.

The application performs a pre-check to return a friendly conflict response. PostgreSQL then applies an exclusion constraint over active bookings as the concurrency-safe guarantee: two concurrent requests cannot double-book the same room and time range.

Cancellation is a state transition from `Active` to `Cancelled`, not a delete. It preserves audit history and lets a cancelled period become available again.

## Assumptions kept configurable

The challenge does not define room capacities or a business timezone. The application uses these isolated defaults:

| Room | Capacity |
| --- | ---: |
| A | 4 |
| B | 6 |
| C | 8 |
| D | 10 |
| E | 12 |

The business timezone defaults to `America/Montevideo`. Persisted instants remain UTC; the timezone is used only to interpret natural-language dates in the assistant.

## Authentication and request identity

React and ASP.NET Core are intended to use the same origin, so the API uses an encrypted HttpOnly cookie rather than a bearer token. The cookie uses `SameSite=Lax` and is always `Secure` outside Development. API failures return `401` or `403`, not login redirects.

`ICurrentUser` exposes the authenticated request identity to Application. Clients and LLM tools never supply a booking owner. State-changing authenticated endpoints require an antiforgery token in `X-CSRF-TOKEN`.

## LLM authority boundary

The model interprets language and requests typed tools. It does not contain booking rules, authorize actions, access EF Core, or access PostgreSQL.

```text
User message → ChatAgentService → typed tool → Application use case → Domain → PostgreSQL
```

The available tools are limited to create booking, list available rooms, get a room schedule, list the current user's bookings, and cancel a booking. They use the same use cases as HTTP endpoints, so every tool call crosses the same validation, authorization, and persistence boundaries.

Groq is used through an OpenAI-compatible adapter in Infrastructure. Application depends only on its own `IChatModel` messages, tool definitions, tool calls, and responses. The adapter uses `IHttpClientFactory`; provider API keys remain outside the repository.

Conversation state is intentionally in memory for this challenge. Sessions are owner-scoped, opaque, expire after inactivity, retain limited history, and bound each request to five model iterations and five tool calls. Tool-call history is written atomically so the model never receives an assistant tool call without its matching results.

## Testing strategy

Business invariants are covered by Domain and Application tests. PostgreSQL integration tests verify persistence, the exclusion constraint, authorization boundaries, and cancellation behavior with a real container. Agent tests use a fake chat model and cover tools, malformed model responses, provider failures, session ownership, bounded execution, and tool-call history integrity.

No test or CI workflow calls Groq.

# Architecture decisions

This document records decisions introduced with the core-domain foundation. It is intentionally concise and will evolve alongside the implementation.

## Modular monolith

The application is one deployable ASP.NET Core process with explicit module boundaries:

```text
Api ──────────────► Application ─────► Domain
 │                       ▲
 └────► Infrastructure ──┘
              │
              └──────────────────────► Domain
```

- **Domain** owns room and booking state, value objects, invariants, and stable domain errors. It has no framework or solution-project dependency.
- **Application** will own use cases and ports. It depends only on Domain and framework abstractions required for composition.
- **Infrastructure** implements Application ports for PostgreSQL, authentication credentials, and the LLM provider.
- **Api** is the composition root and HTTP host. It maps transport concerns to Application use cases.

This structure keeps business rules independent without introducing services, message brokers, or distributed transactions that the challenge does not need.

## Booking intervals

Bookings use half-open intervals `[start, end)`. Two periods overlap when:

```text
first.start < second.end && second.start < first.end
```

This permits an appointment ending at 11:30 and another starting at 11:30. Domain validation requires UTC timestamps, exact 30-minute boundaries, positive duration, and a maximum duration of three hours.

Application validation will provide friendly overlap errors. PostgreSQL will later add an exclusion constraint as the final concurrency-safe guarantee against double booking.

## Cancellation

Cancellation changes a booking from `Active` to `Cancelled`; it does not delete the booking. This preserves audit history and allows the PostgreSQL overlap constraint to ignore cancelled records. Ownership is represented in Domain and will be populated only from the authenticated Application context.

## Business time zone

Persisted instants are UTC. Natural-language dates such as “tomorrow at 10” will be interpreted using the configurable `BusinessTimeZone:Id` option.

The default is `America/Montevideo` because the office is located in Cubo Itaú. The challenge does not specify a time zone, so this is a documented configuration default rather than a hard-coded domain rule.

## Room capacities

The challenge requires room-specific capacities but does not provide their values. The planned deterministic defaults are:

| Room | Capacity |
|------|----------|
| A | 4 |
| B | 6 |
| C | 8 |
| D | 10 |
| E | 12 |

These values will be isolated in seed configuration. Changing them will not require changes to booking rules or use cases.

## LLM authority boundary

The LLM will interpret language and request typed tools. It will not contain business rules, derive authorization, or access PostgreSQL. HTTP endpoints and tools will invoke the same Application use cases, which then enforce Domain rules and persistence constraints.

```text
User message → LLM → typed tool → Application → Domain → PostgreSQL
```

Successful and failed outcomes flow back through the tool to the model. The model may explain an outcome but cannot declare a booking successful without a successful tool result.

## Authentication

React and ASP.NET Core will be served from the same origin, so authentication uses an encrypted HttpOnly cookie instead of a bearer token. The cookie uses `SameSite=Lax` and is always marked `Secure` outside Development. API authentication failures return `401` or `403` rather than redirecting to an HTML login page.

Only `User1` and `User2` are configured, as required by the challenge. Their password is verified with ASP.NET Core's `PasswordHasher`; plaintext credentials are not stored in application configuration. The configured hashes can be replaced through environment variables during deployment.

Application code accesses the request identity through `ICurrentUser`. Neither clients nor LLM tools may provide the booking owner. Authenticated commands that modify state require an antiforgery cookie plus a request token sent through the `X-CSRF-TOKEN` header.

## Deliberately deferred

This foundation does not yet include PostgreSQL, booking endpoints, tool calling, or React. Those behaviors remain in separate pull requests so each change is independently reviewable and keeps CI green.

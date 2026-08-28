# Implementation notes

## Project overview

The solution approaches the challenge as a room-booking system first and a chatbot second. Booking rules, ownership, validation, and concurrency protection are implemented independently of the LLM. The conversational assistant is then a controlled interaction channel over those existing use cases.

This prevents the model from becoming the source of truth for availability or authorization. It can interpret a request such as “reserve room A tomorrow at 10”, but the Application layer decides whether the room exists, has capacity, is free, and belongs to the authenticated user.

## Development path

1. **Project setup** — Created a .NET 8 solution, test projects, package lock files, and GitHub Actions for restore, build, and test.
2. **Core architecture** — Defined Domain, Application, Infrastructure, and API boundaries; modeled rooms, booking periods, booking status, and stable errors.
3. **Authentication** — Added the two challenge users, password-hash verification, same-origin HttpOnly cookies, `ICurrentUser`, and antiforgery protection.
4. **Booking workflow** — Added PostgreSQL persistence, seeded rooms, booking endpoints, ownership checks, cancellation, availability, schedules, and the exclusion constraint for concurrent conflicts.
5. **Tool-calling assistant** — Added Groq integration behind `IChatModel`, typed tools that reuse use cases, owner-scoped in-memory sessions, bounded orchestration, and fake-model tests.

## Key decisions

- **Modular monolith:** enough separation to keep business rules independent, without unnecessary distributed-system complexity.
- **PostgreSQL exclusion constraint:** application pre-checks give friendly errors; the database prevents races.
- **Cookie authentication:** React and API are same-origin, so a cookie is simpler and more appropriate than JWT for this scope.
- **Groq through an internal contract:** Groq's free tier is suitable for the challenge, while the provider remains replaceable and CI does not require a key.
- **LLM has no business authority:** the model requests tools; Application and Domain decide the outcome.
- **Bounded chat execution:** sessions, history, iterations, and tool calls have explicit limits. This avoids unbounded loops and preserves a valid tool-calling conversation sequence.

## Main challenges and how they were addressed

| Challenge | Resolution |
| --- | --- |
| Concurrent booking attempts | PostgreSQL exclusion constraint plus a friendly Application overlap check. |
| Preventing LLM identity spoofing | Tool schemas omit user and owner fields; `ICurrentUser` supplies identity server-side. |
| Avoiding external dependencies in CI | A deterministic `FakeChatModel` covers agent behavior without a Groq key or network access. |
| Tool-call protocol integrity | Tool-call batches are validated before execution and incomplete batches are removed from chat history. |
| Unspecified capacities and timezone | Deterministic room capacities and `America/Montevideo` are documented, isolated configuration assumptions. |

## Verification

The project uses MSTest and FluentAssertions. Tests cover Domain invariants, Application use cases, API authentication and CSRF, real PostgreSQL behavior through Testcontainers, and agent orchestration with a fake model.

The CI workflow executes locked restore, Release build, and the full test suite on pull requests and pushes to `main`.

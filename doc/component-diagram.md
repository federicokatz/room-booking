# Component diagram

This diagram shows what happens from a user chat message to the final assistant response. The React client is served by ASP.NET Core in production, so both use the same origin.

```mermaid
flowchart TD
    Client[React client] -->|Chat message| Api[ASP.NET Core API]
    Api --> Auth[Cookie authentication<br/>and antiforgery]
    Auth --> Chat[ChatAgentService]

    Chat -->|Initial or follow-up model request via IChatModel| Groq[Groq LLM]
    Groq -->|Final response| Chat
    Groq -->|Tool call| Tool[Typed tool]
    Tool --> UseCase[Application use case]
    UseCase --> Domain[Domain rules]
    UseCase --> Repository[Repository]
    Repository --> Database[(PostgreSQL)]
    Database --> Repository
    Repository --> UseCase
    UseCase --> Tool
    Tool -->|Structured tool result| Chat

    Chat -->|Assistant response + effects| Api
    Api --> Client
```

## Responsibilities

1. The React client sends the user message with the authenticated cookie and antiforgery token.
2. The API authenticates the request and resolves the current user on the server.
3. `ChatAgentService` asks the model for a final response or a typed tool call.
4. A tool delegates to an Application use case. It does not access PostgreSQL and does not receive a user identifier.
5. Application and Domain enforce booking rules; PostgreSQL is the final protection against concurrent double bookings.
6. The tool result returns to the model, which produces the final assistant response and optional UI effects.

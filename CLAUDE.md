# Selfwork Acquiring — Project Context

## Purpose
.NET 9 client library + ASP.NET Core 9 web service for integrating with the Selfwork Acquiring API (selfwork.ru).  
Covers the full payment cycle: API verification, invoice creation, status polling, and webhook handling.

## Repository Layout

```
payment/
├── src/
│   ├── Selfwork.Acquiring.Client/   # NuGet-publishable client library
│   └── Selfwork.Acquiring.Web/      # Demo ASP.NET Core 9 service
├── tests/
│   └── Selfwork.Acquiring.Tests/    # xUnit unit + integration tests
├── docs/
│   ├── ТЗ.md                        # Техническое задание
│   └── superpowers/specs/           # Design docs
├── Selfwork.Acquiring.sln
├── CLAUDE.md                        # ← you are here
└── README.md
```

## Testing Convention: AAA Pattern

All tests **must** follow the **Arrange / Act / Assert** structure with explicit comments:

```csharp
[Fact]
public async Task MethodName_Scenario_ExpectedBehavior()
{
    // Arrange
    var input = ...;

    // Act
    var result = await svc.DoSomething(input);

    // Assert
    result.Should().Be(...);
}
```

- **Arrange** — set up stubs, mocks, and input data  
- **Act** — single call to the system under test  
- **Assert** — verify outcome with FluentAssertions  
- Test names follow the pattern: `Method_Scenario_ExpectedBehavior`  
- If a guard throws before any HTTP call, note `// Arrange — no stub needed`

## Key Conventions

- **Target framework:** `net9.0`
- **Nullable:** enabled everywhere
- **Serialization:** `System.Text.Json` with `JsonSerializerDefaults.Web` + snake_case naming
- **DI:** everything registered via `IServiceCollection` extension in the Client library
- **HttpClient:** typed client via `AddHttpClient<IAcquiringService, AcquiringService>()`
- **Resilience:** Polly retry (3×, exponential back-off) + circuit-breaker on transient HTTP errors
- **Auth:** `X-Api-Key` header injected by `AuthHeaderHandler` (DelegatingHandler)
- **Webhook verification:** HMAC-SHA256 over `invoiceId:status:amount` with secret from config
- **Error handling:** `AcquiringApiException` wraps HTTP error responses; never swallows
- **Logging:** structured `ILogger<T>` at every service boundary
- **Tests:** WireMock.Net for HTTP mocking; no real network calls in CI

## Configuration Section

```json
{
  "Selfwork": {
    "BaseUrl": "https://api.selfwork.ru",
    "ApiKey": "...",
    "WebhookSecret": "...",
    "RetryCount": 3,
    "TimeoutSeconds": 30
  }
}
```

## Running Locally

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/Selfwork.Acquiring.Web
```

## Environment Variables (override appsettings)

| Variable | Description |
|---|---|
| `Selfwork__ApiKey` | API key from selfwork.ru dashboard |
| `Selfwork__WebhookSecret` | Webhook signing secret |
| `Selfwork__BaseUrl` | Override for sandbox/prod |

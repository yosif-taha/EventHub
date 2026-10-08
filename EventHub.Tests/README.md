# EventHub automated tests

This project targets .NET 9 and uses xUnit. Existing Event and validator suites are preserved. Tests do not contact Paymob, SMTP servers, or a running EventHub deployment.

## Run

From the solution directory:

```powershell
dotnet test EventHub.Tests/EventHub.Tests.csproj
dotnet build
```

For coverage, write generated results outside the repository:

```powershell
dotnet test EventHub.Tests/EventHub.Tests.csproj --no-restore --collect:"XPlat Code Coverage" --settings EventHub.Tests/coverage.runsettings --results-directory "$env:TEMP/EventHubTestResults" --logger "trx;LogFileName=coverage.trx"
```

The coverage configuration excludes the test assembly, generated migrations, Razor views, and obj files. It retains application configuration and startup code; uncovered lines are not silently removed to increase percentages.

## Organization and test boundaries

| Directory | Main responsibilities | Approach |
| --- | --- | --- |
| Domain | Capacity, registration availability, transitions, refresh-token activity | Pure unit tests |
| Validators | All 30 application validators, invalid inputs and boundary values | Pure unit tests |
| Application | Validation pipeline, results, pagination, handlers, events, categories, registration/cancellation, payments, notifications, dashboards, scoped reads | Pure/mocked units and real MediatR workflows over SQLite |
| Persistence | Repository tracking and projections, soft deletion, transactions/rollback, relational constraints, optimistic concurrency, role seeding/bootstrap | Isolated SQLite persistence tests |
| Infrastructure | Identity authentication and roles, profile operations, JWT validation, claims, Paymob request contracts, configuration | Isolated Identity integration tests and local transport doubles |
| WebAPI | Routing/binding, JWT authentication/roles, response envelopes, private meeting URLs, signed webhooks, background-worker startup | In-process TestServer HTTP tests and worker tests |
| MVC | Input normalization, editing/announcement flows, safe redirects, cookie token handling, backend error mapping, view-model validation, cookie authorization/antiforgery | Controller/service units and in-process HTTP security tests |

New tests carry a Category trait where useful: Unit, MockUnit, Integration, Persistence, or Http. Original domain/validator tests and the small application value/pipeline tests are untagged pure units. For example:

```powershell
dotnet test EventHub.Tests/EventHub.Tests.csproj --filter "Category=Persistence"
dotnet test EventHub.Tests/EventHub.Tests.csproj --filter "Category=Http"
```

### Database isolation

Every WorkflowFixture owns a separate in-memory SQLite connection, context, DI scope, and external-service doubles. EnsureCreated applies the production EF model. No database server, credentials, shared mutable fixture, or execution order is required.

SQLite cannot generate SQL Server rowversion values or integer identity values within a composite primary key. The test context generates these values explicitly while retaining concurrency checks and relationships. These adaptations do **not** establish SQL Server migration, locking, collation, or provider-specific guarantees. Profile tests accommodate SQLite's uppercase GUID text representation.

Identity workflows clear tracking between simulated requests. Data protection uses ephemeral keys. Generated identity IDs, security tokens and lease IDs are treated as opaque values; tests assert behavior rather than random output or exact timestamps.

### HTTP isolation

API tests use production controllers, mappings, JWT configuration, model binding, authorization, and exception middleware, with a strict mediator double. Application integration tests separately use real MediatR, validators, repositories, and the unit of work. This split verifies both boundaries without starting the production database initializer or a recurring worker in every HTTP test.

MVC security tests use actual cookie and antiforgery middleware in an in-process host. Their sign-in endpoint is test-only. Controller unit tests explicitly supply invalid ModelState when testing the action's response to MVC validation.

Payment transport tests inspect requests using a local HttpMessageHandler. Email delivery is substituted at IEmailService; production SMTP is never contacted.

### Time

Domain event tests use fixed timestamps. Features that read DateTime.UtcNow directly use dates safely away from the current boundary (or relative times separated by hours for reminder windows), rather than sleeps or exact-now assertions. Worker tests synchronize on task completion signals and cancel before the five-minute timer.

## Regression fixes retained in the working tree

The previous session reproduced each defect with a failing test before applying a focused correction:

- GenericRepository: tracking lookups can see unsaved additions in the current unit of work; soft deletion updates the tracked instance even when passed a different instance.
- DeleteEventCommandHandler: deletes the loaded entity so its concurrency token is retained.
- EventProfile: binds calculated availability values to the record constructor used by query projection.
- EventController: binds the legacy detail query parameter and canonical detail route parameter separately.
- AuthService: tracks user/token mutations and checks Identity save results, so login and refresh cannot return unpersisted tokens.

The corresponding regression suites cover free registration, tracked soft deletion, event deletion, availability projections, both detail routes, refresh rotation/replay, and failed Identity saves.

## Deliberate limits

- No live SQL Server migrations, SQL Server-generated rowversion, provider locking/isolation races, or SQL Server collation verification.
- No live SMTP/Paymob contract, TLS, credential, rate-limit, or network-delivery verification.
- No browser-rendered Razor/UI, JavaScript, visual, or end-to-end deployed application tests.
- No real five-minute background scheduling wait; startup ordering, failure containment, cancellation, and notification processing are tested independently.
- Direct DateTime.UtcNow dependencies prevent deterministic exact-boundary tests for some application workflows without introducing a production clock abstraction.
- Startup composition and selected failure branches may remain uncovered. Coverage percentages are measurements, not a claim that every possible behavior has been tested.

## Validation snapshot — 2026-10-08

One test project, 86 test files, 329 test methods, and 914 executed cases: 914 passed, 0 failed, 0 skipped. Test-file counts exclude support helpers. All 402 validator cases and the original 52 Event cases remain passing.

| Layer | Files | Methods | Cases | Measured line coverage |
| --- | ---: | ---: | ---: | ---: |
| Domain | 3 | 14 | 59 | 99.00% |
| Application | 15 | 58 | 142 | 95.58% |
| Validators | 30 | 127 | 402 | 100.00% |
| Persistence | 4 | 15 | 25 | 95.09% |
| Infrastructure | 8 | 19 | 54 | 79.45% |
| WebAPI | 11 | 43 | 90 | 87.17% |
| MVC | 15 | 53 | 142 | 74.95% |

Application assembly coverage includes validators; the separate validator measurement covers their 268 executable lines. Other percentages are assembly-level measurements using the exclusions above.

Mutually exclusive case classification: 515 pure units, 156 mock-based units, 124 application/Identity integration cases, 25 persistence cases, and 94 HTTP cases. The HTTP count includes six MVC security cases; WebAPI also contains two mock-based worker cases.

Both the coverage command above and the plain `dotnet test EventHub.Tests/EventHub.Tests.csproj` command passed all 914 cases. On this sandboxed host, plain restore initially required additional read permission for the existing user NuGet.Config; the exact command passed after that access was granted. Test execution itself needs no network services.

`dotnet build` succeeded with zero errors and five existing production warnings: two NU1902 messages for MailKit 4.15.1, one CS8603 in DbExecutor, and two CS8981 messages for the lowercase migration class. `git diff --check` passed. No commits were created.

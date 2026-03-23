# TICKET.md

## Title
Harden task creation endpoint and optimize list query performance

## Priority
**High**

## Type
Bug fix / Performance

## Reporter
Joel

## Background

During a production incident investigation, three bugs were identified in the task API:

1. `POST /api/tasks` throws an unhandled `FormatException` and returns 500 when the `X-Client-Timestamp` header is missing or invalid
2. Input validation runs after the crashing parse call, so bad requests also return 500 instead of 400
3. `GET /api/tasks` loads the full `Tasks` table into memory and filters in C#, causing performance to degrade as the database grows

A fourth issue — no database index on `UserId` — compounds the list performance problem.

## Acceptance Criteria

- [ ] `POST /api/tasks` returns `201 Created` when `X-Client-Timestamp` is absent — server falls back to `DateTime.UtcNow`
- [ ] `POST /api/tasks` returns `201 Created` when `X-Client-Timestamp` is present but unparseable
- [ ] `POST /api/tasks` returns `400 Bad Request` when `userId` or `title` is missing/empty
- [ ] `GET /api/tasks` filters and sorts in the database query, not in application memory
- [ ] A composite index exists on `(UserId, CreatedAt)` in the database schema
- [ ] All existing tests pass
- [ ] New regression tests cover each of the above cases
- [ ] `RUNBOOK.md` is updated with diagnosis and verification steps

## Steps to Reproduce (500 on create)

1. Run the API locally: `dotnet run` from `src/SupportEngineerChallenge.Api`
2. POST to `/api/tasks` via Swagger **without** the `X-Client-Timestamp` header
3. Observe `500 Internal Server Error` and `FormatException` in logs

## Proposed Fix

See `TaskEndpoints.cs` and `AppDbContext.cs` changes in this branch. Summary:
- Replace `DateTime.Parse(clientTimestamp)` with `DateTime.TryParse(...) ? parsed : DateTime.UtcNow`
- Add optional header in Swagger for clients needing timestamp creation in UI
- Move validation above timestamp parsing
- Push filtering/sorting into the EF Core query
- Add composite index on item creation

## Testing Notes

Run `dotnet test` — all new and existing tests should pass. Manually verify via Swagger by posting without the timestamp header.

## Out of Scope

- Pagination beyond the existing `limit` parameter
- Timestamp validation that entered value is not in the past
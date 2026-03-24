# Runbook — SupportEngineerChallenge

> Updated March 2026

## Service overview
- **Service:** SupportEngineerChallenge.Api
- **Purpose:** Minimal task tracker (create + list tasks)
- **Data store:** SQLite (`app.db` in the API working directory)

## Common commands

**Run locally**
```bash
cd src/SupportEngineerChallenge.Api
dotnet run
```

**Run tests**
```bash
dotnet test
```

## Key endpoints
- `GET /api/tasks?userId={id}&limit={n}`
- `POST /api/tasks`

## Using log artifacts

- **Create-task 500:** Inspect `artifacts/sample_api_log.txt` (or production logs). Look for the `CreateTask request` line — `X-Client-Timestamp present=False` or `length=0` indicates missing/invalid header. The stack trace shows `FormatException` at `DateTime.Parse`.
- **Slow list:** Look for `ListTasks completed` lines with high `elapsedMs` (e.g. `artifacts/sample_slow_list_log.txt`). Correlate `userId` and `limit` with slow requests.

## Troubleshooting checklist

### "Create task fails with 500"
- Check API logs for exceptions (particulary `FormatException`) in `TaskEndpoints.cs`.
- Confirm whether `X-Client-Timestamp present=False` appears in the `CreateTask request` log line.
- **Fix:** Instruct client to include UTC timestamp in API call or leave empty to have it automatically set to now.
- **Verify:** POST to `/api/tasks` via Swagger without the `X-Client-Timestamp` header — should return `201`, not `500`.

### "Tasks list is slow"
- Check logs for `ListTasks completed` lines with high `elapsedMs`.
- Verify the count of tasks for a client and how many have been recently created
- **Likely cause:** Inefficient calls may scale linearly with total row count across all users. 
- **Fix:** See if the client can consoliate multiple task calls into a single one, or search by task ID rather than the default sort.
- **Verify:** After fix, `elapsedMs` in logs should drop significantly. Run with a large seeded dataset to confirm.

### "Duplicates / wrong order after refresh"
- Compare the raw API response (via Swagger or browser DevTools) against what the UI displays.
- **Root cause:** The UI may be appending new results to existing state on refresh rather than replacing it, causing duplicates. Order issues may stem from the old in-memory sort being inconsistent.
- **Fix:** Ensure the UI replaces (not merges) the task list on each fetch. The API-side fix (server-side `OrderByDescending`) ensures consistent ordering from the source.
- **Verify:** Create several tasks, refresh via button (not browser) repeatedly — confirm count stays stable and order is newest-first.

## Verification steps
1. POST to `/api/tasks` via Swagger **without** `X-Client-Timestamp` header → expect `201 Created`, no 500.
2. POST with an invalid timestamp value → expect `201 Created` with `createdAt` defaulting to server UTC time.
3. POST with missing `userId` or `title` → expect `400 Bad Request`.
4. GET `/api/tasks?userId=user-001` → confirm only that user's tasks are returned, ordered newest-first.
5. Refresh the UI repeatedly → confirm no duplicate tasks appear and order is stable.
6. Run `dotnet test` → all tests pass.

## Rollback / mitigation
- **Code rollback:** Revert `TaskEndpoints.cs` to v1.8. Note this re-introduces 
the datetime bug and will impact all pre-v2.0 clients.
- **Slow list mitigation:** If a performance regression occurs before a code fix 
can be deployed, instruct clients to reduce their `limit` parameter to reduce load.

## Future improvements
- Add proactive alerting on 5xx error rates
- Add `X-Client-Timestamp` format validation on the client side
- Harden API input validation across all endpoints
# Support Engineer Challenge — Debug & Stabilize

## Challenge: Please complete the following in ~2-4 hours:

1. **Triage & reproduction**  
   - Use the provided log artifacts (e.g. `artifacts/sample_api_log.txt`) to diagnose at least one issue
   - Clear repro steps for each confirmed issue
   - Capture evidence (logs, stack traces, etc.)

2. **Root cause analysis**  
   - Explain what's happening and why (briefly)

3. **Fixes**  
   - Safe, minimal fixes
   - Add/update tests where appropriate

4. **Operational thinking**  
   - Update `RUNBOOK.md` with diagnosis + verification + rollback/mitigation steps

5. **Incident summary**  
   - Fill in `INCIDENT.md` with impact/timeline/root cause/fix/follow-ups

6. **Follow-up ticket**  
   - Write `TICKET.md` with a high-quality ticket (acceptance criteria, priority, etc.)

## Notes

- You can change anything in this repo (including UI) as long as you explain your choices.
- If you get blocked by setup, write down what you tried and where you got stuck.

## Resolutions

### Issues Confirmed and Fixed

1. **500s on task creation** — Was able to reporoduce (see screenshots) and verify same error client reported in `artifacts/sample_api_log.txt`. `DateTime.Parse` was called unconditionally on the `X-Client-Timestamp` header and crashing when the header was absent. This was resolved by updating to `DateTime.TryParse` and falling back to `DateTime.UtcNow`, and also making it clear to users by adding the header in the Swagger UI. Input validation order was also updated to improve error handling messages.

2. **Slow task list** — Confirmed behavior reported in `artifacts/sample_slow_list_log.txt` y checking load times in dev tools. The slow task list was caused due to the full `Tasks` table being loaded into memory before filtering. Fixed by pushing filtering, sorting, and limiting into the EF Core query, and by adding a composite index on `(UserId, CreatedAt)` in `AppDbContext.cs`.

3. **Duplicate tasks / wrong order on refresh** — Confirmed via UI testing by hitting `Refresh`. The frontend was concatenating results onto the existing state on every refresh instead of replacing them. Fixed in `main.js` by replacing `concat` with a direct assignment.

### Trade-offs Made

- I kept fixes minimal as part of a simulated incident to unblock clients as quickly and safely as possible rather than focusing on broader refactoring improvements or more verbose error handling.
- Made `X-Client-Timestamp` optional rather than enforcing it, to maintain backwards compatibility with pre-v2.0 clients.
- Used `DateTime.UtcNow` as the fallback rather than rejecting the request, prioritising availability over strict validation.

### Next Steps With More Time

- Add proactive alerting on 5xx errors so incidents are detected before customers report them.
- Add client version and `User-Agent` tracking to identify which clients are running outdated versions.
- Investigate adding cleaner EF Core migrations rather than relying on `EnsureCreated` for schema management.
- Add pagination support to the list endpoint rather than a hard `limit` cap.
- Expand test coverage to include load/performance tests to catch in-memory filtering regressions earlier##.
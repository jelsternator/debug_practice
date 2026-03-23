# INCIDENT.md

## Incident Summary

**Title** Unhandled Timestamp error causing 500s on task creation for users running versions prior to v2.0
**Date:** 2026-03-22
**Severity:** P0 - All clients running versions prior to v2.0 are impacted
**Status:** Resolved
**Reporter:** Customer reports via slack
**Responder:** Joel

---

## Impact

- **Who:** All pre-v.2.0 users attempting to create tasks via UI or API
- **What:** `POST /api/tasks` returned `500 Errors` and tasks were not created
- **Duration:** 2026-02-12:00 to 2026-02-14:00
- **Scope:** Impacted clients fully blocked by task creation 

---

## Detection
- Client reachout via Slack. There were no proactive alerts or monitoring in place.

---

## Timeline (UTC)

| Time | Event |
|------|-------|
| 2026-02-22-12:00- UTC | Customer reached out via slack reporting issues in task creation and various other issues, included `artifacts/sample_api_log.txt` |
| T+10 (min) | Support Team reviewed, filed CUS-3254, and escalated to Support Engineering (SE), flagging as high priority
| T+15 | SE reviews ticket and confirms error is reproducable and is blocking large segment of clients. P0 incident created to coordinate response|
| T+25 | Support Team displays banner notifying specific clients of product outage |
| T+30 | Root cause confirmed to be `DateTime.Parse("")` raising unhandled exception when `X-Client-Timestamp` header is absent |
| T+60 | While testing fix, secondary issues identified (validation order bug, in-memory list filtering, missing DB index) |
| T+90 | PR opened with updated tests passing |
| T+120 | PR approved, merged, and deployed. Alerts setup for future 500 errors |
| T+150 | All impacted clients unblocked, banner removed. Retro doc created | 

---

## Root Cause

As part of the v2.0 feature release, client-side timestamps were introduced in task creation via the new `X-Client-Timestamp` header to better handle timezones rather than passing in server time.

This introduced a regression for clients that had not yet been updated to v2.0 and had not been setup to explicitly pass the `X-Client-Timestamp` header resulting in a `500` exception when the server attempted to parse an empty string. While investigating the `500` errors other regressions were noticed and fixed.


---

## Fixes

1. **500 error:** Updated datetime validation to fallback on `DateTime.UtcNow` when header is absent or unparsable, restoring compatibility with pre-v.2.0 clients.

2. **Input validation:** Input validation for `userId` and `title` are now checked before datetime validation and show correct errors.

3. **Performance:** Created index and memory improvements when getting tasks for user. This solves for slow load times.

4. **UI Improvements** No longer duplicating tasks in the UI on refresh.

---

## Follow-ups

- `Ticket.md` filed for immediate fix (merged in)
- Add proactive monitoring 
- Create tests for legacy users to alert on failures before major releases

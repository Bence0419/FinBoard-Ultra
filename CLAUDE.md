# CLAUDE.md – Finance Board Ultra

This file defines what Finance Board Ultra is, what it does, and the constraints that apply regardless of implementation. Architecture, project structure, and library choices are not prescribed here — those decisions are made as development progresses and recorded in the decisions log at the bottom.

---

## What this project is

A personal finance dashboard desktop application written in C#. Users manage their income, expenses, and investments in one place. The application runs as a native desktop app.

The current focus is getting the native desktop wrapper working. Backend and frontend work follows after that spike is complete.

---

## Functional scope

### Authentication
Users register with an email and password. Login requires email and password. Multi-factor authentication via SMS code is supported but optional per user. The system handles forgotten passwords and allows users to change their password. Sessions expire and users can log out explicitly.

### Dashboard
After login, the user lands on a dashboard that summarises their financial situation. It shows current balance, monthly income and expenses, total investment value, recent transactions, and a breakdown by category. The figures are always derived from the underlying records — there is no manually maintained balance field.

### Financial records
Users create, view, edit, and delete financial records. A record is one of three types: income, expense, or investment. Every record requires a type, an amount, a date, and a category. Amount is always positive — the type determines whether it is money in or out. Investment records carry additional fields such as asset name, quantity, purchase price, current price, and platform. Deleting a record must be reversible, so hard deletes are not used.

### Categories
Users organise records with categories. Categories belong to a user and are scoped to a record type. The system ships with sensible defaults but users can create and manage their own.

### Filtering and reporting
Users can filter their records by date range, category, and type. The system provides aggregated views by week, month, and year. Income and expenses can be compared over time. Investment performance can be reviewed separately.

### Profile and security settings
Users can update their name, phone number, and preferred currency. They can change their password, enable or disable MFA, and review active sessions.

---

## Business rules

- A user can only access their own data. Every data operation must be scoped to the authenticated user.
- Every financial record requires a type, an amount, a date, and a category.
- Amount is always a positive decimal. Type determines direction.
- Deleting a financial record sets a deleted flag. The record is retained in the database.
- Dashboard figures are aggregated from records at query time.
- MFA requires a valid phone number on the account before it can be enabled.
- An MFA code is single-use and expires after 10 minutes.

---

## Security constraints

- Passwords are stored as a one-way hash. No plain text or reversible format.
- Authentication is token-based with expiry and refresh.
- Every protected operation requires a valid authenticated session.
- Login attempts are rate-limited per IP.
- Critical actions are audit-logged: login, logout, password change, MFA toggle, record create, update, and delete.

---

## Fixed decisions

- Language: C#
- Target: native desktop application

---

## Decisions log

Append every settled decision here so context is not lost between sessions.

| Date | Decision | Reason |
|---|---|---|
| — | Language: C# | Developer preference |
| — | Native desktop target | First deliverable is the native wrapper |
| 2026-04-09 | Target framework: net9.0 (not net8.0) | .NET 8 not installed on dev machine; only .NET 9 and 10 available |
| 2026-04-09 | ORM: EF Core 9 + SQLite (Microsoft.EntityFrameworkCore.Sqlite 9.0.14) | Embedded, no server process; appropriate for standalone desktop app |
| 2026-04-09 | Password hashing: BCrypt.Net-Next, work factor 12 | Adaptive hash with salt; work factor 12 balances security and registration speed |
| 2026-04-09 | AppDbContextFactory must be public (not internal) | EF Core design-time tooling discovers factory via reflection; internal prevents migration generation |
| 2026-04-09 | AuditLog.UserId is a plain nullable column, not a FK | Audit rows must survive user deletion; a FK would block or cascade-delete audit history |
| 2026-04-09 | No global HasQueryFilter for IsDeleted | Explicit per-query filtering avoids hidden behaviour and makes each query's intent clear |
| 2026-04-09 | RecordFilter query type lives in Domain, not Application | IFinancialRecordRepository (Domain) takes it as a parameter; placing it in Application would create a circular dependency |

---

## How we build this

Follow these steps at the start of every development session, in order.

1. **Read the three reference documents first.** Read `CLAUDE.md`, `ARCHITECTURE.md`, and `IMPLEMENTATION_PLAN.md` before writing any code. Do not rely on memory from a previous session.

2. **Identify the current task.** Check the progress tracker in `IMPLEMENTATION_PLAN.md`. Find the first phase that is not marked Complete. Within that phase, find the first task that has not been marked done. That task is the only thing to work on this session.

3. **Work on exactly that task and no other.** Do not start a later task, do not refactor adjacent code, do not jump ahead because a future task looks simple. One task at a time.

4. **Mark each task done immediately after completing it.** Update `IMPLEMENTATION_PLAN.md` to reflect the completed task before moving to the next one. Do not batch updates.

5. **Run the phase verification step before closing a phase.** When the last task in a phase is done, run the exact command listed under that phase's verification section. Confirm the output matches what is expected. Only then mark the phase Complete in the progress tracker.

6. **Append new decisions to the decisions log before ending the session.** If any implementation choice was made that is not already recorded in `CLAUDE.md` — a library version pinned, a pattern adopted, a constraint discovered — add a row to the decisions log at the bottom of this file. Do not let decisions live only in code or commit messages.

7. **Never leave a phase partially complete without updating the progress tracker.** If a session ends mid-phase, update the tracker to *In progress* and add a note identifying the last completed task. The next session must be able to resume without reading git history.

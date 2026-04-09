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

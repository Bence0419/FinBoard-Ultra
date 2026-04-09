# Finance Board Ultra

> Personal finance — owned by you, running on your machine.

Finance Board Ultra is a native desktop application for tracking income, expenses, and investments. No subscriptions, no cloud sync, no third-party access to your financial data.

---

## What it does

- **Dashboard** — balance, monthly income and expenses, investment value, and recent transactions at a glance
- **Financial records** — create, edit, and manage income, expense, and investment entries with full categorisation
- **Investments** — track assets with purchase price, current value, quantity, and platform
- **Filtering and reports** — slice data by date range, category, type, and time period
- **Secure authentication** — email and password login with optional SMS-based MFA
- **Profile settings** — preferred currency, phone number, password, and session management

---

## Status

| Phase | Description | Status |
|---|---|---|
| 1 | Native core — full domain logic, console-exercisable, unit-tested | In progress |
| 2 | Security layer — MFA, audit log, rate limiting, password reset | Planned |
| 3 | Analytics — charts, advanced filtering, investment performance | Planned |
| 4 | Advanced — export, budgets, notifications, external integrations | Planned |

---

## Tech

- C# / .NET 8
- Native desktop (approach determined after Phase 1 spike)
- PostgreSQL
- Full unit test coverage on domain logic

---

## Getting started

Documentation will be added as the project structure is finalised after the native component spike.

---

## Design principles

**Local first.** Your data stays on your machine. No account required beyond your own instance.

**Hard deletes are forbidden.** Financial records are never permanently removed — only soft-deleted and recoverable.

**No stored balance.** Every figure on the dashboard is derived from your actual records at query time.

**User isolation is absolute.** The system is built so that one user's data is structurally inaccessible to any other.

---


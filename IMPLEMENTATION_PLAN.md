# Finance Board Ultra — Native Component Implementation Plan

This document is the authoritative build guide for the native component. Each development session
opens this file first, updates the progress tracker, and works through the next incomplete task in
sequence. A phase is not complete until its verification step passes with zero errors or failures.

---

## Progress Tracker

| Phase | Title | Status |
|---|---|---|
| 1 | Solution scaffold and Domain layer | **Complete** |
| 2 | Infrastructure layer | Not started |
| 3 | Application layer | Not started |
| 4 | Console runner and end-to-end smoke test | Not started |
| 5 | Unit tests | Not started |
| 6 | Integration tests | Not started |

> **How to use this tracker:** At the start of each session, change the relevant phase to
> *In progress*. When the phase verification step passes, change it to *Complete*.

---

## Conventions applied throughout

- All `async` methods return `Task` or `Task<T>`; no `void` async methods.
- `DateTime` values are always UTC — use `DateTime.UtcNow`, never `DateTime.Now`.
- Hard deletes are forbidden. Any deletion sets `IsDeleted = true` via `UpdateAsync`.
- No service method may expose raw EF Core types — all public return types are either
  domain entities or DTOs.
- Every state-changing service method writes an `AuditLog` entry before returning.
- Amount fields are always positive `decimal`; direction is determined by `RecordType`.

---

## Phase 1 — Solution scaffold and Domain layer

**Status: Complete**

### What was built

The full solution structure was created with six projects, project references wired exactly as
specified in ARCHITECTURE.md §2, and the entire `FinBoardUltra.Domain` project implemented.

### Files committed

| Area | Files |
|---|---|
| Solution | `FinBoardUltra.slnx` |
| Domain project | `src/FinBoardUltra.Domain/FinBoardUltra.Domain.csproj` |
| Entities | `User`, `FinancialRecord`, `InvestmentDetail`, `Category`, `MfaChallenge`, `UserSession`, `AuditLog` |
| Enums | `RecordType`, `PeriodType`, `AuditAction` |
| Exceptions | `DomainException`, `ValidationException`, `NotFoundException`, `UnauthorizedException`, `ConflictException`, `TooManyRequestsException` |
| Queries | `RecordFilter` |
| Repository interfaces | `IUserRepository`, `IFinancialRecordRepository`, `ICategoryRepository`, `IMfaChallengeRepository`, `IUserSessionRepository`, `IAuditLogRepository` |
| Service interfaces | `IPasswordHasher`, `ITokenGenerator` |
| Empty shells | `Application`, `Infrastructure`, `ConsoleApp`, `Tests.Unit`, `Tests.Integration` |

### Phase verification (already passing)

```
dotnet build FinBoardUltra.slnx
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

---

## Phase 2 — Infrastructure layer

**Goal:** Implement EF Core persistence and the two security services. By the end of this phase
the database schema can be created from migrations and every domain interface has a concrete
implementation, but nothing is wired into a running application yet.

---

### Task 2.1 — Add NuGet packages to the Infrastructure project

**What:** Install the three required packages into `FinBoardUltra.Infrastructure`.

```
dotnet add src/FinBoardUltra.Infrastructure reference — not applicable
dotnet add src/FinBoardUltra.Infrastructure package Microsoft.EntityFrameworkCore.Sqlite --version 9.*
dotnet add src/FinBoardUltra.Infrastructure package Microsoft.EntityFrameworkCore.Design --version 9.*
dotnet add src/FinBoardUltra.Infrastructure package BCrypt.Net-Next
```

**Acceptance criterion:** `dotnet restore` succeeds; the three packages appear in
`FinBoardUltra.Infrastructure.csproj`.

**Constraints:**
- Use the EF Core SQLite provider, not the SQL Server or PostgreSQL provider.
- `Microsoft.EntityFrameworkCore.Design` is required for the `dotnet ef migrations add` tool.
- BCrypt.Net-Next, not BCrypt.Net (the original unmaintained package).

---

### Task 2.2 — Create `AppDbContext`

**What:** Create `src/FinBoardUltra.Infrastructure/Persistence/AppDbContext.cs` — the EF Core
`DbContext` that registers all seven domain entities and applies their configurations.

The class must:
- Inherit from `DbContext`.
- Expose a `DbSet<T>` for each entity: `Users`, `FinancialRecords`, `InvestmentDetails`,
  `Categories`, `MfaChallenges`, `UserSessions`, `AuditLogs`.
- Override `OnModelCreating` and configure:
  - `FinancialRecord.Amount` — column type `decimal(18,4)`.
  - `InvestmentDetail.Quantity`, `PurchasePrice`, `CurrentPrice` — column type `decimal(18,4)`.
  - `InvestmentDetail` — unique index on `RecordId` (enforces 1:1 with `FinancialRecord`).
  - `User.Email` — unique index (case-insensitive collation where supported).
  - `AuditLog` — no cascade deletes; rows are append-only.
  - `UserSession.Token` — unique index.
  - Query filters: none at the DbContext level — soft-delete filtering is done inside
    repository methods, not as global query filters, to keep behaviour explicit.

**Acceptance criterion:** `dotnet build FinBoardUltra.slnx` succeeds with zero errors.

**Constraints:**
- No global `HasQueryFilter` for `IsDeleted` — each repository method must explicitly filter.
  This is intentional: it prevents silent omission of deleted records in audit/admin queries.
- The context must accept `DbContextOptions<AppDbContext>` via constructor injection.

---

### Task 2.3 — Create the initial EF Core migration

**What:** Use the EF Core tools to generate the initial migration and verify the generated SQL
produces the correct schema.

```
dotnet tool install --global dotnet-ef   # if not already installed
dotnet ef migrations add InitialSchema \
  --project src/FinBoardUltra.Infrastructure \
  --startup-project src/FinBoardUltra.Infrastructure \
  --output-dir Persistence/Migrations
```

Because the Infrastructure project is a class library (not a console app), a temporary
`IDesignTimeDbContextFactory<AppDbContext>` must be added inside the Infrastructure project
for the tools to instantiate the context:

```
src/FinBoardUltra.Infrastructure/Persistence/AppDbContextFactory.cs
```

The factory points the context at a local `finboard-design.db` file (for tooling only;
the real path is supplied by the ConsoleApp at runtime).

**Acceptance criterion:**
1. `Persistence/Migrations/` directory contains `<timestamp>_InitialSchema.cs` and
   `AppDbContextModelSnapshot.cs`.
2. `dotnet ef migrations script --project src/FinBoardUltra.Infrastructure` produces
   SQL that creates tables for all seven entities with correct column types.

**Constraints:**
- Do not apply the migration to a real database in this task — only generate the files.
- The `AppDbContextFactory` is for tooling only; it must not be registered in DI.
- Commit both the migration file and the snapshot.

---

### Task 2.4 — Implement `UserRepository`

**What:** Create `src/FinBoardUltra.Infrastructure/Persistence/Repositories/UserRepository.cs`
implementing `IUserRepository`.

| Method | Behaviour |
|---|---|
| `GetByIdAsync(Guid id)` | Returns the user if found and not soft-deleted; otherwise `null`. |
| `GetByEmailAsync(string email)` | Case-insensitive match; returns non-deleted user or `null`. |
| `AddAsync(User user)` | Sets `Id = Guid.NewGuid()` and `CreatedAt = DateTime.UtcNow` if not set, then adds and saves. |
| `UpdateAsync(User user)` | Marks entity as modified and saves. |

**Acceptance criterion:** Class compiles; all four methods are implemented with no `NotImplementedException`.

**Constraints:**
- Use `StringComparison.OrdinalIgnoreCase` or SQLite's `COLLATE NOCASE` for the email lookup —
  do not rely on the database default collation.
- `IsDeleted == true` records must never be returned by any read method.

---

### Task 2.5 — Implement `FinancialRecordRepository`

**What:** Create `src/FinBoardUltra.Infrastructure/Persistence/Repositories/FinancialRecordRepository.cs`
implementing `IFinancialRecordRepository`.

| Method | Behaviour |
|---|---|
| `GetByIdAsync(Guid userId, Guid recordId)` | Returns the record only if it belongs to `userId` and `IsDeleted == false`; otherwise `null`. |
| `GetAllAsync(Guid userId, RecordFilter? filter)` | Returns all non-deleted records for the user, applies `RecordFilter` predicates in AND combination, orders by `Date` descending. Eagerly loads `InvestmentDetail`. |
| `AddAsync(FinancialRecord record)` | Sets `Id`, `CreatedAt`, and `UpdatedAt`; saves. |
| `UpdateAsync(FinancialRecord record)` | Sets `UpdatedAt = DateTime.UtcNow`; marks modified; saves. |
| `SumByTypeAsync(Guid userId, RecordType type, DateOnly? from, DateOnly? to)` | Sums `Amount` for non-deleted records of the given type, optionally within the date range. Returns `0m` if no records match. |
| `GetRecentAsync(Guid userId, int count)` | Returns the `count` most recent non-deleted records ordered by `Date` descending. |

**Acceptance criterion:** Class compiles; all six methods are implemented.

**Constraints:**
- `GetByIdAsync` must check both `UserId == userId` AND `IsDeleted == false` — never return
  a deleted record even if the ID matches.
- `GetAllAsync` must apply `RecordFilter` predicates only when the filter field is non-null.
- Eagerly load `InvestmentDetail` in `GetAllAsync` and `GetByIdAsync` using `.Include()`.

---

### Task 2.6 — Implement `CategoryRepository`

**What:** Create `src/FinBoardUltra.Infrastructure/Persistence/Repositories/CategoryRepository.cs`
implementing `ICategoryRepository`.

| Method | Behaviour |
|---|---|
| `GetByIdAsync(Guid id)` | Returns the category if not soft-deleted; otherwise `null`. |
| `GetAllAsync(Guid userId, RecordType? type)` | Returns all non-deleted categories where `UserId == userId` OR `UserId == null` (system defaults). If `type` is supplied, additionally filters on `Category.Type`. |
| `AddAsync(Category category)` | Sets `Id`; saves. |
| `UpdateAsync(Category category)` | Marks modified; saves. |
| `AnyDefaultsExistAsync()` | Returns `true` if any `Category` with `IsDefault == true` exists (deleted or not). Used by the seeder. |

**Acceptance criterion:** Class compiles; all five methods are implemented.

**Constraints:**
- System defaults (`UserId == null`) must always be included in `GetAllAsync` results regardless
  of which user is querying.
- `AnyDefaultsExistAsync` must not filter on `IsDeleted` — it checks existence of the seed data
  record, not whether it is currently visible.

---

### Task 2.7 — Implement `MfaChallengeRepository`

**What:** Create `src/FinBoardUltra.Infrastructure/Persistence/Repositories/MfaChallengeRepository.cs`
implementing `IMfaChallengeRepository`.

| Method | Behaviour |
|---|---|
| `GetByIdAsync(Guid id)` | Returns the challenge regardless of expiry or used status — validity is checked by the service layer. |
| `AddAsync(MfaChallenge challenge)` | Sets `Id` and `CreatedAt`; saves. |
| `UpdateAsync(MfaChallenge challenge)` | Marks modified; saves (used when setting `UsedAt`). |

**Acceptance criterion:** Class compiles; all three methods are implemented.

**Constraints:**
- Do not filter on `UsedAt` or `ExpiresAt` in the repository — that logic belongs in
  `AuthService`, not here.

---

### Task 2.8 — Implement `UserSessionRepository`

**What:** Create `src/FinBoardUltra.Infrastructure/Persistence/Repositories/UserSessionRepository.cs`
implementing `IUserSessionRepository`.

| Method | Behaviour |
|---|---|
| `GetByTokenAsync(string token)` | Returns the session for that exact token string, or `null` if not found. Does not filter on `IsRevoked` or `ExpiresAt`. |
| `AddAsync(UserSession session)` | Sets `Id` and `CreatedAt`; saves. |
| `UpdateAsync(UserSession session)` | Marks modified; saves. |
| `RevokeAllForUserAsync(Guid userId)` | Sets `IsRevoked = true` on all sessions for the user where `IsRevoked == false`; saves in a single `ExecuteUpdateAsync` call. |

**Acceptance criterion:** Class compiles; all four methods are implemented.

**Constraints:**
- `GetByTokenAsync` must not apply any validity checks — `AuthService.ValidateSessionAsync`
  is the single place where expiry and revocation are evaluated.
- `RevokeAllForUserAsync` must use a bulk update (not load-and-loop) to avoid race conditions.

---

### Task 2.9 — Implement `AuditLogRepository`

**What:** Create `src/FinBoardUltra.Infrastructure/Persistence/Repositories/AuditLogRepository.cs`
implementing `IAuditLogRepository`.

| Method | Behaviour |
|---|---|
| `AddAsync(AuditLog entry)` | Sets `Id` and `Timestamp = DateTime.UtcNow`; adds and saves. Rows are never updated or deleted. |
| `CountRecentFailedLoginsAsync(string? ipAddress, DateTime since)` | Returns the count of `AuditLog` rows where `Action == AuditAction.LoginFailed`, `Timestamp >= since`, and (if `ipAddress` is non-null) `IpAddress == ipAddress`. |

**Acceptance criterion:** Class compiles; both methods are implemented.

**Constraints:**
- `AddAsync` must never call `UpdateAsync` or `Remove` on any row.
- The `CountRecentFailedLoginsAsync` query must be a database-side count (`.CountAsync()`),
  not a client-side `Count()` on a loaded list.

---

### Task 2.10 — Implement `BcryptPasswordHasher`

**What:** Create `src/FinBoardUltra.Infrastructure/Security/BcryptPasswordHasher.cs`
implementing `IPasswordHasher`.

| Method | Behaviour |
|---|---|
| `Hash(string plaintext)` | Returns `BCrypt.Net.BCrypt.HashPassword(plaintext, workFactor: 12)`. |
| `Verify(string plaintext, string hash)` | Returns `BCrypt.Net.BCrypt.Verify(plaintext, hash)`. |

**Acceptance criterion:** Class compiles and implements `IPasswordHasher` with no `NotImplementedException`.

**Constraints:**
- Work factor must be 12 (not less). This is the security minimum.
- Never log or surface the `plaintext` parameter.

---

### Task 2.11 — Implement `SecureTokenGenerator`

**What:** Create `src/FinBoardUltra.Infrastructure/Security/SecureTokenGenerator.cs`
implementing `ITokenGenerator`.

| Method | Behaviour |
|---|---|
| `Generate()` | Allocates a 32-byte array, fills it with `RandomNumberGenerator.Fill(buffer)`, returns `Convert.ToBase64String(buffer)`. Result is 44 characters (256 bits). |

**Acceptance criterion:** Class compiles and implements `ITokenGenerator`; the generated string
is always 44 characters long (base-64 of 32 bytes).

**Constraints:**
- Must use `System.Security.Cryptography.RandomNumberGenerator` — not `Random` or `Guid`.
- Do not use URL-safe base-64 encoding at this layer (the ConsoleApp stores the token in
  memory; URL safety is an API-layer concern).

---

### Phase 2 verification

```
dotnet build FinBoardUltra.slnx
dotnet ef migrations script \
  --project src/FinBoardUltra.Infrastructure \
  --startup-project src/FinBoardUltra.Infrastructure
```

Expected:
1. `Build succeeded. 0 Warning(s) 0 Error(s)`
2. The migration script output contains `CREATE TABLE` statements for all seven entities.

---

## Phase 3 — Application layer

**Goal:** Implement all six application services and their DTOs. By the end of this phase every
piece of business logic is in place and callable, but nothing is wired to a UI or console yet.

---

### Task 3.1 — Auth DTOs

**What:** Create the four DTO types in `src/FinBoardUltra.Application/Auth/Dtos/`:

| File | Fields |
|---|---|
| `RegisterRequest.cs` | `string Email`, `string Password`, `string Name` |
| `LoginRequest.cs` | `string Email`, `string Password`, `string? IpAddress` |
| `MfaVerifyRequest.cs` | `Guid ChallengeId`, `string Code` |
| `LoginResult.cs` | `bool MfaRequired`, `Guid? ChallengeId`, `string? Token`, `string? RefreshToken` |

**Acceptance criterion:** All four files compile. `LoginResult` has both the MFA-pending path
(`MfaRequired = true`, `ChallengeId` set, tokens null) and the success path (`Token` set)
representable via the same type.

**Constraints:**
- DTOs are plain data carriers — no methods, no logic.
- `Password` in `RegisterRequest` and `LoginRequest` is the plain-text input from the user;
  it is hashed by `AuthService`, never stored as-is.

---

### Task 3.2 — Implement `AuthService`

**What:** Create `src/FinBoardUltra.Application/Auth/AuthService.cs`.

Constructor dependencies: `IUserRepository`, `IUserSessionRepository`, `IMfaChallengeRepository`,
`IAuditLogRepository`, `IPasswordHasher`, `ITokenGenerator`, `ICategoryRepository`.

| Method signature | Behaviour summary |
|---|---|
| `Task<Guid> RegisterAsync(RegisterRequest request)` | Validate non-empty email, password, name. Check email uniqueness (throw `ConflictException` if taken). Hash password. Create and persist `User`. Seed default categories for the new user. Write `AuditLog` (action `Register`). Return new user `Id`. |
| `Task<LoginResult> LoginAsync(LoginRequest request)` | Look up user by email (throw `UnauthorizedException` if not found or deleted). Check rate limit: query `IAuditLogRepository.CountRecentFailedLoginsAsync` for the last 15 minutes; throw `TooManyRequestsException` if count ≥ 5. Verify password hash (throw `UnauthorizedException` on mismatch; write `LoginFailed` audit entry and re-throw). If `MfaEnabled`, generate a six-digit code, create `MfaChallenge` (`ExpiresAt = UtcNow + 10 min`), write `Login` audit entry, return `LoginResult { MfaRequired = true, ChallengeId }`. Otherwise create `UserSession` (expiry 24 h), write `Login` audit entry, return token. |
| `Task<LoginResult> VerifyMfaAsync(MfaVerifyRequest request)` | Load `MfaChallenge` by `ChallengeId` (throw `NotFoundException` if absent). Throw `ValidationException` if expired or already used. Set `UsedAt = UtcNow`. Load user. Create `UserSession`. Return token. |
| `Task LogoutAsync(string token)` | Load session by token (throw `NotFoundException` if absent). Set `IsRevoked = true`. Write `Logout` audit entry. |
| `Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)` | Load user. Verify current password (throw `UnauthorizedException` on mismatch). Hash new password. Update user. Call `IUserSessionRepository.RevokeAllForUserAsync(userId)`. Write `PasswordChanged` audit entry. |
| `Task<Guid> ValidateSessionAsync(string token)` | Load session by token. Throw `UnauthorizedException` if not found, revoked, or expired. Return `session.UserId`. |

**Default categories to seed on registration:**

| Name | Type |
|---|---|
| Salary | Income |
| Freelance | Income |
| Other Income | Income |
| Housing | Expense |
| Food | Expense |
| Transport | Expense |
| Healthcare | Expense |
| Entertainment | Expense |
| Other Expense | Expense |
| Stocks | Investment |
| Crypto | Investment |
| Real Estate | Investment |
| Other Investment | Investment |

Seeding only runs if `ICategoryRepository.AnyDefaultsExistAsync()` returns `false`.
Create categories with `IsDefault = true` and `UserId = null`.

**Acceptance criterion:** Class compiles and all six methods are implemented with no
`NotImplementedException`.

**Constraints:**
- The six-digit MFA code must be generated with `Random.Shared.Next(100000, 1000000).ToString()`.
  It is numeric-only (zero-padded if needed, though the range ensures six digits).
- `ValidateSessionAsync` is the single gate for all protected operations — no other service
  duplicates session validation logic.
- `RegisterAsync` must validate input before touching any repository.

---

### Task 3.3 — Financial Record DTOs

**What:** Create the four DTO types in `src/FinBoardUltra.Application/Records/Dtos/`:

| File | Fields |
|---|---|
| `InvestmentDetailDto.cs` | `string AssetName`, `decimal Quantity`, `decimal PurchasePrice`, `decimal CurrentPrice`, `string? Platform`, `decimal GainLoss` (computed: `(CurrentPrice - PurchasePrice) * Quantity`) |
| `RecordDto.cs` | `Guid Id`, `RecordType Type`, `decimal Amount`, `DateOnly Date`, `Guid CategoryId`, `string? CategoryName`, `string? Note`, `DateTime CreatedAt`, `DateTime UpdatedAt`, `InvestmentDetailDto? InvestmentDetail` |
| `CreateRecordRequest.cs` | `RecordType Type`, `decimal Amount`, `DateOnly Date`, `Guid CategoryId`, `string? Note`, `string? AssetName`, `decimal? Quantity`, `decimal? PurchasePrice`, `decimal? CurrentPrice`, `string? Platform` |
| `UpdateRecordRequest.cs` | Same fields as `CreateRecordRequest` except no `Type` (type cannot be changed after creation) |

**Acceptance criterion:** All four files compile.

**Constraints:**
- `GainLoss` in `InvestmentDetailDto` is a computed property, not a stored value.
- Investment-specific fields in `CreateRecordRequest` are nullable because they are only
  required when `Type == Investment`.

---

### Task 3.4 — Implement `FinancialRecordService`

**What:** Create `src/FinBoardUltra.Application/Records/FinancialRecordService.cs`.

Constructor dependencies: `IFinancialRecordRepository`, `ICategoryRepository`, `IAuditLogRepository`.

| Method signature | Behaviour summary |
|---|---|
| `Task<RecordDto> CreateAsync(Guid userId, CreateRecordRequest request)` | Validate: `Amount > 0` (throw `ValidationException`); `CategoryId` non-empty; category exists and is accessible to the user (throw `ValidationException` if not); category `Type` matches record `Type` (throw `ValidationException` if not); if `Type == Investment`, all four investment fields (`AssetName`, `Quantity`, `PurchasePrice`, `CurrentPrice`) must be non-null and `Quantity > 0`, `PurchasePrice > 0`, `CurrentPrice > 0` (throw `ValidationException`). Create `FinancialRecord`; if Investment, create and attach `InvestmentDetail`. Write `RecordCreated` audit entry. Return `RecordDto`. |
| `Task<RecordDto> GetByIdAsync(Guid userId, Guid recordId)` | Load record (throw `NotFoundException` if absent, deleted, or wrong user). Return `RecordDto`. |
| `Task<IReadOnlyList<RecordDto>> GetAllAsync(Guid userId, RecordFilter? filter)` | Return all non-deleted records for user, filtered by `RecordFilter` if supplied, ordered by `Date` descending. |
| `Task<RecordDto> UpdateAsync(Guid userId, Guid recordId, UpdateRecordRequest request)` | Load record (throw `NotFoundException` if absent or wrong user). Apply same field validations as `CreateAsync` (excluding Type check). Update entity fields. If Investment, update `InvestmentDetail` fields. Write `RecordUpdated` audit entry. Return updated `RecordDto`. |
| `Task DeleteAsync(Guid userId, Guid recordId)` | Load record (throw `NotFoundException` if absent or wrong user). Set `IsDeleted = true`. Call `UpdateAsync` on repository. Write `RecordDeleted` audit entry. |

**Acceptance criterion:** Class compiles; all five methods implemented with no `NotImplementedException`.

**Constraints:**
- Hard deletes are absolutely forbidden — `DeleteAsync` must only set `IsDeleted = true`.
- Category accessibility check: the category must either have `UserId == userId` (user-owned)
  or `UserId == null` (system default). A category belonging to a different user is invalid.
- Do not change `RecordType` on update — the type is immutable after creation.

---

### Task 3.5 — Category DTOs and `CategoryService`

**What:** Create DTOs in `src/FinBoardUltra.Application/Categories/Dtos/` and the service.

DTOs:

| File | Fields |
|---|---|
| `CreateCategoryRequest.cs` | `string Name`, `RecordType Type` |
| `CategoryDto.cs` | `Guid Id`, `Guid? UserId`, `string Name`, `RecordType Type`, `bool IsDefault` |

Service — create `src/FinBoardUltra.Application/Categories/CategoryService.cs`:

Constructor dependencies: `ICategoryRepository`.

| Method signature | Behaviour summary |
|---|---|
| `Task<CategoryDto> CreateAsync(Guid userId, CreateCategoryRequest request)` | Validate `Name` is non-empty (throw `ValidationException`). Load existing categories for the user+type; throw `ConflictException` if a category with the same name (case-insensitive) and type already exists for this user. Create `Category` with `UserId = userId`, `IsDefault = false`. Return `CategoryDto`. |
| `Task<IReadOnlyList<CategoryDto>> GetAllAsync(Guid userId, RecordType? type)` | Return all non-deleted categories for the user (user-owned plus system defaults). |
| `Task<CategoryDto> UpdateAsync(Guid userId, Guid categoryId, string newName)` | Load category (throw `NotFoundException` if absent). Throw `UnauthorizedException` if `category.UserId != userId` (cannot rename system defaults). Validate `newName` non-empty. Update name. Return `CategoryDto`. |
| `Task DeleteAsync(Guid userId, Guid categoryId)` | Load category (throw `NotFoundException` if absent). Throw `UnauthorizedException` if `category.UserId != userId`. Set `IsDeleted = true`. Save. |

**Acceptance criterion:** All DTO files and the service class compile; all four service methods implemented.

**Constraints:**
- System defaults (`UserId == null`) can be read by everyone but modified or deleted by no one.
- `CreateAsync` must not allow duplicate category names (case-insensitive) for the same user and type.

---

### Task 3.6 — Dashboard DTOs and `DashboardService`

**What:** Create DTOs in `src/FinBoardUltra.Application/Dashboard/Dtos/` and the service.

DTOs:

| File | Fields |
|---|---|
| `CategoryBreakdownDto.cs` | `string CategoryName`, `decimal TotalAmount` |
| `DashboardSummaryDto.cs` | `decimal CurrentBalance`, `decimal MonthlyIncome`, `decimal MonthlyExpenses`, `decimal TotalInvestmentValue`, `IReadOnlyList<RecordDto> RecentTransactions`, `IReadOnlyList<CategoryBreakdownDto> CategoryBreakdown` |

Service — create `src/FinBoardUltra.Application/Dashboard/DashboardService.cs`:

Constructor dependencies: `IFinancialRecordRepository`.

| Method signature | Behaviour summary |
|---|---|
| `Task<DashboardSummaryDto> GetSummaryAsync(Guid userId)` | Compute all figures from live data with no caching. `CurrentBalance = SumByType(Income, all time) - SumByType(Expense, all time)`. `MonthlyIncome = SumByType(Income, first-to-last day of current month)`. `MonthlyExpenses = SumByType(Expense, same window)`. `TotalInvestmentValue = sum of CurrentPrice × Quantity across all non-deleted Investment records`. `RecentTransactions = GetRecentAsync(userId, 10)` mapped to `RecordDto`. `CategoryBreakdown` = per-category sum of amounts for the current month, sorted by total descending. |

**Acceptance criterion:** Both DTO files and the service compile; `GetSummaryAsync` implemented.

**Constraints:**
- Investment records are excluded from the balance calculation (`CurrentBalance`).
- All monetary aggregations use `decimal` arithmetic — no `double` or `float`.
- Dashboard figures are never cached or stored; every call recomputes from the repository.

---

### Task 3.7 — Report DTOs and `ReportService`

**What:** Create DTOs in `src/FinBoardUltra.Application/Reports/Dtos/` and the service.

DTOs:

| File | Fields |
|---|---|
| `FilterRequest.cs` | `DateOnly? DateFrom`, `DateOnly? DateTo`, `RecordType? Type`, `Guid? CategoryId` |
| `PeriodSummaryDto.cs` | `DateOnly PeriodStart`, `DateOnly PeriodEnd`, `decimal TotalIncome`, `decimal TotalExpenses`, `decimal NetBalance` |
| `InvestmentPositionDto.cs` | `Guid RecordId`, `string AssetName`, `string? Platform`, `decimal Quantity`, `decimal PurchasePrice`, `decimal CurrentPrice`, `decimal TotalCost`, `decimal CurrentValue`, `decimal GainLoss`, `decimal GainLossPercent` |
| `InvestmentPerformanceDto.cs` | `IReadOnlyList<InvestmentPositionDto> Positions`, `decimal TotalCost`, `decimal TotalValue`, `decimal TotalGainLoss`, `decimal TotalGainLossPercent` |

Service — create `src/FinBoardUltra.Application/Reports/ReportService.cs`:

Constructor dependencies: `IFinancialRecordRepository`.

| Method signature | Behaviour summary |
|---|---|
| `Task<IReadOnlyList<RecordDto>> GetFilteredAsync(Guid userId, FilterRequest filter)` | Translate `FilterRequest` to `RecordFilter`; call `IFinancialRecordRepository.GetAllAsync`; map to `RecordDto` list. |
| `Task<IReadOnlyList<PeriodSummaryDto>> GetByPeriodAsync(Guid userId, PeriodType period, DateOnly from, DateOnly to)` | Divide the date range into consecutive buckets of the given period. For each bucket load records and sum income and expenses. Include buckets with no records (zeroed). Return list ordered by `PeriodStart` ascending. |
| `Task<InvestmentPerformanceDto> GetInvestmentPerformanceAsync(Guid userId)` | Load all non-deleted Investment records. Map each to `InvestmentPositionDto` computing `TotalCost = PurchasePrice × Quantity`, `CurrentValue = CurrentPrice × Quantity`, `GainLoss = CurrentValue - TotalCost`, `GainLossPercent = GainLoss / TotalCost × 100` (0 if TotalCost is zero). Aggregate totals. |

**Acceptance criterion:** All DTO files and the service compile; all three methods implemented.

**Constraints:**
- `GetByPeriodAsync` must include empty buckets (zero values) so callers can plot continuous charts.
- Division for `GainLossPercent` must guard against `TotalCost == 0` to prevent `DivideByZeroException`.
- `FilterRequest` (Application DTO) is separate from `RecordFilter` (Domain query type).
  `ReportService` must translate between the two — do not expose domain query types in Application DTOs.

---

### Task 3.8 — Profile DTOs and `UserProfileService`

**What:** Create the DTO in `src/FinBoardUltra.Application/Profile/Dtos/` and the service.

DTO:

| File | Fields |
|---|---|
| `UpdateProfileRequest.cs` | `string? Name`, `string? PhoneNumber`, `string? PreferredCurrency` |

Service — create `src/FinBoardUltra.Application/Profile/UserProfileService.cs`:

Constructor dependencies: `IUserRepository`, `IAuditLogRepository`.

| Method signature | Behaviour summary |
|---|---|
| `Task UpdateProfileAsync(Guid userId, UpdateProfileRequest request)` | Load user (throw `NotFoundException` if absent). If `request.PhoneNumber` is an empty string and `user.MfaEnabled == true`, throw `ValidationException` ("Cannot remove phone number while MFA is enabled"). Apply non-null fields from the request. Save. Write `ProfileUpdated` audit entry. |
| `Task EnableMfaAsync(Guid userId)` | Load user (throw `NotFoundException` if absent). Throw `ValidationException` if `user.PhoneNumber` is null or whitespace. Set `MfaEnabled = true`. Save. Write `MfaEnabled` audit entry. |
| `Task DisableMfaAsync(Guid userId)` | Load user (throw `NotFoundException` if absent). Set `MfaEnabled = false`. Save. Write `MfaDisabled` audit entry. |

**Acceptance criterion:** DTO and service compile; all three methods implemented.

**Constraints:**
- `UpdateProfileAsync` must apply only fields that are non-null in the request — a null field
  means "do not change this value", not "set to null".
- `EnableMfaAsync` must check `string.IsNullOrWhiteSpace(user.PhoneNumber)`.

---

### Phase 3 verification

```
dotnet build FinBoardUltra.slnx
```

Expected: `Build succeeded. 0 Warning(s) 0 Error(s)`

All six services must be fully implemented — no `NotImplementedException` anywhere in the
Application project.

---

## Phase 4 — Console runner and end-to-end smoke test

**Goal:** Wire the application together with a DI container and implement the full menu system.
By the end of this phase the application runs, accepts user input, persists data to a SQLite file,
and exercises every service path at least once manually.

---

### Task 4.1 — Add NuGet packages to the ConsoleApp project

**What:** Install the required packages:

```
dotnet add src/FinBoardUltra.ConsoleApp package Microsoft.Extensions.DependencyInjection
dotnet add src/FinBoardUltra.ConsoleApp package Microsoft.EntityFrameworkCore.Sqlite --version 9.*
```

**Acceptance criterion:** `dotnet restore` succeeds; packages appear in the `ConsoleApp.csproj`.

---

### Task 4.2 — Bootstrap DI container and apply migrations in `Program.cs`

**What:** Replace the default `Program.cs` content with startup code that:

1. Builds a `ServiceCollection`.
2. Registers `AppDbContext` with the SQLite provider pointing at `finboard.db` in the
   working directory.
3. Registers all six repository implementations against their interfaces (scoped lifetime).
4. Registers `BcryptPasswordHasher` as `IPasswordHasher` (singleton).
5. Registers `SecureTokenGenerator` as `ITokenGenerator` (singleton).
6. Registers all six application services (scoped lifetime).
7. Builds the `ServiceProvider`.
8. Resolves `AppDbContext` and calls `context.Database.MigrateAsync()` to apply pending migrations.
9. Calls the seeder (`AuthService.RegisterAsync` checks for defaults via `CategoryRepository`).
10. Enters the main menu loop.

**Acceptance criterion:** `dotnet run --project src/FinBoardUltra.ConsoleApp` starts without
exceptions and the file `finboard.db` is created in the working directory.

**Constraints:**
- Use `IServiceScope` for all service resolution — do not resolve scoped services directly
  from the root container.
- The database file path must be configurable by changing one string constant, not scattered
  across multiple files.

---

### Task 4.3 — Implement `ConsoleSession` state holder

**What:** Create `src/FinBoardUltra.ConsoleApp/ConsoleSession.cs` — a simple in-memory object
that holds the current session token and derived user context for the lifetime of the process.

```csharp
// Fields:
string? Token          // null when no user is logged in
Guid? UserId           // populated after successful login
bool IsAuthenticated   // computed: Token is not null
void Clear()           // resets both fields (called on logout)
```

**Acceptance criterion:** Class compiles; `IsAuthenticated` returns `false` when `Token` is null.

**Constraints:**
- `ConsoleSession` is registered as a singleton in DI.
- It must never be persisted — it lives only for the duration of the process.

---

### Task 4.4 — Implement `AuthMenu`

**What:** Create `src/FinBoardUltra.ConsoleApp/Menus/AuthMenu.cs` with two entry points:

- `Task RegisterAsync()` — prompts for name, email, and password; calls `AuthService.RegisterAsync`;
  prints success or error message.
- `Task<bool> LoginAsync()` — prompts for email and password; calls `AuthService.LoginAsync`;
  if `MfaRequired`, prompts for the MFA code and calls `VerifyMfaAsync`; on success stores token
  in `ConsoleSession`; returns `true` on success.

Errors from domain exceptions must be caught and displayed as plain messages, not stack traces.

**Acceptance criterion:** Running the app and choosing Register then Login completes without
exception; `ConsoleSession.IsAuthenticated` is `true` after successful login.

---

### Task 4.5 — Implement `MainMenu`

**What:** Create `src/FinBoardUltra.ConsoleApp/Menus/MainMenu.cs` with the menu loop:

```
[1] Register
[2] Login
[0] Exit

(after login)
[1] Dashboard
[2] Financial Records
[3] Categories
[4] Reports
[5] Profile & Security
[0] Logout
```

Each post-login option delegates to the appropriate sub-menu. Selecting Logout calls
`AuthService.LogoutAsync(ConsoleSession.Token)` and clears `ConsoleSession`.

**Acceptance criterion:** Menu renders correctly; all options route to the right sub-menu or action.

---

### Task 4.6 — Implement `DashboardMenu`

**What:** Create `src/FinBoardUltra.ConsoleApp/Menus/DashboardMenu.cs`.

Calls `DashboardService.GetSummaryAsync(userId)` and prints:
- Current balance
- Monthly income and expenses
- Total investment value
- Last 10 transactions as a formatted table (date, type, amount, category)
- Category breakdown for the current month

**Acceptance criterion:** After creating at least one record, the dashboard displays non-zero values.

---

### Task 4.7 — Implement `RecordsMenu`

**What:** Create `src/FinBoardUltra.ConsoleApp/Menus/RecordsMenu.cs` with options:

```
[1] List all records
[2] Filter records
[3] Create record
[4] Edit record
[5] Delete record
[0] Back
```

- **List:** calls `GetAllAsync` and prints a table.
- **Filter:** prompts for optional date range, type, and category; calls `GetFilteredAsync`.
- **Create:** prompts for type, amount, date, category; if Investment, prompts for investment fields;
  calls `CreateAsync`.
- **Edit:** prompts for record ID and new values; calls `UpdateAsync`.
- **Delete:** prompts for record ID; confirms; calls `DeleteAsync`.

**Acceptance criterion:** Creating a record, listing it, editing it, and deleting it all succeed
without exceptions; the deleted record does not appear in the list.

---

### Task 4.8 — Implement `CategoriesMenu`

**What:** Create `src/FinBoardUltra.ConsoleApp/Menus/CategoriesMenu.cs` with options:

```
[1] List categories
[2] Create category
[3] Rename category
[4] Delete category
[0] Back
```

**Acceptance criterion:** Creating a category and listing it shows it alongside system defaults.
Attempting to delete a system default displays a meaningful error message.

---

### Task 4.9 — Implement `ReportsMenu`

**What:** Create `src/FinBoardUltra.ConsoleApp/Menus/ReportsMenu.cs` with options:

```
[1] Period summary (weekly / monthly / yearly)
[2] Investment performance
[0] Back
```

- **Period summary:** prompts for period type and date range; calls `GetByPeriodAsync`; prints a table
  of period buckets including empty ones.
- **Investment performance:** calls `GetInvestmentPerformanceAsync`; prints per-position breakdown
  and aggregate totals.

**Acceptance criterion:** Both options print formatted output without exceptions.

---

### Task 4.10 — Implement `ProfileMenu`

**What:** Create `src/FinBoardUltra.ConsoleApp/Menus/ProfileMenu.cs` with options:

```
[1] Update profile (name / phone / currency)
[2] Change password
[3] Enable MFA
[4] Disable MFA
[0] Back
```

**Acceptance criterion:** Updating the profile and changing the password complete without
exceptions. Enabling MFA without a phone number displays the validation error.

---

### Task 4.11 — End-to-end smoke test (manual)

**What:** Run the application and exercise every major path in sequence:

1. Register a new user.
2. Log in.
3. Create one Income record, one Expense record, and one Investment record.
4. View the Dashboard — verify balance, monthly figures, and investment value are non-zero.
5. Filter records by type.
6. View Period Summary (monthly).
7. View Investment Performance.
8. Create a custom category, then create a record using it.
9. Delete a record; verify it no longer appears in the list.
10. Update profile (add phone number).
11. Enable MFA.
12. Log out.
13. Log in again — verify the MFA code prompt appears.
14. Enter the MFA code displayed in the console (printed in place of SMS for now).
15. Verify successful login.

**Acceptance criterion:** All 15 steps complete without unhandled exceptions. `finboard.db` exists
and is non-empty after the session.

**Constraints:**
- Since SMS delivery is not implemented, the MFA code must be printed to the console immediately
  after the challenge is created (prefixed with a clear label such as `[DEV] MFA code: XXXXXX`).
  This is acceptable only at this layer — remove the print when a real SMS adapter is added.

---

### Phase 4 verification

```
dotnet run --project src/FinBoardUltra.ConsoleApp
```

Complete the smoke test checklist in Task 4.11. All 15 steps pass with no unhandled exceptions.

---

## Phase 5 — Unit tests

**Goal:** Every application service has full branch coverage via isolated unit tests using Moq.
No database, no filesystem, no network is touched.

---

### Task 5.1 — Add Moq to the unit test project

**What:**

```
dotnet add tests/FinBoardUltra.Tests.Unit package Moq
```

**Acceptance criterion:** `dotnet restore` succeeds; Moq appears in the `.csproj`.

---

### Task 5.2 — `AuthServiceTests`

**What:** Create `tests/FinBoardUltra.Tests.Unit/Auth/AuthServiceTests.cs`.

| Test | Scenario | Expected outcome |
|---|---|---|
| `Register_WithValidInput_ReturnsNewUserId` | All fields valid, email not taken | Returns a non-empty `Guid` |
| `Register_WithDuplicateEmail_ThrowsConflictException` | `IUserRepository.GetByEmailAsync` returns an existing user | Throws `ConflictException` |
| `Login_WithUnknownEmail_ThrowsUnauthorizedException` | `GetByEmailAsync` returns `null` | Throws `UnauthorizedException` |
| `Login_WithWrongPassword_ThrowsUnauthorizedException` | `IPasswordHasher.Verify` returns `false` | Throws `UnauthorizedException` |
| `Login_MfaDisabled_ReturnsToken` | Correct credentials, `MfaEnabled = false` | Returns `LoginResult` with `Token` set and `MfaRequired = false` |
| `Login_MfaEnabled_ReturnsChallengeId` | Correct credentials, `MfaEnabled = true` | Returns `LoginResult` with `MfaRequired = true` and `ChallengeId` set |
| `VerifyMfa_WithValidCode_ReturnsToken` | Challenge is valid (not used, not expired) | Returns `LoginResult` with `Token` set |
| `VerifyMfa_WithExpiredCode_ThrowsValidationException` | `ExpiresAt < UtcNow` | Throws `ValidationException` |
| `VerifyMfa_WithUsedCode_ThrowsValidationException` | `UsedAt != null` | Throws `ValidationException` |
| `Login_RateLimitExceeded_ThrowsTooManyRequestsException` | `CountRecentFailedLoginsAsync` returns 5 | Throws `TooManyRequestsException` before checking password |
| `ChangePassword_ValidCurrentPassword_RevokesAllSessions` | Correct current password | `RevokeAllForUserAsync` is called exactly once |
| `ValidateSession_ExpiredToken_ThrowsUnauthorizedException` | `ExpiresAt < UtcNow` | Throws `UnauthorizedException` |
| `ValidateSession_RevokedToken_ThrowsUnauthorizedException` | `IsRevoked = true` | Throws `UnauthorizedException` |

**Acceptance criterion:** All 13 tests pass with `dotnet test tests/FinBoardUltra.Tests.Unit`.

**Constraints:**
- Every repository and service dependency must be mocked with `Mock<T>` — no real implementations.
- Tests must be independent — no shared state between test methods.

---

### Task 5.3 — `FinancialRecordServiceTests`

**What:** Create `tests/FinBoardUltra.Tests.Unit/Records/FinancialRecordServiceTests.cs`.

| Test | Scenario | Expected outcome |
|---|---|---|
| `Create_WithZeroAmount_ThrowsValidationException` | `Amount = 0` | Throws `ValidationException` |
| `Create_WithNegativeAmount_ThrowsValidationException` | `Amount = -1` | Throws `ValidationException` |
| `Create_WithInaccessibleCategory_ThrowsValidationException` | Category belongs to a different user | Throws `ValidationException` |
| `Create_InvestmentType_WithoutInvestmentFields_ThrowsValidationException` | `Type = Investment`, `AssetName = null` | Throws `ValidationException` |
| `Create_ValidRecord_WritesAuditLog` | All fields valid | `IAuditLogRepository.AddAsync` called with `RecordCreated` |
| `GetById_RecordBelongsToDifferentUser_ThrowsNotFoundException` | `GetByIdAsync` returns `null` (repo enforces ownership) | Throws `NotFoundException` |
| `Delete_ValidRecord_SetsIsDeletedTrue` | Normal delete | `IFinancialRecordRepository.UpdateAsync` called; `IsDeleted = true` on the entity |
| `Delete_ValidRecord_WritesAuditLog` | Normal delete | `IAuditLogRepository.AddAsync` called with `RecordDeleted` |

**Acceptance criterion:** All 8 tests pass.

---

### Task 5.4 — `CategoryServiceTests`

**What:** Create `tests/FinBoardUltra.Tests.Unit/Categories/CategoryServiceTests.cs`.

| Test | Scenario | Expected outcome |
|---|---|---|
| `Create_ValidNameAndType_AddsCategoryToRepository` | Non-duplicate name | `ICategoryRepository.AddAsync` called once |
| `Create_DuplicateName_ThrowsConflictException` | Existing category with same name and type for user | Throws `ConflictException` |
| `Delete_SystemDefaultCategory_ThrowsUnauthorizedException` | `category.UserId == null` | Throws `UnauthorizedException` |

**Acceptance criterion:** All 3 tests pass.

---

### Task 5.5 — `DashboardServiceTests`

**What:** Create `tests/FinBoardUltra.Tests.Unit/Dashboard/DashboardServiceTests.cs`.

| Test | Scenario | Expected outcome |
|---|---|---|
| `GetSummary_BalanceIsIncomeMínusExpense` | Income = 1000, Expense = 300 | `CurrentBalance = 700` |
| `GetSummary_MonthlyFiguresUseCurrentMonthOnly` | Records from last month and this month | Monthly figures reflect only the current month |
| `GetSummary_InvestmentValueIsSumOfCurrentPriceTimesQuantity` | Two investment records | `TotalInvestmentValue = sum of (CurrentPrice × Quantity)` |
| `GetSummary_ReturnsMaxTenRecentTransactions` | Repo returns 10 records | `RecentTransactions.Count == 10` |

**Acceptance criterion:** All 4 tests pass.

---

### Task 5.6 — `ReportServiceTests`

**What:** Create `tests/FinBoardUltra.Tests.Unit/Reports/ReportServiceTests.cs`.

| Test | Scenario | Expected outcome |
|---|---|---|
| `GetFiltered_AppliesTypeFilter` | Filter by `Type = Income` | Only Income records in result |
| `GetByPeriod_IncludesEmptyBuckets` | Date range spans three months, records only in one | Result contains three `PeriodSummaryDto` entries; two are zeroed |
| `GetInvestmentPerformance_ComputesGainLossCorrectly` | `PurchasePrice = 100`, `CurrentPrice = 150`, `Quantity = 2` | `GainLoss = 100`, `GainLossPercent = 50` |

**Acceptance criterion:** All 3 tests pass.

---

### Task 5.7 — `UserProfileServiceTests`

**What:** Create `tests/FinBoardUltra.Tests.Unit/Profile/UserProfileServiceTests.cs`.

| Test | Scenario | Expected outcome |
|---|---|---|
| `EnableMfa_WithoutPhoneNumber_ThrowsValidationException` | `PhoneNumber = null` | Throws `ValidationException` |
| `EnableMfa_WithBlankPhoneNumber_ThrowsValidationException` | `PhoneNumber = "  "` | Throws `ValidationException` |
| `UpdateProfile_ClearPhoneWhileMfaEnabled_ThrowsValidationException` | `MfaEnabled = true`, `request.PhoneNumber = ""` | Throws `ValidationException` |

**Acceptance criterion:** All 3 tests pass.

---

### Phase 5 verification

```
dotnet test tests/FinBoardUltra.Tests.Unit --verbosity normal
```

Expected: all tests pass, zero failures, zero skipped.

---

## Phase 6 — Integration tests

**Goal:** Every repository implementation is exercised against a real EF Core `DbContext` backed
by the in-memory provider. No mocks in this phase.

---

### Task 6.1 — Add EF Core InMemory package to the integration test project

**What:**

```
dotnet add tests/FinBoardUltra.Tests.Integration package Microsoft.EntityFrameworkCore.InMemory --version 9.*
```

**Acceptance criterion:** `dotnet restore` succeeds; package appears in the `.csproj`.

---

### Task 6.2 — Create a test `DbContext` factory helper

**What:** Create `tests/FinBoardUltra.Tests.Integration/DbContextFactory.cs`.

```csharp
// Returns a fresh AppDbContext backed by an in-memory database with a unique name
// so each test method gets a clean, isolated store.
public static AppDbContext Create(string? dbName = null)
```

Uses `DbContextOptionsBuilder<AppDbContext>()
    .UseInMemoryDatabase(dbName ?? Guid.NewGuid().ToString())`.

**Acceptance criterion:** Factory compiles and returns a non-null `AppDbContext`.

**Constraints:**
- Each call with no argument must create a distinct database — never share state between tests.

---

### Task 6.3 — `UserRepositoryTests`

**What:** Create `tests/FinBoardUltra.Tests.Integration/Persistence/UserRepositoryTests.cs`.

| Test | Scenario | Expected outcome |
|---|---|---|
| `AddAndGetById_ReturnsCorrectUser` | Add user, retrieve by Id | Retrieved user has same email and name |
| `GetByEmail_CaseInsensitive_ReturnsUser` | Add user with lowercase email, retrieve with uppercase | Returns the user |
| `GetByEmail_SoftDeletedUser_ReturnsNull` | Add user with `IsDeleted = true`, retrieve by email | Returns `null` |
| `UpdateAsync_PersistsChanges` | Add user, update name, retrieve again | Retrieved user has new name |

**Acceptance criterion:** All 4 tests pass.

---

### Task 6.4 — `FinancialRecordRepositoryTests`

**What:** Create `tests/FinBoardUltra.Tests.Integration/Persistence/FinancialRecordRepositoryTests.cs`.

| Test | Scenario | Expected outcome |
|---|---|---|
| `AddAndGetById_ReturnsRecord` | Add record, retrieve by userId + recordId | Retrieved record matches |
| `GetById_WrongUserId_ReturnsNull` | Add record for user A, retrieve with user B's Id | Returns `null` |
| `GetAll_ExcludesSoftDeletedRecords` | Add two records; soft-delete one | `GetAllAsync` returns only one |
| `GetAll_SoftDeletedRecord_StillExistsInDbSet` | Soft-delete a record | Raw `context.FinancialRecords.FindAsync` still finds it |
| `GetAll_AppliesTypeFilter` | Add Income and Expense records | Filtering by `Type = Income` returns only Income records |
| `SumByType_ReturnsCorrectTotal` | Add three Income records with known amounts | `SumByTypeAsync` returns the correct sum |
| `GetRecent_ReturnsCorrectCount` | Add 15 records | `GetRecentAsync(userId, 10)` returns exactly 10 |

**Acceptance criterion:** All 7 tests pass.

---

### Task 6.5 — `CategoryRepositoryTests`

**What:** Create `tests/FinBoardUltra.Tests.Integration/Persistence/CategoryRepositoryTests.cs`.

| Test | Scenario | Expected outcome |
|---|---|---|
| `GetAll_IncludesSystemDefaults` | Add one system default (`UserId = null`) and one user-owned | `GetAllAsync` returns both |
| `GetAll_ExcludesOtherUsersCategories` | Add categories for user A and user B | User A's query does not return user B's category |
| `GetAll_FiltersByType` | Add Income and Expense categories | Filtering by `Income` returns only Income categories |
| `AnyDefaultsExist_WhenDefaultPresent_ReturnsTrue` | Add a category with `IsDefault = true` | `AnyDefaultsExistAsync()` returns `true` |
| `AnyDefaultsExist_WhenDefaultSoftDeleted_StillReturnsTrue` | Add default, soft-delete it | `AnyDefaultsExistAsync()` still returns `true` (seeder must not re-seed) |

**Acceptance criterion:** All 5 tests pass.

---

### Phase 6 verification

```
dotnet test FinBoardUltra.slnx --verbosity normal
```

Expected: all tests across both test projects pass, zero failures, zero skipped.

---

## Final state checklist

When all six phases are complete and all verifications pass, the native component satisfies the
following properties drawn from CLAUDE.md:

| Requirement | Satisfied by |
|---|---|
| User data isolation | Every service method scoped to `userId`; repositories filter on `UserId` |
| Soft deletes only | `FinancialRecordService.DeleteAsync` sets `IsDeleted`, never calls hard delete |
| Dashboard live aggregation | `DashboardService` has no cache; all figures computed on every call |
| MFA requires phone number | `UserProfileService.EnableMfaAsync` validates `PhoneNumber` before setting flag |
| MFA code single-use, 10-minute expiry | `AuthService.VerifyMfaAsync` checks `UsedAt` and `ExpiresAt` |
| Passwords as one-way hash | `IPasswordHasher` / `BcryptPasswordHasher` with work factor 12 |
| Token-based auth with expiry | `UserSession.ExpiresAt` checked in `ValidateSessionAsync` |
| Rate limiting on login | `CountRecentFailedLoginsAsync` gates `LoginAsync` at threshold 5 / 15 min |
| Audit log on critical actions | All 14 `AuditAction` values written by corresponding service methods |

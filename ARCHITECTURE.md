# Finance Board Ultra – Native Component Architecture

This document describes the architecture of the native component of Finance Board Ultra. The native component implements the complete business logic and domain of the application in pure C#, with no dependency on any UI framework, API layer, or external network service. Every feature described in CLAUDE.md is exercisable from a console runner and verifiable through automated tests. This layer is the foundation on which the desktop UI and any future API surface will be built.

---

## 1. Technology Choices

| Technology | Purpose | Justification |
|---|---|---|
| **.NET 8 (LTS)** | Runtime and base class library | Long-term support until November 2026; cross-platform; enables modern C# 12 features such as primary constructors and collection expressions. |
| **C# 12** | Implementation language | Mandated by CLAUDE.md. Primary constructors reduce boilerplate in entity and DTO definitions. |
| **Entity Framework Core 8** | ORM and database access | Code-first migrations, LINQ-based querying, and strong .NET ecosystem integration. Allows swapping the underlying database provider without changing application code. |
| **SQLite** (via `Microsoft.EntityFrameworkCore.Sqlite`) | Persistent storage | Embedded, file-based, zero-configuration. No server process required, which is appropriate for a standalone desktop application. **Rejected alternative: PostgreSQL** — requires a running server process, which is unsuitable for the current native desktop target. |
| **BCrypt.Net-Next** | Password hashing | Industry-standard adaptive hashing with a configurable work factor and per-password salt. **Rejected alternatives: SHA-256 / MD5** — these are cryptographic hash functions, not password hashing functions; they are too fast and susceptible to brute-force and rainbow table attacks. |
| **xUnit** | Unit and integration test framework | De-facto standard in the .NET ecosystem; superior async/await support compared to NUnit; no global state between tests. |
| **Moq** | Test double / mock library | Fluent API for verifying interaction and stubbing return values on interface dependencies; integrates naturally with xUnit. |
| **Microsoft.EntityFrameworkCore.InMemory** | In-memory EF Core provider for integration tests | Allows repository implementations to be tested against a real `DbContext` without touching the filesystem; faster than SQLite in test runs. |
| **Microsoft.Extensions.DependencyInjection** | IoC container | Ships with .NET; lightweight, widely understood, no additional dependencies. Used in both the console runner and the test harness. |

---

## 2. Project and Folder Structure

The solution uses a layered architecture with strict dependency direction: Domain has no dependencies; Application depends only on Domain; Infrastructure depends only on Domain; ConsoleApp depends on all three. Test projects reference the layers they exercise.

```
FinanceDashboard/
├── FinBoardUltra.sln
├── CLAUDE.md
├── ARCHITECTURE.md
├── FinBoardUltra_spec.md
│
├── src/
│   │
│   ├── FinBoardUltra.Domain/
│   │   ├── FinBoardUltra.Domain.csproj        # No project references; no NuGet dependencies
│   │   ├── Entities/
│   │   │   ├── User.cs                        # User account and credentials
│   │   │   ├── FinancialRecord.cs             # Income, expense, or investment record
│   │   │   ├── InvestmentDetail.cs            # Extra fields for investment records (1:1 with FinancialRecord)
│   │   │   ├── Category.cs                    # User-owned or system-default category
│   │   │   ├── MfaChallenge.cs                # Single-use MFA code with expiry
│   │   │   ├── UserSession.cs                 # Active session / token pair
│   │   │   └── AuditLog.cs                    # Immutable audit event record
│   │   ├── Enums/
│   │   │   ├── RecordType.cs                  # Income | Expense | Investment
│   │   │   ├── PeriodType.cs                  # Weekly | Monthly | Yearly
│   │   │   └── AuditAction.cs                 # Enumeration of all loggable actions
│   │   ├── Interfaces/
│   │   │   ├── Repositories/
│   │   │   │   ├── IUserRepository.cs
│   │   │   │   ├── IFinancialRecordRepository.cs
│   │   │   │   ├── ICategoryRepository.cs
│   │   │   │   ├── IMfaChallengeRepository.cs
│   │   │   │   ├── IUserSessionRepository.cs
│   │   │   │   └── IAuditLogRepository.cs
│   │   │   └── Services/
│   │   │       ├── IPasswordHasher.cs         # Hash and verify; keeps BCrypt out of Application
│   │   │       └── ITokenGenerator.cs         # Generate cryptographically random tokens
│   │   └── Exceptions/
│   │       ├── DomainException.cs             # Base for all domain-originating exceptions
│   │       ├── ValidationException.cs         # Invalid input or broken invariant
│   │       ├── NotFoundException.cs           # Requested resource does not exist or is not visible to user
│   │       ├── UnauthorizedException.cs       # Operation not permitted for this user
│   │       └── ConflictException.cs           # Uniqueness violation (e.g. duplicate email)
│   │
│   ├── FinBoardUltra.Application/
│   │   ├── FinBoardUltra.Application.csproj   # References: Domain
│   │   ├── Auth/
│   │   │   ├── AuthService.cs
│   │   │   └── Dtos/
│   │   │       ├── RegisterRequest.cs
│   │   │       ├── LoginRequest.cs
│   │   │       ├── LoginResult.cs             # Contains session token, or MFA challenge flag
│   │   │       └── MfaVerifyRequest.cs
│   │   ├── Records/
│   │   │   ├── FinancialRecordService.cs
│   │   │   └── Dtos/
│   │   │       ├── CreateRecordRequest.cs
│   │   │       ├── UpdateRecordRequest.cs
│   │   │       ├── RecordDto.cs               # Safe read-only projection of FinancialRecord
│   │   │       └── InvestmentDetailDto.cs
│   │   ├── Categories/
│   │   │   ├── CategoryService.cs
│   │   │   └── Dtos/
│   │   │       ├── CreateCategoryRequest.cs
│   │   │       └── CategoryDto.cs
│   │   ├── Dashboard/
│   │   │   ├── DashboardService.cs
│   │   │   └── Dtos/
│   │   │       ├── DashboardSummaryDto.cs
│   │   │       └── CategoryBreakdownDto.cs
│   │   ├── Reports/
│   │   │   ├── ReportService.cs
│   │   │   └── Dtos/
│   │   │       ├── FilterRequest.cs           # Date range, type, category filters
│   │   │       ├── PeriodSummaryDto.cs        # Aggregated income/expense for one period bucket
│   │   │       └── InvestmentPerformanceDto.cs
│   │   └── Profile/
│   │       ├── UserProfileService.cs
│   │       └── Dtos/
│   │           └── UpdateProfileRequest.cs
│   │
│   ├── FinBoardUltra.Infrastructure/
│   │   ├── FinBoardUltra.Infrastructure.csproj  # References: Domain; NuGet: EF Core, SQLite, BCrypt
│   │   ├── Persistence/
│   │   │   ├── AppDbContext.cs                  # EF Core DbContext; all entity configurations
│   │   │   ├── Migrations/                      # Auto-generated EF Core migration files
│   │   │   └── Repositories/
│   │   │       ├── UserRepository.cs
│   │   │       ├── FinancialRecordRepository.cs
│   │   │       ├── CategoryRepository.cs
│   │   │       ├── MfaChallengeRepository.cs
│   │   │       ├── UserSessionRepository.cs
│   │   │       └── AuditLogRepository.cs
│   │   └── Security/
│   │       ├── BcryptPasswordHasher.cs          # Implements IPasswordHasher using BCrypt.Net-Next
│   │       └── SecureTokenGenerator.cs          # Implements ITokenGenerator using RandomNumberGenerator
│   │
│   └── FinBoardUltra.ConsoleApp/
│       ├── FinBoardUltra.ConsoleApp.csproj      # References: Domain, Application, Infrastructure
│       ├── Program.cs                           # DI bootstrap, database migration, entry point
│       └── Menus/
│           ├── MainMenu.cs                      # Top-level: register / login / exit
│           ├── AuthMenu.cs                      # Register and login flows
│           ├── DashboardMenu.cs                 # Display summary
│           ├── RecordsMenu.cs                   # List, create, edit, delete records
│           ├── CategoriesMenu.cs                # List, create, delete categories
│           ├── ReportsMenu.cs                   # Filtered view, period breakdown, investment performance
│           └── ProfileMenu.cs                   # Update profile, change password, MFA toggle
│
└── tests/
    ├── FinBoardUltra.Tests.Unit/
    │   ├── FinBoardUltra.Tests.Unit.csproj      # References: Application, Domain; NuGet: xUnit, Moq
    │   ├── Auth/
    │   │   └── AuthServiceTests.cs
    │   ├── Records/
    │   │   └── FinancialRecordServiceTests.cs
    │   ├── Categories/
    │   │   └── CategoryServiceTests.cs
    │   ├── Dashboard/
    │   │   └── DashboardServiceTests.cs
    │   ├── Reports/
    │   │   └── ReportServiceTests.cs
    │   └── Profile/
    │       └── UserProfileServiceTests.cs
    │
    └── FinBoardUltra.Tests.Integration/
        ├── FinBoardUltra.Tests.Integration.csproj  # References: Infrastructure, Domain; NuGet: xUnit, EF InMemory
        └── Persistence/
            ├── UserRepositoryTests.cs
            ├── FinancialRecordRepositoryTests.cs
            └── CategoryRepositoryTests.cs
```

---

## 3. Domain Model

### User

Represents an authenticated account. The email address is the unique identifier used for login. The password is never stored — only its BCrypt hash. The phone number is optional but is a prerequisite for enabling MFA.

| Field | Type | Notes |
|---|---|---|
| Id | `Guid` | Primary key, generated on creation |
| Email | `string` | Unique; case-insensitive comparison enforced at service layer |
| PasswordHash | `string` | BCrypt hash; never exposed outside the domain |
| Name | `string` | Display name |
| PhoneNumber | `string?` | Required before MFA can be enabled |
| PreferredCurrency | `string` | ISO 4217 code (e.g. `"USD"`, `"HUF"`); default `"USD"` |
| MfaEnabled | `bool` | Whether MFA is required at login |
| CreatedAt | `DateTime` | UTC; set once on creation |
| IsDeleted | `bool` | Soft-delete flag |

### FinancialRecord

The central entity. Represents any single financial event — money received (Income), money spent (Expense), or an asset purchase (Investment). The `Amount` field is always positive; the `Type` field determines direction.

| Field | Type | Notes |
|---|---|---|
| Id | `Guid` | Primary key |
| UserId | `Guid` | Foreign key → User; all queries filter on this |
| Type | `RecordType` | `Income`, `Expense`, or `Investment` |
| Amount | `decimal` | Always positive; precision (18, 4) |
| Date | `DateOnly` | The date the event occurred |
| CategoryId | `Guid` | Foreign key → Category; required |
| Note | `string?` | Optional free-text annotation |
| IsDeleted | `bool` | Soft-delete flag; set instead of removing the row |
| CreatedAt | `DateTime` | UTC |
| UpdatedAt | `DateTime` | UTC; updated on every edit |

**Relationship**: A record of type `Investment` has exactly one `InvestmentDetail` child (1:1). Records of type `Income` or `Expense` have no `InvestmentDetail`.

### InvestmentDetail

Carries the extra fields that only make sense for investment records. Stored in its own table to keep `FinancialRecord` clean.

| Field | Type | Notes |
|---|---|---|
| Id | `Guid` | Primary key |
| RecordId | `Guid` | Foreign key → FinancialRecord; unique (1:1) |
| AssetName | `string` | Name of the asset (e.g. `"Apple Inc."`, `"Bitcoin"`) |
| Quantity | `decimal` | Number of units held |
| PurchasePrice | `decimal` | Price per unit at purchase time |
| CurrentPrice | `decimal` | Latest known price per unit; updated by the user |
| Platform | `string?` | Brokerage or exchange (e.g. `"Interactive Brokers"`) |

**Computed value**: Gain/loss = `(CurrentPrice - PurchasePrice) * Quantity`. This is calculated at query time, never stored.

### Category

Categories tag financial records by type. System-default categories (where `UserId` is null) are visible to all users. User-created categories are private. Categories are scoped to a `RecordType` so that income categories do not appear when creating an expense record.

| Field | Type | Notes |
|---|---|---|
| Id | `Guid` | Primary key |
| UserId | `Guid?` | Null for system defaults; Guid for user-owned |
| Name | `string` | Display name |
| Type | `RecordType` | The record type this category applies to |
| IsDefault | `bool` | True for system-seeded categories |
| IsDeleted | `bool` | Soft-delete flag |

### MfaChallenge

Records a single issued MFA code. When a user whose account has `MfaEnabled = true` submits correct credentials, an `MfaChallenge` is created (the code is transmitted out-of-band via SMS in later components). The code is single-use and expires after 10 minutes.

| Field | Type | Notes |
|---|---|---|
| Id | `Guid` | Primary key |
| UserId | `Guid` | Foreign key → User |
| Code | `string` | Six-digit random numeric string |
| CreatedAt | `DateTime` | UTC |
| ExpiresAt | `DateTime` | `CreatedAt + 10 minutes` |
| UsedAt | `DateTime?` | Set when the code is successfully verified; null = unused |

A code is valid only when `UsedAt == null && DateTime.UtcNow < ExpiresAt`.

### UserSession

Represents an active authenticated session. Both the session token and the refresh token are opaque 256-bit random strings generated by `ITokenGenerator`. Sessions are never deleted — they are revoked by setting `IsRevoked = true` (required for audit purposes).

| Field | Type | Notes |
|---|---|---|
| Id | `Guid` | Primary key |
| UserId | `Guid` | Foreign key → User |
| Token | `string` | Primary session credential; 256-bit random, base-64 encoded |
| RefreshToken | `string` | Used to issue a new Token when the current one expires |
| CreatedAt | `DateTime` | UTC |
| ExpiresAt | `DateTime` | UTC; configurable, default 24 hours |
| IsRevoked | `bool` | Set on logout or password change |

### AuditLog

An append-only record of security-relevant events. Rows are never updated or deleted. Every critical action in the application services writes an `AuditLog` entry.

| Field | Type | Notes |
|---|---|---|
| Id | `Guid` | Primary key |
| UserId | `Guid?` | Null for pre-authentication events (e.g. failed login with unknown email) |
| Action | `AuditAction` | Enumeration of loggable event types (see below) |
| Timestamp | `DateTime` | UTC; set at the moment the event occurs |
| Details | `string` | Free-text or JSON with event-specific context (e.g. changed field names, IP address placeholder) |

`AuditAction` values: `Register`, `Login`, `LoginFailed`, `Logout`, `PasswordChanged`, `MfaEnabled`, `MfaDisabled`, `MfaChallengeFailed`, `RecordCreated`, `RecordUpdated`, `RecordDeleted`, `CategoryCreated`, `CategoryDeleted`, `ProfileUpdated`.

---

## 4. Core Components

### 4.1 AuthService

**Responsibility**: All authentication and session lifecycle operations: registration, credential verification, MFA challenge issuance and verification, logout, and password change.

**Dependencies**: `IUserRepository`, `IUserSessionRepository`, `IMfaChallengeRepository`, `IAuditLogRepository`, `IPasswordHasher`, `ITokenGenerator`.

**Public surface**:

```
Task<Guid> RegisterAsync(RegisterRequest request)
```
Validates that the email is not already taken (`ConflictException` otherwise). Hashes the password via `IPasswordHasher`. Creates the `User` entity. Seeds default categories for the user. Writes an `AuditLog` entry with action `Register`. Returns the new user's Id.

```
Task<LoginResult> LoginAsync(LoginRequest request)
```
Looks up the user by email. Verifies the password hash. On failure, writes `LoginFailed` to audit log and checks the rolling failure count (last 15 minutes, same IP placeholder) — throws `TooManyRequestsException` if threshold exceeded. On success: if `MfaEnabled`, creates an `MfaChallenge`, writes `Login` (pending MFA) to audit log, and returns a `LoginResult` with `MfaRequired = true` and the challenge Id. Otherwise, creates a `UserSession`, writes `Login` to audit log, and returns the session token.

```
Task<LoginResult> VerifyMfaAsync(MfaVerifyRequest request)
```
Loads the `MfaChallenge` by Id. Validates it is unused and not expired (`ValidationException` otherwise). Sets `UsedAt`. Creates a `UserSession`. Returns the session token.

```
Task LogoutAsync(string token)
```
Loads the `UserSession` by token. Sets `IsRevoked = true`. Writes `Logout` to audit log.

```
Task ChangePasswordAsync(Guid userId, string currentPassword, string newPassword)
```
Loads the user. Verifies `currentPassword` against stored hash. Hashes `newPassword`. Revokes all active sessions for the user (password change invalidates all existing sessions). Writes `PasswordChanged` to audit log.

```
Task<Guid> ValidateSessionAsync(string token)
```
Loads the session by token. Returns the `UserId` if the session exists, is not revoked, and has not expired. Throws `UnauthorizedException` otherwise. Called at the start of every protected service method.

---

### 4.2 FinancialRecordService

**Responsibility**: Create, retrieve, update, and soft-delete financial records. Enforces user isolation — a user can never see or modify another user's records. Validates all required fields and type-specific constraints.

**Dependencies**: `IFinancialRecordRepository`, `ICategoryRepository`, `IAuditLogRepository`.

**Public surface**:

```
Task<RecordDto> CreateAsync(Guid userId, CreateRecordRequest request)
```
Validates: `Type` is set; `Amount > 0`; `Date` is provided; `CategoryId` refers to a category accessible to this user and of matching `Type`. For `Investment` type, validates that `InvestmentDetail` fields are present. Creates the `FinancialRecord` (and `InvestmentDetail` if applicable). Writes `RecordCreated` to audit log. Returns a `RecordDto`.

```
Task<RecordDto> GetByIdAsync(Guid userId, Guid recordId)
```
Loads the record. Throws `NotFoundException` if it does not exist, is soft-deleted, or belongs to a different user.

```
Task<IReadOnlyList<RecordDto>> GetAllAsync(Guid userId, FilterRequest? filter = null)
```
Returns all non-deleted records for the user. Applies optional filters: date range, `RecordType`, and `CategoryId`. Results are ordered by `Date` descending.

```
Task<RecordDto> UpdateAsync(Guid userId, Guid recordId, UpdateRecordRequest request)
```
Validates ownership and existence. Validates updated fields using the same rules as `CreateAsync`. Updates `UpdatedAt`. Writes `RecordUpdated` to audit log.

```
Task DeleteAsync(Guid userId, Guid recordId)
```
Validates ownership and existence. Sets `IsDeleted = true` and updates `UpdatedAt`. Writes `RecordDeleted` to audit log. The row is retained in the database.

---

### 4.3 CategoryService

**Responsibility**: Manage the set of categories available to a user. A user can read system defaults and their own categories. Users cannot modify or delete system defaults.

**Dependencies**: `ICategoryRepository`.

**Public surface**:

```
Task<CategoryDto> CreateAsync(Guid userId, string name, RecordType type)
```
Validates that `name` is non-empty and that the user does not already have a category with the same name and type. Creates the category.

```
Task<IReadOnlyList<CategoryDto>> GetAllAsync(Guid userId, RecordType? type = null)
```
Returns all non-deleted categories visible to the user: those owned by the user plus system defaults (`UserId == null`). Optionally filtered by `RecordType`.

```
Task<CategoryDto> UpdateAsync(Guid userId, Guid categoryId, string newName)
```
Validates ownership (system defaults cannot be renamed). Updates the name.

```
Task DeleteAsync(Guid userId, Guid categoryId)
```
Validates ownership (system defaults cannot be deleted). Sets `IsDeleted = true`. Records that reference this category retain their `CategoryId` — queries handle orphaned categories gracefully by left-joining.

---

### 4.4 DashboardService

**Responsibility**: Produce the live dashboard summary. All figures are computed from records at query time — there is no cached or manually maintained balance. This is a read-only service.

**Dependencies**: `IFinancialRecordRepository`.

**Public surface**:

```
Task<DashboardSummaryDto> GetSummaryAsync(Guid userId)
```
Executes the following aggregations over the user's non-deleted records in a single query pass:
- **Current balance**: sum of all Income amounts minus sum of all Expense amounts (investments are excluded from balance calculation).
- **Monthly income**: sum of Income records whose `Date` falls in the current calendar month.
- **Monthly expenses**: sum of Expense records in the current calendar month.
- **Total investment value**: sum of `CurrentPrice * Quantity` across all Investment records' `InvestmentDetail`.
- **Recent transactions**: the 10 most recent non-deleted records by `Date`, regardless of type.
- **Category breakdown**: for the current month, a list of `(CategoryName, TotalAmount)` pairs, one per category that has at least one record.

`DashboardSummaryDto` contains all of the above as typed fields.

---

### 4.5 ReportService

**Responsibility**: Produce filtered and aggregated views of the user's financial history. Supports date range filtering, type filtering, period-based bucketing, and investment performance analysis. Read-only.

**Dependencies**: `IFinancialRecordRepository`.

**Public surface**:

```
Task<IReadOnlyList<RecordDto>> GetFilteredAsync(Guid userId, FilterRequest filter)
```
Returns records matching the filter. `FilterRequest` fields: `DateFrom?`, `DateTo?`, `Type?` (RecordType), `CategoryId?`. All fields are optional and combined with AND logic.

```
Task<IReadOnlyList<PeriodSummaryDto>> GetByPeriodAsync(
    Guid userId, PeriodType period, DateOnly from, DateOnly to)
```
Divides the date range into buckets of the specified `PeriodType` (Weekly, Monthly, Yearly). For each bucket, returns total income and total expenses. Buckets with no records are included with zero values to preserve continuity in charts.

```
Task<InvestmentPerformanceDto> GetInvestmentPerformanceAsync(Guid userId)
```
Returns all investment records with their computed gain/loss `(CurrentPrice - PurchasePrice) * Quantity`, total portfolio value, and total gain/loss across all positions. `InvestmentPerformanceDto` contains a list of per-record breakdowns and aggregate totals.

---

### 4.6 UserProfileService

**Responsibility**: Update profile information and manage MFA settings.

**Dependencies**: `IUserRepository`, `IAuditLogRepository`.

**Public surface**:

```
Task UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
```
Updates any combination of `Name`, `PhoneNumber`, and `PreferredCurrency`. If `PhoneNumber` is being cleared while `MfaEnabled = true`, throws `ValidationException` (phone number cannot be removed while MFA is active). Writes `ProfileUpdated` to audit log.

```
Task EnableMfaAsync(Guid userId)
```
Loads the user. Throws `ValidationException` if `PhoneNumber` is null or empty. Sets `MfaEnabled = true`. Writes `MfaEnabled` to audit log.

```
Task DisableMfaAsync(Guid userId)
```
Sets `MfaEnabled = false`. Writes `MfaDisabled` to audit log.

---

### 4.7 Repository Interfaces

Each repository interface is defined in `FinBoardUltra.Domain/Interfaces/Repositories/` and contains only the operations the application services require. No repository exposes raw `IQueryable` — all filtering is encapsulated inside the repository implementation. This keeps the application services free from EF Core details.

Example — `IFinancialRecordRepository`:

```
Task<FinancialRecord?> GetByIdAsync(Guid userId, Guid recordId);
Task<IReadOnlyList<FinancialRecord>> GetAllAsync(Guid userId, FilterRequest? filter);
Task AddAsync(FinancialRecord record);
Task UpdateAsync(FinancialRecord record);
Task<decimal> SumByTypeAsync(Guid userId, RecordType type, DateOnly? from, DateOnly? to);
Task<IReadOnlyList<FinancialRecord>> GetRecentAsync(Guid userId, int count);
```

All repository methods return domain entities, never EF Core proxies — entities are mapped to DTOs in the application service layer.

---

### 4.8 Infrastructure Services

**BcryptPasswordHasher** implements `IPasswordHasher`:
- `string Hash(string plaintext)` — calls `BCrypt.HashPassword` with work factor 12.
- `bool Verify(string plaintext, string hash)` — calls `BCrypt.Verify`.

**SecureTokenGenerator** implements `ITokenGenerator`:
- `string Generate()` — fills a 32-byte buffer with `RandomNumberGenerator.Fill`, returns a URL-safe base-64 string (43 characters, 256 bits of entropy).

---

## 5. Console Interface

The console runner (`FinBoardUltra.ConsoleApp`) exists solely to exercise the full application stack end-to-end without any UI framework. It is not intended to be a polished user interface — it is a verification harness and development driver.

### Bootstrap (`Program.cs`)

1. Builds a `ServiceCollection` and registers:
   - `AppDbContext` with the SQLite provider, pointed at `finboard.db` in the working directory.
   - All six repository implementations against their interfaces.
   - `BcryptPasswordHasher` and `SecureTokenGenerator` against their interfaces.
   - All six application services.
2. Applies pending EF Core migrations on startup (ensures schema is current).
3. Seeds system-default categories if none exist.
4. Resolves `MainMenu` from the container and calls `RunAsync()`.

### Session State

After a successful login or MFA verification, the console runner stores the session token in a `ConsoleSession` object held in memory for the lifetime of the process. Every menu action that calls a protected service passes this token to `AuthService.ValidateSessionAsync` first. On logout, the token is cleared.

### Menu Structure

```
Main Menu
├── [1] Register
├── [2] Login
│       └── (if MFA required) Enter MFA code
└── [0] Exit

(After login)
Main Menu
├── [1] View Dashboard
├── [2] Financial Records
│       ├── [1] List all records
│       ├── [2] Filter records (date range / type / category)
│       ├── [3] Create record
│       │       └── (if type = Investment) Enter investment details
│       ├── [4] Edit record
│       ├── [5] Delete record (soft)
│       └── [0] Back
├── [3] Categories
│       ├── [1] List categories
│       ├── [2] Create category
│       ├── [3] Rename category
│       ├── [4] Delete category
│       └── [0] Back
├── [4] Reports
│       ├── [1] Period summary (weekly / monthly / yearly)
│       ├── [2] Income vs expense comparison
│       ├── [3] Investment performance
│       └── [0] Back
├── [5] Profile & Security
│       ├── [1] Update profile (name / phone / currency)
│       ├── [2] Change password
│       ├── [3] Enable MFA
│       ├── [4] Disable MFA
│       └── [0] Back
└── [0] Logout
```

Each menu option calls the relevant application service and prints results as formatted text tables or labelled key-value pairs. Exceptions from the domain (e.g. `ValidationException`, `UnauthorizedException`) are caught at the menu level and displayed as plain error messages rather than stack traces.

---

## 6. Testing Approach

### Unit Tests (`FinBoardUltra.Tests.Unit`)

Every application service is tested in complete isolation. All repository and infrastructure interfaces are replaced with Moq mocks. No database, no filesystem, no network.

**AuthService tests** cover:
- Registration succeeds with valid input and hashes the password.
- Registration throws `ConflictException` when the email is already taken.
- Login throws `UnauthorizedException` for unknown email or wrong password.
- Login with correct credentials and `MfaEnabled = false` creates a session and returns a token.
- Login with correct credentials and `MfaEnabled = true` creates an `MfaChallenge` and returns `MfaRequired = true`.
- `VerifyMfaAsync` succeeds with a valid, unused, unexpired code.
- `VerifyMfaAsync` throws `ValidationException` for an expired code.
- `VerifyMfaAsync` throws `ValidationException` for an already-used code.
- Exceeding the failed-login threshold throws `TooManyRequestsException`.
- `ChangePasswordAsync` revokes all active sessions.
- `ValidateSessionAsync` throws `UnauthorizedException` for expired or revoked tokens.

**FinancialRecordService tests** cover:
- `CreateAsync` rejects `Amount <= 0`.
- `CreateAsync` rejects a category that belongs to a different user.
- `CreateAsync` rejects an `Investment` record that is missing `InvestmentDetail`.
- `GetByIdAsync` throws `NotFoundException` for a record belonging to a different user.
- `DeleteAsync` sets `IsDeleted = true` and does not call any hard-delete method.
- Audit log entries are written on create, update, and delete.

**DashboardService tests** cover:
- Balance is `sum(Income) - sum(Expense)` across all time.
- Monthly figures are restricted to the current calendar month.
- Investment value is `sum(CurrentPrice * Quantity)`.
- Recent transactions returns the correct count in the correct order.

**CategoryService tests** cover:
- Users can create their own categories.
- System defaults are returned alongside user categories.
- Deleting a system default throws `UnauthorizedException`.

**ReportService tests** cover:
- `GetFilteredAsync` returns only records matching all supplied filters.
- `GetByPeriodAsync` returns zero-value buckets for periods with no records.
- `GetInvestmentPerformanceAsync` computes gain/loss correctly.

**UserProfileService tests** cover:
- `EnableMfaAsync` throws `ValidationException` when phone number is absent.
- `UpdateProfileAsync` throws `ValidationException` when removing phone number while MFA is enabled.

### Integration Tests (`FinBoardUltra.Tests.Integration`)

Each test class creates a fresh `AppDbContext` using the EF Core InMemory provider in its constructor (no shared state between tests). The concrete repository implementation is used directly — no mocks.

**Covered scenarios**:
- Insert and retrieve a `User`; confirm the retrieved entity matches.
- Soft-delete a `FinancialRecord`; confirm it is excluded from `GetAllAsync` results but still present in the raw `DbContext.Set<FinancialRecord>()`.
- Insert records across multiple users; confirm per-user queries never return the other user's records.
- Category query returns system defaults (`UserId == null`) alongside user-owned categories.
- `MfaChallenge` query correctly excludes expired and used codes.

### Coverage Expectations

- All application service methods: 100% branch coverage.
- All domain validation logic (field presence, Amount positivity, MFA prerequisites): 100% branch coverage.
- Repository implementations: all public methods exercised by at least one integration test.

Coverage is enforced with `dotnet test --collect:"XPlat Code Coverage"` and reported via `reportgenerator`. The CI gate (added when CI is configured) rejects the build if application-layer branch coverage falls below 100%.

---

## 7. Constraints Respected

This section maps each CLAUDE.md business rule and security constraint to the specific design decision that satisfies it.

### Business Rules

| Rule | How satisfied |
|---|---|
| A user can only access their own data | Every service method that reads or writes records accepts a `userId` parameter. Repository implementations include `WHERE UserId = @userId` on every query. `GetByIdAsync` throws `NotFoundException` (not `UnauthorizedException`) so callers cannot distinguish between "does not exist" and "belongs to another user". |
| Every financial record requires type, amount, date, and category | `CreateRecordRequest` validation in `FinancialRecordService.CreateAsync` checks all four fields and throws `ValidationException` before touching the repository. |
| Amount is always a positive decimal; type determines direction | `CreateAsync` and `UpdateAsync` reject `Amount <= 0` with `ValidationException`. The `Type` field is required and must be a valid `RecordType` enum value. |
| Deleting a financial record sets a deleted flag; record is retained | `FinancialRecordService.DeleteAsync` sets `IsDeleted = true` and calls `UpdateAsync` on the repository. No hard-delete path exists anywhere in the codebase. |
| Dashboard figures are aggregated from records at query time | `DashboardService` has no state and no cache. Every call to `GetSummaryAsync` executes fresh aggregation queries. No `Balance` field exists on `User` or anywhere else. |
| MFA requires a valid phone number before it can be enabled | `UserProfileService.EnableMfaAsync` loads the user and throws `ValidationException` if `PhoneNumber` is null or whitespace before setting `MfaEnabled = true`. |
| An MFA code is single-use and expires after 10 minutes | `MfaChallenge.ExpiresAt` is set to `CreatedAt + 10 minutes` on creation. `AuthService.VerifyMfaAsync` throws `ValidationException` if `UsedAt != null` or `DateTime.UtcNow >= ExpiresAt`. |

### Security Constraints

| Constraint | How satisfied |
|---|---|
| Passwords stored as a one-way hash | `IPasswordHasher` is the only way to interact with passwords. `BcryptPasswordHasher` uses BCrypt with work factor 12. The `User.PasswordHash` field is never returned in any DTO. |
| Authentication is token-based with expiry and refresh | `UserSession` stores a random 256-bit token and a refresh token. `ExpiresAt` is checked on every call to `ValidateSessionAsync`. `IsRevoked` allows explicit invalidation. |
| Every protected operation requires a valid authenticated session | Each protected menu action calls `AuthService.ValidateSessionAsync` before invoking any application service. Application services accept `userId` (already validated), not the raw token, to keep session logic centralised. |
| Login attempts are rate-limited per IP | `AuthService.LoginAsync` queries `AuditLogRepository` for the count of `LoginFailed` events in the last 15 minutes for the requesting IP placeholder. If the count exceeds the threshold (default 5), a `TooManyRequestsException` is thrown before any credential check occurs. The IP field on `AuditLog` is a string placeholder at this layer; the actual IP is supplied by the calling layer (ConsoleApp passes a fixed string; a future API layer will supply the real remote address). |
| Critical actions are audit-logged | Every state-changing service method writes to `IAuditLogRepository` before returning. The full list of logged actions is the `AuditAction` enum: `Register`, `Login`, `LoginFailed`, `Logout`, `PasswordChanged`, `MfaEnabled`, `MfaDisabled`, `MfaChallengeFailed`, `RecordCreated`, `RecordUpdated`, `RecordDeleted`, `CategoryCreated`, `CategoryDeleted`, `ProfileUpdated`. Audit log rows are never updated or deleted. |

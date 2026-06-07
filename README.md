# CareerHub API — Assignment 2.4

Continues from Assignment 2.3. The data layer has been hardened for production load. Database-level check constraints enforce business rules independently of the application. Strategic indexes stop full table scans. Full-text search uses a stored tsvector column with a GIN index. Compiled queries eliminate per-call query plan overhead on hot paths. A slow query interceptor logs any command exceeding a configurable threshold. A raw SQL endpoint delivers application statistics using window functions EF Core cannot express in LINQ. The connection pool is sized for a three-instance production deployment.

---

## What is inside

```
CareerHub.Api/
├── CareerHub.Api.csproj
├── Program.cs
├── appsettings.json                          updated — pool settings and SlowQueryThresholdMs
├── appsettings.Development.json              updated — dev pool settings
├── Models/
│   ├── JobListing.cs                         updated — adds NpgsqlTsVector SearchVector
│   ├── JobListingStatus.cs                   Active or Closed
│   ├── JobType.cs
│   ├── Company.cs
│   ├── Applicant.cs
│   ├── Application.cs
│   └── ApplicationStatus.cs
├── Data/
│   ├── CareerHubDbContext.cs                 updated — check constraints, indexes, tsvector
│   └── JobListingStore.cs                    empty — replaced by EF Core
├── Migrations/                               all migration files
├── DTOs/
│   ├── CreateJobRequest.cs
│   ├── UpdateJobRequest.cs
│   ├── JobResponse.cs
│   ├── JobDetailResponse.cs
│   ├── ApplicationSummary.cs
│   ├── ApplicationDTOs.cs
│   ├── CompanyDTOs.cs
│   ├── JobListingStatsResponse.cs            new — for raw SQL stats endpoint
│   ├── LoginRequest.cs
│   └── LoginResponse.cs
├── Exceptions/
│   ├── JobNotFoundException.cs
│   ├── CompanyNotFoundException.cs
│   ├── DuplicateJobListingException.cs
│   ├── DuplicateApplicationException.cs
│   ├── ListingClosedException.cs
│   ├── InvalidStatusTransitionException.cs
│   ├── UnauthorizedOperationException.cs
│   └── InvalidListingException.cs
├── Repositories/
│   ├── IJobListingRepository.cs              updated — SearchAsync, GetApplicationStatsAsync
│   ├── JobListingRepository.cs               updated — compiled query, FTS, raw SQL
│   ├── ICompanyRepository.cs
│   ├── CompanyRepository.cs
│   ├── IApplicationRepository.cs
│   └── ApplicationRepository.cs             updated — compiled query for HasAlreadyApplied
├── Services/
│   ├── ApplicationStatusTransitions.cs
│   ├── IJobListingService.cs                 updated — SearchAsync, GetApplicationStatsAsync
│   ├── JobListingService.cs                  updated — two new methods
│   ├── ICompanyService.cs
│   ├── CompanyService.cs
│   ├── IApplicationService.cs
│   └── ApplicationService.cs
├── Infrastructure/
│   ├── ServiceCollectionExtensions.cs        updated — registers SlowQueryInterceptor
│   └── SlowQueryInterceptor.cs               new — logs slow SQL commands
├── Middleware/
│   └── GlobalExceptionHandler.cs
├── Controllers/
│   ├── JobsController.cs                     updated — search and stats endpoints
│   ├── CompaniesController.cs
│   ├── ApplicationsController.cs
│   └── AuthController.cs
└── Properties/
    └── launchSettings.json
```

---

## What it does

| Request | What happens | Response |
| --- | --- | --- |
| `GET /jobs` | Returns all active listings with company name and application count | 200 OK |
| `GET /jobs/{id}` | Returns one listing with full application details | 200 OK or 404 |
| `GET /jobs/search?q={term}` | Full-text search on title and description using GIN index | 200 OK |
| `GET /jobs/stats?companyId={id}` | Application statistics per listing with RANK | 200 OK |
| `POST /jobs` | Creates a new listing | 201, 400, or 409 |
| `PUT /jobs/{id}` | Updates an existing listing | 200, 400, or 404 |
| `DELETE /jobs/{id}` | Closes a listing | 204 or 404 |
| `POST /applications/{listingId}` | Applicant applies for a job | 201 or 409 |
| `GET /applications/listing/{id}` | Employer views all applicants for a listing | 200 OK |
| `GET /applications/my` | Applicant views their own applications | 200 OK |
| `PUT /applications/{listingId}/{applicantId}/status` | Employer updates application status | 204 or 422 |
| `DELETE /applications/{listingId}` | Applicant withdraws their application | 204 or 403 |
| `GET /companies` | Returns all companies | 200 OK |
| `POST /companies` | Creates a new company | 201 or 409 |
| `POST /auth/login` | Returns a signed JWT token | 200 OK or 401 |
| `GET /auth/me` | Returns current user's username and role | 200 OK or 401 |

---

## Why I used Controllers and not Minimal APIs

The assignment asked us to pick one and explain why. I went with Controllers because it is the style used in class, each URL is easy to find, `[ApiController]` gives automatic validation and 400 responses for free, and it scales cleanly when more resources are added.

---

## Design Decisions

### Why PostedAt belongs in JobResponse but not CreateJobRequest

PostedAt is set by the server at the exact moment a job is created. If we allowed the client to supply it, they could submit a job with a past date. Since the server owns this value it appears in the response so the frontend can show "Posted 3 days ago" but must never appear in the request DTO.

### Salary cross-field validation

Standard Data Annotations only inspect one field at a time. To enforce SalaryMax must be greater than SalaryMin, I implemented `IValidatableObject`. The `Validate()` method runs after all individual annotations pass. The controller never touches salary logic — if the check fails, `[ApiController]` returns 400 before the controller method runs.

### PUT returns 200 OK with body

Returning the updated `JobResponse` means the frontend can update its local state without a second GET request.

### DELETE returns 404 for a missing ID

A 204 would imply success — but nothing was removed. 404 tells the client the resource did not exist.

### Controller Thinning

Throwing `JobNotFoundException` instead of returning `NotFound()` means the controller only handles the happy path. `GlobalExceptionHandler` is the single place that decides the HTTP status code. You write that mapping once and it works everywhere.

### Structured Logging

`Console.WriteLine` outputs a flat string that cannot be searched. Serilog writes JSON with named fields. In production a log aggregator can query "show me all 404s in the last hour" or alert when 500s spike.

### Stateless Authentication — Session vs JWT

JWT is stateless — the token contains all the information including who you are, your role, and when it expires. Any server can verify it using only the secret key. No shared session store needed across multiple servers.

### 401 Unauthorized vs 403 Forbidden

401 means the client has not proven who they are — no token, expired token, or bad signature. `UseAuthentication()` produces this. 403 means the identity is confirmed but the role does not match. `UseAuthorization()` produces this. This is why `UseAuthentication()` must come before `UseAuthorization()` in the pipeline.

### JWT Token Storage

`localStorage` is accessible to any JavaScript on the page. An XSS vulnerability would expose every token. The safer alternatives are HttpOnly cookies — JavaScript cannot read them — or in-memory storage where the token disappears on page refresh.

### The Change Tracker

EF Core takes a snapshot of every entity it loads. When you mutate a property only the in-memory object changes. When you call `SaveChangesAsync()`, EF Core compares the current state against the snapshot and generates the minimum SQL needed. You call it once at the end — not once per property change.

### Migrations as Version Control

A migration file is the SQL definition of your schema changes expressed as C# code. If you commit code that references a new table but forget to commit the migration, your teammate pulls the code and their database fails at runtime. Committing both together ensures everyone has a clear script to bring their database up to date.

### Connection String Security

`appsettings.json` is committed to source control. A database password there is a real security risk. `appsettings.Development.json` only loads locally and should be in `.gitignore`. For production the safer approach is environment variables or Azure Key Vault.

### Relationship Design Decisions

**Company to JobListing delete behaviour: Restrict**

A company cannot be deleted while it still has job listings. Silently wiping all of a company's listings when the company is deleted would be dangerous on a job board. If a company needs to be removed, the listings must be explicitly deleted first. This forces deliberate cleanup.

**Why the Application entity cannot be a hidden join table**

A hidden join table only stores two foreign keys and carries no data of its own. An application carries a submission timestamp and a status that changes over time. A hidden join table has no columns to store either of those. The moment a relationship needs to carry its own data it must become an explicit entity.

### The N+1 Query Problem

**Before the fix:** Loading company names caused one SQL query for all job listings then one additional query for each listing to load its company. With five listings that was six queries. With a hundred listings it would be 201 queries.

**After the fix:** Using a projection with `.Select()` produces exactly one SQL statement with JOIN clauses regardless of how many listings exist. ApplicationCount is computed by the database with `COUNT(*)` — not by loading all applications into memory.

**Why this is dangerous in production:** In development with five rows the difference is invisible. In production with ten thousand listings the API fires ten thousand individual database queries per request. The database connection pool runs out. The app collapses under real traffic with no obvious error.

### Read vs Write Queries

A GET endpoint that uses the change tracker pays extra cost — EF Core snapshots every entity it loads. On a read endpoint that never calls `SaveChangesAsync()` that snapshot is wasted work. `AsNoTracking()` skips it.

A dangerous scenario: if you used `AsNoTracking()` on a PUT endpoint, mutated a property, and called `SaveChangesAsync()`, EF Core would have no snapshot and generate no UPDATE statement. The change would silently disappear with no error.

### Repository Design Decisions

One repository per entity: `IJobListingRepository`, `ICompanyRepository`, `IApplicationRepository`. Each repository owns exactly the queries that relate to its entity. When `ApplicationService` needs to validate that a listing is open, it calls `IJobListingRepository.IsOpenForApplicationsAsync`. The query lives in the repository that owns the entity.

Returning `IQueryable<T>` from a repository interface breaks the abstraction because `IQueryable<T>` is tied to EF Core's LINQ provider. Any class consuming it must import `Microsoft.EntityFrameworkCore`. This forces the service layer to know about EF Core — defeating the purpose of the repository.

### What the Controller Lost

Every piece of logic that moved out of the controllers during the 2.3 refactor:

| Logic | Moved to | Why |
|---|---|---|
| Company existence check | JobListingService | Business rule |
| Closing date validation | JobListingService | Business rule |
| Duplicate listing check | JobListingRepository | Database query |
| Duplicate application check | ApplicationRepository | Database query |
| Status transition validation | ApplicationService + ApplicationStatusTransitions | Business rule |
| Applicant ownership check | ApplicationService | Business rule |
| AsNoTracking, Include, FindAsync | Repositories | EF Core — must not appear outside repository |
| Entity construction | JobListingService | Business logic |
| MapToResponse, SalaryDisplay | JobListingRepository | Projection |

### Status Transition Design

A static dictionary where each key is a from status and each value is the set of permitted to statuses. Rules are defined in exactly one place. `IsValid` is a pure function with no database query. Adding a new valid transition requires changing one line — no switch statements or if/else chains anywhere else.

Valid workflow:
```
Submitted → UnderReview → Shortlisted → Offered
                        → Rejected
             Shortlisted → Rejected
```

### Lifetime Misconfiguration

When `JobListingService` was registered as `AddSingleton` instead of `AddScoped`:

```
Cannot consume scoped service 'IJobListingRepository' from singleton 'IJobListingService'.
```

A Singleton lives for the entire application lifetime. A Scoped service lives for one HTTP request. If a Singleton captures a Scoped `DbContext` at startup, it reuses the same `DbContext` forever — the change tracker accumulates state from unrelated requests and causes data corruption. The fix is `AddScoped`.

### Constraint Decisions

**ck_job_listings_salarymin_positive:** SalaryMin must be greater than zero when provided. Bypass scenario: a direct psql INSERT could store a negative salary. Every API response would show corrupt data with no error.

**ck_job_listings_salarymax_gt_min:** SalaryMax must be greater than SalaryMin when both are provided. Bypass scenario: a batch migration script importing old data could store inverted salary ranges.

**ck_job_listings_closingdate_after_postedat:** ClosingDate must be after PostedAt. Bypass scenario: an UPDATE statement run directly against the database could backdate ClosingDate to before PostedAt.

**ck_applications_submittedAt_not_future:** SubmittedAt must not be in the future. Bypass scenario: a direct INSERT with a future timestamp would make applications appear to have been submitted before the listing existed.

### Index Decisions

**ix_job_listings_status_closingdate — (Status, ClosingDate):** Supports `GetActiveListingsAsync` — the most frequent query called on every page load. Status first because it eliminates all Closed rows immediately. ClosingDate then filters within the small Active set. A query filtering only on ClosingDate cannot use this index.

**ix_job_listings_companyid_status — (CompanyId, Status):** Supports the employer's own listing view. CompanyId first because one company's listings is a very small subset of the table.

**ix_job_listings_searchvector — GIN on SearchVector:** Supports `SearchAsync`. B-tree cannot index tsvector. GIN inverts the vector so each word maps to the rows containing it.

**ix_applications_joblistingid_applicantid — (JobListingId, ApplicantId):** Supports `HasAlreadyAppliedAsync` — called on every application submission.

**ix_applications_joblistingid — JobListingId:** Supports `GetByListingIdAsync` — the employer dashboard.

### EXPLAIN ANALYZE Findings

**Before indexes — GetActiveListingsAsync:**
```
Seq Scan on job_listings
Filter: (Status = 'Active') AND (ClosingDate > now())
Rows Removed by Filter: 157
Execution Time: 8.9 ms
```

**After indexes — GetActiveListingsAsync:**
```
Bitmap Heap Scan on job_listings
  -> Bitmap Index Scan on ix_job_listings_status_closingdate
Execution Time: 0.3 ms
```

Changed from scanning all 200 rows to scanning 43 matching rows. A Seq Scan reads every row then filters. A Bitmap Index Scan uses the composite index to identify matching row IDs before touching the table. With 50,000 rows the difference would be the application staying online versus crashing.

### Hot Path Justification

**IsOpenForApplicationsAsync:** Called on every application submission. With 1,000 active daily users submitting roughly 3 applications each, this runs approximately 125 times per hour during peak. EF Core rebuilds the LINQ expression tree and generates the SQL plan on every call without compilation. Compiling it at startup amortises that cost across all future calls.

**HasAlreadyAppliedAsync:** Called immediately after `IsOpenForApplicationsAsync` on every submission — same frequency. Two compiled queries run per application attempt.

### FromSql Parameterisation

String interpolation inside `SqlQuery<T>($"...{companyId}...")` is safe. EF Core receives a `FormattableString` and extracts interpolated values as named parameters — `{companyId}` becomes `@p0`. The SQL sent to PostgreSQL never contains the actual value.

Using `string.Format` or concatenation before passing the string to `SqlQuery<T>` is not safe. EF Core receives a completed string with the value already embedded. It cannot extract parameters from a pre-built string. That is a SQL injection risk.

### Connection Pool Calculation

```
PostgreSQL max_connections        = 100
Reserved for admin and monitoring = 10
Available for application         = 90
Number of instances               = 3
Connections per instance          = 90 / 3 = 30

MaxPoolSize = 30  (production)
MaxPoolSize = 10  (development — one instance, less load)
```

When all connections are in use, new requests wait up to 15 seconds for one to become available. If none is returned, the client receives a 500 after a 15-second hang — not an immediate failure.

---

## How to Run

### Prerequisites

- .NET 10 SDK — check with `dotnet --version`
- Docker Desktop — must be running before starting the app

### First time setup

```bash
docker run -d --name careerhub-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=CareerHub -p 5432:5432 postgres:latest
cd CareerHub.Api
dotnet ef database update
dotnet run
```

### Every time after that

```
1. Open Docker Desktop
2. docker start careerhub-postgres
3. dotnet run
```

Browser opens at **http://localhost:5000/scalar/v1**

---

## Credentials

| Username | Password | Role | Who they are |
| --- | --- | --- | --- |
| employer | password123 | Employer | Posts and manages jobs |
| applicant1 | password123 | Applicant | Alice Smith |
| applicant2 | password123 | Applicant | Bob Jones |

---

## How to Test — Assignment 2.3

### Test 1 — Layer separation

Open `Services/JobListingService.cs` and `Services/ApplicationService.cs`. Confirm neither contains `using Microsoft.EntityFrameworkCore`.

### Test 2 — Duplicate application

Enable query logging in `Program.cs`:
```csharp
options.UseNpgsql(...).LogTo(Console.WriteLine, LogLevel.Information)
```

Get employer token. Create a company. Create a job. Get applicant token. Apply — confirm 201 and INSERT in terminal. Apply again — confirm 409 and no second INSERT.

### Test 3 — Status transition

Using employer token, try PUT /Applications/{jobId}/{applicantId}/status with `{ "status": "Offered" }` on a Submitted application. Confirm 422. Then walk the valid path: UnderReview → Shortlisted → Offered. Confirm 204 each time.

### Test 4 — Lifetime validation

Change `AddScoped<IJobListingService, JobListingService>()` to `AddSingleton`. Run the app. Confirm startup error. Fix back to `AddScoped`. Confirm clean startup.

### Test 5 — Controller line count

Show any two controller actions. Each must be 10 lines or less with no business logic.

### Test 6 — End-to-end flow

With logging enabled: create a job listing, show the INSERT in the terminal and 201 response. Then try to create a job with a non-existent company ID. Confirm 404.

### Test 7 — Extension method registration

Show `Program.cs` contains no direct `AddScoped`, `AddTransient`, or `AddSingleton` calls — only extension method calls.

---

## How to Test — Assignment 2.4

### Test 1 — Constraint enforcement

```bash
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "INSERT INTO job_listings (\"Id\", \"Title\", \"Description\", \"CompanyId\", \"Location\", \"Type\", \"PostedAt\", \"IsActive\", \"ClosingDate\", \"Status\", \"SalaryMin\", \"SalaryMax\") SELECT gen_random_uuid(), 'Bad Salary', 'Test description here', \"Id\", 'Joburg', 'FullTime', NOW(), true, NOW() + interval '30 days', 'Active', 5000, 1000 FROM companies LIMIT 1;"
```

Expected: `new row violates check constraint "ck_job_listings_salarymax_gt_min"`

```bash
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "INSERT INTO applications (\"JobListingId\", \"ApplicantId\", \"SubmittedAt\", \"Status\") SELECT \"Id\", 'a0000000-0000-0000-0000-000000000001', NOW() + interval '1 day', 'Submitted' FROM job_listings LIMIT 1;"
```

Expected: `new row violates check constraint "ck_applications_submittedAt_not_future"`

### Test 2 — Index verification

```bash
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "\d job_listings"
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "\d applications"
```

Confirm all five indexes are present.

### Test 3 — EXPLAIN ANALYZE before and after

Seed 200 listings:
```bash
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "
INSERT INTO job_listings (\"Id\", \"Title\", \"Description\", \"CompanyId\", \"Location\", \"Type\", \"PostedAt\", \"IsActive\", \"ClosingDate\", \"Status\")
SELECT gen_random_uuid(), 'Developer Role ' || gs, 'Test description for listing number ' || gs || ' at our company.', (SELECT \"Id\" FROM companies ORDER BY RANDOM() LIMIT 1), 'Bloemfontein', 'FullTime', NOW() - (random() * interval '60 days'), true, NOW() + (random() * interval '180 days'), CASE WHEN random() > 0.3 THEN 'Active' ELSE 'Closed' END
FROM generate_series(1, 200) gs;"
```

Drop index, run EXPLAIN ANALYZE, show Seq Scan. Recreate index, run EXPLAIN ANALYZE, show Bitmap Index Scan.

### Test 4 — Full-text search

GET /Jobs/search?q=developer — show matching results. GET /Jobs/search?q=developing — show stemming returns developer results too. Run EXPLAIN ANALYZE to confirm GIN index is used.

### Test 5 — Compiled query confirmation

Show `private static readonly Func<...>` fields in `JobListingRepository.cs` and `ApplicationRepository.cs`.

### Test 6 — Slow query interceptor

Set `SlowQueryThresholdMs` to 0. Run app. Call GET /Jobs. Show warnings in terminal. Restore to 100. Show no warnings.

### Test 7 — Raw SQL statistics

Call GET /Jobs/stats?companyId={id}. Show response with per-status counts and rank field. Confirm the listing with most applications has rank 1.

### Test 8 — Connection pool

Show `appsettings.json` with `Maximum Pool Size=30` and `appsettings.Development.json` with `Maximum Pool Size=10`. Explain the calculation.

---

## Seeded Applicants

Two applicants are automatically inserted when migrations are applied:

```bash
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "SELECT * FROM applicants;"
```

Expected:
```
a0000000-0000-0000-0000-000000000001 | Alice Smith | alice@example.com | applicant1
a0000000-0000-0000-0000-000000000002 | Bob Jones   | bob@example.com   | applicant2
```

If the table is empty run:
```bash
dotnet ef migrations add SeedApplicants
dotnet ef database update
```

---

## Git history

```bash
git log --oneline --graph --all
```
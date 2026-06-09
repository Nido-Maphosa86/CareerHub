# CareerHub API — Assignment 3.1

Continues from Assignment 2.4. The API is now production-ready for frontend consumption. Pagination stops unbounded responses on the job board. Client-controlled filtering and sorting reduce unnecessary data transfer. A PATCH endpoint resolves the PUT race condition. URL segment versioning adds a non-breaking contract for future changes. ETags eliminate redundant responses for unchanged resources. Rate limiting protects the search and application endpoints from abuse.

---

## What is inside

```
CareerHub.Api/
├── CareerHub.Api.csproj                      updated — Asp.Versioning.Mvc package added
├── Program.cs                                updated — CORS, versioning, rate limiting
├── appsettings.json
├── appsettings.Development.json
├── Models/
│   ├── JobListing.cs
│   ├── JobListingStatus.cs
│   ├── JobType.cs
│   ├── Company.cs
│   ├── Applicant.cs
│   ├── Application.cs
│   └── ApplicationStatus.cs
├── Data/
│   ├── CareerHubDbContext.cs
│   └── JobListingStore.cs
├── Migrations/
├── DTOs/
│   ├── PagedResponse.cs                      new — pagination envelope
│   ├── JobListingFilterQuery.cs              new — filter and sort parameters
│   ├── UpdateJobListingRequest.cs            new — all nullable fields for PATCH
│   ├── CreateJobRequest.cs
│   ├── UpdateJobRequest.cs
│   ├── JobResponse.cs
│   ├── JobDetailResponse.cs
│   ├── ApplicationSummary.cs
│   ├── ApplicationDTOs.cs
│   ├── CompanyDTOs.cs
│   ├── JobListingStatsResponse.cs
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
│   ├── IJobListingRepository.cs              updated — paged, company listings, PatchAsync
│   ├── JobListingRepository.cs               updated — pagination, filtering, sorting, PATCH
│   ├── ICompanyRepository.cs
│   ├── CompanyRepository.cs
│   ├── IApplicationRepository.cs
│   └── ApplicationRepository.cs
├── Services/
│   ├── ApplicationStatusTransitions.cs
│   ├── IJobListingService.cs                 updated — paged and patch methods
│   ├── JobListingService.cs                  updated — implements paged and patch
│   ├── ICompanyService.cs
│   ├── CompanyService.cs
│   ├── IApplicationService.cs
│   └── ApplicationService.cs
├── Infrastructure/
│   ├── ServiceCollectionExtensions.cs
│   └── SlowQueryInterceptor.cs
├── Middleware/
│   └── GlobalExceptionHandler.cs
├── Controllers/
│   ├── JobsController.cs                     updated — versioning, pagination, PATCH, ETags, rate limiting
│   ├── CompaniesController.cs                updated — versioning
│   ├── ApplicationsController.cs             updated — versioning, PATCH status, rate limiting
│   └── AuthController.cs                     updated — versioning
└── Properties/
    └── launchSettings.json
```

---

## What it does

| Request | What happens | Response |
| --- | --- | --- |
| `GET /api/v1/jobs` | Returns paginated active listings with filters and sort | 200 OK |
| `GET /api/v1/jobs/{id}` | Returns one listing with ETag for conditional requests | 200 OK, 304, or 404 |
| `GET /api/v1/jobs/search?q={term}` | Full-text search using GIN index | 200 OK |
| `GET /api/v1/jobs/stats?companyId={id}` | Application statistics per listing with RANK | 200 OK |
| `GET /api/v1/jobs/company/{companyId}` | Employer views their own listings paginated | 200 OK |
| `POST /api/v1/jobs` | Creates a new listing | 201, 400, or 409 |
| `PUT /api/v1/jobs/{id}` | Fully replaces an existing listing | 200, 400, or 404 |
| `PATCH /api/v1/jobs/{id}` | Partially updates a listing — only supplied fields change | 200, 400, or 404 |
| `DELETE /api/v1/jobs/{id}` | Closes a listing | 204 or 404 |
| `POST /api/v1/applications/{listingId}` | Applicant applies for a job | 201 or 409 |
| `GET /api/v1/applications/listing/{id}` | Employer views all applicants for a listing | 200 OK |
| `GET /api/v1/applications/my` | Applicant views their own applications | 200 OK |
| `PATCH /api/v1/applications/{listingId}/{applicantId}/status` | Employer updates application status | 204 or 422 |
| `DELETE /api/v1/applications/{listingId}` | Applicant withdraws their application | 204 or 403 |
| `GET /api/v1/companies` | Returns all companies | 200 OK |
| `POST /api/v1/companies` | Creates a new company | 201 or 409 |
| `POST /api/v1/auth/login` | Returns a signed JWT token | 200 OK or 401 |
| `GET /api/v1/auth/me` | Returns current user's username and role | 200 OK or 401 |

Calls without a version prefix — for example `GET /api/jobs` — are treated as v1 and return identical results.

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

A company cannot be deleted while it still has job listings. Silently wiping all of a company's listings when the company is deleted would be dangerous on a job board. If a company needs to be removed, the listings must be explicitly deleted first.

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

**ix_job_listings_status_closingdate — (Status, ClosingDate):** Supports `GetActiveListingsAsync` — the most frequent query called on every page load. Status first because it eliminates all Closed rows immediately. ClosingDate then filters within the small Active set.

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

Changed from scanning all 200 rows to scanning 43 matching rows. A Seq Scan reads every row then filters. A Bitmap Index Scan uses the composite index to identify matching row IDs before touching the table.

### Hot Path Justification

**IsOpenForApplicationsAsync:** Called on every application submission. With 1,000 active daily users submitting roughly 3 applications each, this runs approximately 125 times per hour during peak. Compiling it at startup amortises the expression tree translation cost across all future calls.

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

### Pagination Decision

I used offset pagination. Offset uses `Skip` and `Take` in SQL — predictable page numbers and simple for the frontend to display "Page 2 of 10".

The accepted tradeoff: if a new listing is posted between a user fetching page 1 and page 2, the listings shift by one position. The last listing from page 1 might appear again at the top of page 2. For a job board this is acceptable — a user who sees a duplicate listing while browsing is a minor inconvenience, not a data integrity problem.

The implementation issues exactly two database queries per request — one `CountAsync` and one `ToListAsync` applied to the same `IQueryable` to guarantee the count and the data are always consistent. `OrderBy` appears before `Skip` so pagination is deterministic.

### PATCH Race Condition

**The PUT race condition — exact sequence of events:**

Two recruiters open the same listing at the same time. Recruiter A changes the salary to 65000 and submits a full PUT. Recruiter B changes the description and submits a full PUT one second later. B's PUT replaces the entire listing including A's salary field which B still has at the old value. A's salary change is silently overwritten. No error is thrown. No one knows.

**Why nullable DTO prevents it:**

Each recruiter sends only the field they changed. A sends `{ "salaryMin": 65000 }`. B sends `{ "description": "New description" }`. The PATCH implementation only applies non-null fields. Both changes survive independently.

**One remaining limitation:**

You cannot use null to deliberately clear a field. If an employer wants to remove the salary information, sending `{ "salaryMin": null }` is interpreted as "don't change" rather than "set to null". JSON Patch (RFC 6902) solves this with an explicit `{ "op": "remove", "path": "/salaryMin" }` operation. For CareerHub, salary fields are never intended to be removed once set, so this limitation does not affect current requirements.

### Versioning Lifecycle

To introduce a v2 `JobListingResponse` that renames `SalaryMin` to `MinimumSalary`:

Create a `JobListingResponseV2` record with the new field name. Add a new `JobsControllerV2` decorated with `[ApiVersion(2)]`. Keep v1 completely unchanged. Run both simultaneously for at least 6 months. Add a `Sunset` header to v1 responses indicating the removal date. Add a `Deprecation` header with the date v1 was deprecated. After the sunset period, remove the v1 controller. The `api-supported-versions` header already informs clients which versions are active.

### ETag Fingerprint

The current ETag is computed from `{id}-{PostedAt.Ticks}-{SalaryMin}`. A change to Description or Location does not change this ETag. A client who cached the listing would receive 304 and render stale content after a description update.

A stronger ETag would use a `LastModifiedAt` timestamp on the `JobListing` entity updated on every write. Any change to any field updates `LastModifiedAt` which changes the ETag. This field would be set inside `SaveChangesAsync` or via a DbContext interceptor.

### Rate Limiting

**Why the apply policy uses a 60-minute window:**

A 60-second window would be too short. A legitimate applicant might apply to several jobs in one session and hit the limit accidentally. A 60-minute window of 5 submissions is generous for a normal job seeker and restrictive enough to block automated bots submitting thousands of fake applications.

**Why IP-based rate limiting is insufficient for authenticated requests:**

An office building with many employees sharing a NAT gateway all appear as one IP address. If one employee hits the limit, the entire office is blocked. A bot using a VPN pool can rotate IPs and bypass the limit entirely. The correct partition key for authenticated requests is the `sub` claim from the JWT — the user's identity. This limits each individual user regardless of which IP, device, or VPN they use.

### CORS — AllowAnyOrigin and AllowCredentials

Calling `AllowAnyOrigin()` together with `AllowCredentials()` causes a startup exception:

```
The CORS protocol does not allow specifying a wildcard origin with credentials.
```

A wildcard origin tells the browser any website can call the API. `AllowCredentials` tells the browser to send the Authorization header. Together they would allow any malicious website to make credentialed requests using the user's own token. The browser specification forbids this combination. Specific origins must be listed explicitly.

### Connection Pool — Effect of Rate Limiting

Rate limiting converts burst traffic into a steady stream. Without rate limiting, 1,000 users hitting the search endpoint simultaneously could exhaust the connection pool of 30. With the sliding window policy of 30 requests per 60 seconds, the maximum concurrent search connections at any moment is a small fraction of the pool. The pool sizing calculation from Assignment 2.4 remains correct and is now conservative.

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

Open `Services/JobListingService.cs` and `Services/ApplicationService.cs` in VS Code. Confirm neither contains `using Microsoft.EntityFrameworkCore`. Screenshot both files.

### Test 2 — Duplicate application

Get employer token. Create a company. Create a job. Get applicant token. Apply — confirm 201 and INSERT in terminal. Apply again — confirm 409 and no second INSERT. Screenshot both responses.

### Test 3 — Status transition

Using employer token, try PATCH /api/v1/Applications/{jobId}/{applicantId}/status with `{ "status": "Offered" }` on a Submitted application. Confirm 422. Then walk the valid path: UnderReview → Shortlisted → Offered. Confirm 204 each time. Screenshot.

### Test 4 — Lifetime validation

Change `AddScoped<IJobListingService, JobListingService>()` to `AddSingleton`. Run the app. Confirm startup error. Fix back to `AddScoped`. Confirm clean startup. Screenshot the error.

### Test 5 — Controller line count

Open any two controller actions in VS Code. Each must be 10 lines or less with no business logic. Screenshot.

### Test 6 — End-to-end flow

With logging enabled: create a job listing, show the INSERT in the terminal and 201 response. Then try to create a job with a non-existent company ID. Confirm 404. Screenshot.

### Test 7 — Extension method registration

Open `Program.cs` in VS Code. Confirm it contains no direct `AddScoped`, `AddTransient`, or `AddSingleton` calls for application services — only extension method calls like `AddJobListingFeature()`. Screenshot.

---

## How to Test — Assignment 2.4

All psql tests require connecting to the database first. Open a terminal and run:

```bash
docker exec -it careerhub-postgres psql -U postgres -d CareerHub
```

### Test 1 — Constraint enforcement

**Bad salary range:**

```sql
INSERT INTO job_listings ("Id", "Title", "Description", "CompanyId", "Location", "Type", "PostedAt", "IsActive", "ClosingDate", "Status", "SalaryMin", "SalaryMax")
VALUES (
  gen_random_uuid(),
  'Bad Salary',
  'Test description here',
  (SELECT "Id" FROM companies LIMIT 1),
  'Joburg',
  'FullTime',
  NOW(),
  true,
  NOW() + interval '30 days',
  'Active',
  5000,
  1000
);
```

Expected: `new row violates check constraint "ck_job_listings_salarymax_gt_min"`

**ClosingDate before PostedAt:**

```sql
INSERT INTO job_listings ("Id", "Title", "Description", "CompanyId", "Location", "Type", "PostedAt", "IsActive", "ClosingDate", "Status")
VALUES (
  gen_random_uuid(),
  'Bad Date',
  'Test description here',
  (SELECT "Id" FROM companies LIMIT 1),
  'Joburg',
  'FullTime',
  NOW(),
  true,
  NOW() - interval '1 day',
  'Active'
);
```

Expected: `new row violates check constraint "ck_job_listings_closingdate_after_postedat"`

**Future SubmittedAt:**

```sql
INSERT INTO applications ("JobListingId", "ApplicantId", "SubmittedAt", "Status")
VALUES (
  (SELECT "Id" FROM job_listings LIMIT 1),
  'a0000000-0000-0000-0000-000000000001',
  NOW() + interval '1 day',
  'Submitted'
);
```

Expected: `new row violates check constraint "ck_applications_submittedAt_not_future"`

Screenshot all three errors. Type `\q` to exit psql.

### Test 2 — Index verification

In psql run:

```sql
\d job_listings
\d applications
```

Confirm these indexes exist on job_listings: `ix_job_listings_status_closingdate`, `ix_job_listings_companyid_status`, `ix_job_listings_searchvector` (GIN), `ix_job_listings_title_companyid`.

Confirm these indexes exist on applications: `ix_applications_joblistingid_applicantid`, `ix_applications_joblistingid`.

Screenshot both outputs.

### Test 3 — EXPLAIN ANALYZE before and after

**Drop the index:**

```sql
DROP INDEX IF EXISTS ix_job_listings_status_closingdate;
```

**Run EXPLAIN ANALYZE — should show Seq Scan:**

```sql
EXPLAIN ANALYZE SELECT * FROM job_listings WHERE "Status" = 'Active' AND "ClosingDate" > NOW();
```

Screenshot showing `Seq Scan`.

**Recreate the index:**

```sql
CREATE INDEX ix_job_listings_status_closingdate ON job_listings("Status", "ClosingDate");
```

**Run EXPLAIN ANALYZE again — should show Bitmap Index Scan:**

```sql
EXPLAIN ANALYZE SELECT * FROM job_listings WHERE "Status" = 'Active' AND "ClosingDate" > NOW();
```

Screenshot showing `Bitmap Index Scan`. Type `\q` to exit psql.

### Test 4 — Full-text search

In Scalar call **GET /api/v{version}/Jobs/search** (version = 1) with query parameter `q = developer`. Confirm matching results. Change to `q = developing`. Confirm the same listings come back — stemming works. Screenshot both.

### Test 5 — Compiled query confirmation

Open `Repositories/JobListingRepository.cs` and `Repositories/ApplicationRepository.cs` in VS Code. Show the `private static readonly Func<...>` fields near the top of each file. Screenshot both.

### Test 6 — Slow query interceptor

Open `appsettings.Development.json` and change `SlowQueryThresholdMs` to `0`. Restart with `dotnet run`. Call GET /api/v1/jobs. Show `[WRN]` warnings in the terminal. Screenshot.

Restore `SlowQueryThresholdMs` to `100`. Restart. Call GET /api/v1/jobs again. No warnings. Screenshot the clean terminal.

### Test 7 — Raw SQL statistics

In Scalar call **GET /api/v{version}/Jobs/stats** (version = 1) with query parameter `companyId = {paste-company-id}`. Use employer token. Confirm the response has `rank`, `totalApplications`, and per-status counts. Screenshot.

### Test 8 — Connection pool

Open both files in VS Code. Screenshot `appsettings.json` showing `Minimum Pool Size=5;Maximum Pool Size=30` and `appsettings.Development.json` showing `Minimum Pool Size=2;Maximum Pool Size=10`.

---

## How to Test — Assignment 3.1 (Full Step-by-Step)

Tools used: Test 1 and Test 2 use **Postman**. All other tests use **Scalar**.

### Before anything — start the app

```bash
docker start careerhub-postgres
cd CareerHub.Api
dotnet run
```

Open **http://localhost:5000/scalar/v1** for Scalar tests.

Open **Postman** for CORS and versioning tests.

---

### Test 1 — CORS (Postman)

Open Postman. Create a new **GET** request.

**URL:**
```
http://localhost:5000/api/v1/jobs
```

Click the **Headers** tab (not Params). Add this header:

```
Key:    Origin
Value:  http://localhost:3000
```

Click **Send**.

Click the **Headers** tab in the response section. Confirm these headers:

```
Access-Control-Allow-Credentials:  true
Access-Control-Allow-Origin:       http://localhost:3000
Access-Control-Expose-Headers:     X-Total-Count
X-Total-Count:                     0
api-supported-versions:            1.0
```

Screenshot the Postman response headers.

---

### Test 2 — Versioning (Postman)

Keep the Origin header from Test 1.

**Unversioned URL — change the URL to:**
```
http://localhost:5000/api/jobs
```

Click Send. Expected: 200 OK. Confirm `api-supported-versions: 1.0` in response headers. Screenshot.

**Versioned URL — change the URL to:**
```
http://localhost:5000/api/v1/jobs
```

Click Send. Expected: 200 OK — identical response. Screenshot.

**v2 does not exist — change the URL to:**
```
http://localhost:5000/api/v2/jobs
```

Click Send. Expected: 404 Not Found. Screenshot.

---

### Test 3 — Create test data (Scalar)

You need data before pagination, filtering, PATCH, and ETags can be tested.

**Step 1 — get employer token**

In Scalar find **POST /api/Auth/login**. Request body:

```json
{ "username": "employer", "password": "password123" }
```

Copy the token from the response.

For every request below that needs a token, add this header manually in Scalar:

```
Header name:  Authorization
Header value: Bearer PASTE-YOUR-TOKEN-HERE
```

**Step 2 — create 5 companies**

In Scalar find **POST /api/v{version}/Companies** (version = 1). Add the Authorization header. Send each one separately:

```json
{ "name": "BitCube", "website": "https://bitcube.co.za", "industry": "Technology" }
```

```json
{ "name": "Google", "industry": "Technology" }
```

```json
{ "name": "Amazon", "industry": "Cloud" }
```

```json
{ "name": "Microsoft", "industry": "Software" }
```

```json
{ "name": "Netflix", "industry": "Streaming" }
```

Copy each company `id` from the responses.

**Step 3 — create 10 job listings (2 per company)**

In Scalar find **POST /api/v{version}/Jobs** (version = 1). Add the Authorization header. Replace the companyId with the actual id for each pair:

BitCube jobs:

```json
{
  "title": "Senior Developer",
  "companyId": "PASTE-BITCUBE-ID",
  "location": "Bloemfontein",
  "description": "Build scalable applications for our enterprise platform.",
  "type": "FullTime",
  "salaryMin": 45000,
  "salaryMax": 65000,
  "closingDate": "2027-01-01T00:00:00Z"
}
```

```json
{
  "title": "DevOps Engineer",
  "companyId": "PASTE-BITCUBE-ID",
  "location": "Johannesburg",
  "description": "Manage infrastructure and deployment pipelines for the team.",
  "type": "FullTime",
  "salaryMin": 55000,
  "salaryMax": 75000,
  "closingDate": "2027-01-01T00:00:00Z"
}
```

Google jobs:

```json
{
  "title": "Frontend Developer",
  "companyId": "PASTE-GOOGLE-ID",
  "location": "Cape Town",
  "description": "Build modern web applications using React and TypeScript.",
  "type": "FullTime",
  "salaryMin": 50000,
  "salaryMax": 70000,
  "closingDate": "2027-01-01T00:00:00Z"
}
```

```json
{
  "title": "Data Engineer",
  "companyId": "PASTE-GOOGLE-ID",
  "location": "Bloemfontein",
  "description": "Design and maintain data pipelines for analytics.",
  "type": "FullTime",
  "salaryMin": 60000,
  "salaryMax": 80000,
  "closingDate": "2027-01-01T00:00:00Z"
}
```

Amazon jobs:

```json
{
  "title": "Cloud Architect",
  "companyId": "PASTE-AMAZON-ID",
  "location": "Johannesburg",
  "description": "Design cloud infrastructure solutions for enterprise clients.",
  "type": "FullTime",
  "salaryMin": 70000,
  "salaryMax": 90000,
  "closingDate": "2027-01-01T00:00:00Z"
}
```

```json
{
  "title": "Backend Developer",
  "companyId": "PASTE-AMAZON-ID",
  "location": "Cape Town",
  "description": "Build RESTful APIs and microservices for our platform.",
  "type": "Contract",
  "salaryMin": 40000,
  "salaryMax": 60000,
  "closingDate": "2027-01-01T00:00:00Z"
}
```

Microsoft jobs:

```json
{
  "title": "Software Engineer",
  "companyId": "PASTE-MICROSOFT-ID",
  "location": "Pretoria",
  "description": "Develop enterprise software solutions for our clients.",
  "type": "FullTime",
  "salaryMin": 65000,
  "salaryMax": 85000,
  "closingDate": "2027-01-01T00:00:00Z"
}
```

```json
{
  "title": "QA Engineer",
  "companyId": "PASTE-MICROSOFT-ID",
  "location": "Bloemfontein",
  "description": "Ensure software quality through automated and manual testing.",
  "type": "FullTime",
  "salaryMin": 35000,
  "salaryMax": 55000,
  "closingDate": "2027-01-01T00:00:00Z"
}
```

Netflix jobs:

```json
{
  "title": "Mobile Developer",
  "companyId": "PASTE-NETFLIX-ID",
  "location": "Johannesburg",
  "description": "Build cross-platform mobile applications using React Native.",
  "type": "FullTime",
  "salaryMin": 48000,
  "salaryMax": 68000,
  "closingDate": "2027-01-01T00:00:00Z"
}
```

```json
{
  "title": "Security Engineer",
  "companyId": "PASTE-NETFLIX-ID",
  "location": "Cape Town",
  "description": "Protect our platform and customer data from security threats.",
  "type": "FullTime",
  "salaryMin": 72000,
  "salaryMax": 92000,
  "closingDate": "2027-01-01T00:00:00Z"
}
```

Copy one job `id` — you need it for PATCH and ETag tests.

**Step 4 — seed 200 extra listings for pagination**

Open a new terminal (keep the app running):

```bash
docker exec -it careerhub-postgres psql -U postgres -d CareerHub
```

Paste this SQL and press Enter:

```sql
INSERT INTO job_listings ("Id", "Title", "Description", "CompanyId", "Location", "Type", "PostedAt", "IsActive", "ClosingDate", "Status")
SELECT
  gen_random_uuid(),
  'Developer Role ' || gs,
  'This is a test description for listing number ' || gs || ' at our company.',
  (SELECT "Id" FROM companies ORDER BY RANDOM() LIMIT 1),
  'Bloemfontein',
  'FullTime',
  NOW() - (random() * interval '60 days'),
  true,
  NOW() + (random() * interval '180 days'),
  'Active'
FROM generate_series(1, 200) gs;
```

Expected: `INSERT 0 200`. Type `\q` to exit psql.

---

### Test 4 — Pagination (Scalar)

In Scalar find **GET /api/v{version}/Jobs** (version = 1). Add query parameters:

```
page     = 1
pageSize = 5
```

Click Send.

Expected:
```json
{
  "data": [ ...5 listings... ],
  "page": 1,
  "pageSize": 5,
  "totalCount": 210,
  "totalPages": 42,
  "hasNextPage": true,
  "hasPreviousPage": false
}
```

Check the response headers for `X-Total-Count: 210`. Screenshot.

**Page 2:** change to `page = 2`, `pageSize = 5`. Expected: different listings, `hasPreviousPage: true`. Screenshot.

**Default — no parameters:** Call **GET /api/Jobs** with no parameters. Expected: 20 results, `page: 1`. Screenshot.

---

### Test 5 — Filtering and Sorting (Scalar)

In Scalar find **GET /api/v{version}/Jobs** (version = 1). Add query parameters:

```
employmentType = FullTime
salaryMin      = 50000
sort           = salaryMin
```

Click Send. Expected: every result is FullTime with SalaryMin >= 50000, sorted lowest salary first. Screenshot.

Add one more parameter:

```
dir = desc
```

Click Send. Expected: same results but highest salary first. Screenshot.

---

### Test 6 — PATCH partial update (Scalar)

**Step 1 — get employer token**

In Scalar find **POST /api/Auth/login**. Body:

```json
{ "username": "employer", "password": "password123" }
```

Copy the token.

**Step 2 — note current salary**

In Scalar call **GET /api/v{version}/Jobs/{id}** (version = 1). Paste a job id. Note the current `salaryMin`.

**Step 3 — patch only the salary**

In Scalar find **PATCH /api/v{version}/Jobs/{id}** (version = 1). Set the job id. Add header:

```
Authorization: Bearer PASTE-YOUR-TOKEN
```

Body:

```json
{ "salaryMin": 75000 }
```

Click Send. Expected: 200 OK. Only `salaryMin` changed. Screenshot.

**Step 4 — verify**

Call GET the same job id. Confirm `salaryMin` is now 75000 and nothing else changed. Screenshot.

**Step 5 — invalid salary range**

Same PATCH endpoint with body:

```json
{ "salaryMin": 90000, "salaryMax": 50000 }
```

Expected: 400 Bad Request. Screenshot.

---

### Test 7 — PATCH application status (Scalar)

**Step 1 — apply as applicant1**

In Scalar find **POST /api/Auth/login**. Body:

```json
{ "username": "applicant1", "password": "password123" }
```

Copy the applicant token. In Scalar find **POST /api/v{version}/Applications/{listingId}** (version = 1). Set `listingId` to any job id. Add header:

```
Authorization: Bearer PASTE-APPLICANT-TOKEN
```

No body needed. Click Send. Expected: 201 Created. The applicant id is `a0000000-0000-0000-0000-000000000001`.

**Step 2 — switch to employer token**

In Scalar find **POST /api/Auth/login**. Body:

```json
{ "username": "employer", "password": "password123" }
```

Copy the employer token.

**Step 3 — illegal transition**

In Scalar find **PATCH /api/v{version}/Applications/{listingId}/{applicantId}/status** (version = 1). Set `listingId` to the same job id. Set `applicantId` to `a0000000-0000-0000-0000-000000000001`. Add header:

```
Authorization: Bearer PASTE-EMPLOYER-TOKEN
```

Body:

```json
{ "status": "Offered" }
```

Click Send. Expected: 422 Unprocessable Entity. Screenshot.

**Step 4 — valid transition**

Same endpoint, same header. Body:

```json
{ "status": "UnderReview" }
```

Click Send. Expected: 204 No Content. Screenshot.

---

### Test 8 — ETags (Scalar)

**Step 1 — first request**

In Scalar call **GET /api/v{version}/Jobs/{id}** (version = 1). Use any job id. Click Send.

In the response headers find `ETag`. Copy the full value including the quotes. Example:

```
"fa292fba-638765432100000-45000"
```

Screenshot the response with the ETag header.

**Step 2 — conditional request**

Call the same endpoint again. Add this request header in Scalar:

```
Header name:  If-None-Match
Header value: "fa292fba-638765432100000-45000"
```

Click Send. Expected: **304 Not Modified** — no body returned. Screenshot.

**Step 3 — change the listing**

With employer token call **PATCH /api/v{version}/Jobs/{id}** (version = 1). Add Authorization header. Body:

```json
{ "salaryMin": 80000 }
```

**Step 4 — request with old ETag**

Call **GET /api/v{version}/Jobs/{id}** again with the same old `If-None-Match` header from Step 2.

Expected: **200 OK** — the listing changed so the old ETag no longer matches. Check the response headers for a new `ETag` value. Screenshot.

---

### Test 9 — Rate limiting (Scalar)

**Step 1 — lower the search limit**

Open `Program.cs` in VS Code. Find the search policy and change `PermitLimit` to 2:

```csharp
options.AddSlidingWindowLimiter("search", o =>
{
    o.PermitLimit      = 2;
    o.Window           = TimeSpan.FromSeconds(60);
    o.SegmentsPerWindow = 6;
    o.QueueLimit       = 0;
});
```

Restart the app with `dotnet run`.

**Step 2 — make 3 search requests quickly**

In Scalar find **GET /api/v{version}/Jobs/search** (version = 1). Add query parameter:

```
q = developer
```

Click Send 3 times quickly.

First request: 200 OK.
Second request: 200 OK.
Third request: **429 Too Many Requests**.

On the 429 response check:

Response header: `Retry-After: 60`
Response body: `Rate limit exceeded. Please retry after 60 seconds.`

Screenshot the 429 response.

**Step 3 — restore the limit**

Change `PermitLimit` back to 30 in `Program.cs`. Restart. Confirm search works normally.

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

---

## Screenshots Checklist — Assignment 3.1

| Test | Tool | What to capture |
| --- | --- | --- |
| 1 — CORS | Postman | Response headers showing Access-Control-Allow-Origin, Access-Control-Allow-Credentials, X-Total-Count |
| 2 — Versioning | Postman | /api/jobs 200 + /api/v1/jobs 200 + api-supported-versions header + /api/v2/jobs 404 |
| 3 — Create data | Scalar | Companies created, jobs created, 200 seeded via psql |
| 4 — Pagination | Scalar | Page 1 envelope + Page 2 envelope + X-Total-Count header + default 20 results |
| 5 — Filtering | Scalar | Filtered results + reversed sort with dir=desc |
| 6 — PATCH | Scalar | 200 PATCH + GET before + GET after + 400 invalid salary |
| 7 — Status PATCH | Scalar | 422 illegal transition + 204 valid transition |
| 8 — ETags | Scalar | 200 with ETag + 304 no body + 200 with new ETag |
| 9 — Rate limiting | Scalar | 429 with Retry-After header and plain text body |

---

## Git history

```bash
git log --oneline --graph --all
```
# CareerHub API — Assignment 2.1

Continues from Assignment 1.4. The in-memory job listing store has been replaced with a real PostgreSQL database backed by EF Core 10. Data now survives server restarts, deployments, and failures.

---

## What's inside

```
CareerHub.Api/
├── CareerHub.Api.csproj              the project file (lists the .NET version and packages)
├── Program.cs                        the entry point that starts the app
├── Models/
│   ├── JobListing.cs                 entity class (converted from record for EF Core)
│   └── JobType.cs                    enum — FullTime, PartTime, Contract, Internship
├── Data/
│   └── CareerHubDbContext.cs         EF Core DbContext with Fluent API configuration
├── Migrations/                       auto-generated EF Core migration files
├── DTOs/
│   ├── CreateJobRequest.cs           what the client sends to create a job
│   ├── UpdateJobRequest.cs           what the client sends to replace a job
│   ├── JobResponse.cs                what the API returns (includes SalaryDisplay)
│   ├── LoginRequest.cs               what the client sends to log in
│   └── LoginResponse.cs              what the API returns after a successful login
├── Exceptions/
│   ├── JobNotFoundException.cs       thrown when a job ID does not exist
│   └── DuplicateJobListingException.cs  thrown when title + company already exists
├── Middleware/
│   └── GlobalExceptionHandler.cs    translates exceptions to Problem Details responses
├── Controllers/
│   ├── JobsController.cs             handles all incoming requests to /jobs
│   └── AuthController.cs             handles POST /auth/login and GET /auth/me
└── Properties/
    └── launchSettings.json           tells `dotnet run` to open Scalar in the browser
```

---

## What it does

| Request              | What happens                              | Response                    |
| -------------------- | ----------------------------------------- | --------------------------- |
| `GET /jobs`          | Returns all job listings from the database | 200 OK                     |
| `GET /jobs/{id}`     | Returns one job by id                     | 200 OK if found, 404 if not |
| `POST /jobs`         | Creates a new job listing (Employer only) | 201 Created, 400, or 409    |
| `PUT /jobs/{id}`     | Fully replaces an existing job (Employer) | 200 OK, 400, or 404         |
| `DELETE /jobs/{id}`  | Removes a job listing (Employer only)     | 204 No Content or 404       |
| `POST /auth/login`   | Returns a signed JWT token                | 200 OK or 401               |
| `GET /auth/me`       | Returns current user's username and role  | 200 OK or 401               |

---

## Why I used Controllers and not Minimal APIs

The assignment asked us to pick one and explain why. I went with
**Controllers** because:

- **It's the style we used in class.** Same `[ApiController]`,
  `[Route]`, `[HttpGet]` and `Task<ActionResult<T>>` setup.
- **Each URL is easy to find.** The HTTP verb and the route sit right
  above the method that handles them.
- **`[ApiController]` gives us things for free.** It automatically
  returns a 400 error if someone sends bad input.
- **It scales.** When CareerHub grows, each resource gets its own
  controller file. The pattern stays the same.

---

## Design Decisions

### Why PostedAt belongs in JobResponse but not CreateJobRequest

PostedAt is set by the server at the exact moment a job is created — the client has no say in when that is. If we allowed the client to supply it, they could submit a job with a past date to make it look more established than it is. Since the server owns this value, it makes sense to include it in the response so the frontend can display things like "Posted 3 days ago", but it must never appear in the request DTO.

### Salary cross-field validation approach

Standard Data Annotations like `[Range]` and `[Required]` only inspect a single field in isolation — they cannot compare two fields against each other. To enforce that SalaryMax must be greater than SalaryMin when both are provided, I implemented `IValidatableObject` on the `CreateJobRequest` and `UpdateJobRequest` records. The benefit is that the controller never touches salary logic — if the check fails, `[ApiController]` intercepts it and returns a 400 Problem Details response before the controller method even runs.

### PUT returns 200 OK with body (not 204 No Content)

I chose 200 with the updated `JobResponse` body. The React frontend needs to update its local state after a successful PUT. Returning the updated job means the client immediately has the confirmed server version without making a second GET request.

### DELETE returns 404 for a missing ID

If a client sends DELETE for a job that does not exist, the API returns 404 Not Found rather than 204. A 204 would imply the operation succeeded — but nothing was actually removed, which is misleading.

### Controller Thinning

When you throw `JobNotFoundException` instead of returning `NotFound()`, the controller only cares about one question: does this job exist or not? The `GlobalExceptionHandler` is the single place that decides the HTTP status code and Problem Details shape. You write that mapping once and it works everywhere consistently.

### Structured Logging

`Console.WriteLine` outputs a flat string that cannot be searched or filtered. Serilog writes JSON — each log entry has named fields like `RequestPath`, `StatusCode`, and `Elapsed`. In production, a log aggregator can query "show me all 404s in the last hour" or alert when 500s spike.

### Stateless Authentication — Session vs JWT

JWT is stateless: the token itself contains all the information — who you are, what your role is, and when it expires. Any server can verify a JWT using only the secret key, with no shared database needed. For CareerHub, if we eventually run three server instances behind a load balancer, all three can validate the same token independently.

### 401 Unauthorized vs 403 Forbidden

401 means "I don't know who you are" — no token, expired token, or bad signature. `UseAuthentication()` produces this. 403 means "I know who you are but you are not allowed" — valid token but wrong role. `UseAuthorization()` produces this. This is why `UseAuthentication()` must come before `UseAuthorization()` in the pipeline.

### JWT Token Storage

`localStorage` is accessible to any JavaScript on the page. If the site has an XSS vulnerability, an attacker can steal every token. The safer alternatives are HttpOnly cookies — JavaScript cannot read them at all — or in-memory storage where the token disappears on page refresh.

### The Change Tracker

EF Core's change tracker takes a snapshot of every entity it loads from the database. When you mutate a property — `existingJob.Title = request.Title` — only the in-memory object changes. The snapshot is untouched. When you call `SaveChangesAsync()`, EF Core compares the current state against the snapshot and generates the minimum SQL needed: if only Title changed, only Title is included in the UPDATE statement. You call it once at the end of an operation, not once per property change, because each property change is just an in-memory update. The actual database write — and the transaction that wraps it — happens once when you explicitly ask for it with `SaveChangesAsync()`.

### Migrations as Version Control

A migration file is the SQL definition of your schema changes expressed as C# code. If you commit code that references a new table or column but forget to commit the migration that creates it, your teammate pulls the code and runs the app — but their database does not have that table yet, so every query fails at runtime. Committing the migration alongside the code that requires it ensures everyone on the team has a clear, ordered script to bring their local database up to date. The `__EFMigrationsHistory` table records which migrations have already been applied, so EF Core knows exactly where each environment is and only runs the ones that are missing.

### Connection String Security

`appsettings.json` is committed to source control, which means anyone with access to the repository can read it. Putting a database username and password there is a real security risk. `appsettings.Development.json` is loaded only when `ASPNETCORE_ENVIRONMENT` is `Development` and should be added to `.gitignore` so it is never committed. For production, the safer alternative is environment variables set directly on the server, or a secrets manager like Azure Key Vault, where the credentials never appear in any file at all.

---

## How to Run

### Prerequisites

- **.NET 10 SDK** — check with `dotnet --version`
- **Docker Desktop** — must be running before starting the app

### First time setup

**1. Start PostgreSQL container:**

```bash
docker run -d --name careerhub-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=CareerHub -p 5432:5432 postgres:latest
```

**2. Apply migrations:**

```bash
cd CareerHub.Api
dotnet ef database update
```

**3. Start the app:**

```bash
dotnet run
```

### Every time after that

Always follow this order:

```
1. Open Docker Desktop
2. docker start careerhub-postgres
3. dotnet run
```

Browser opens at **http://localhost:5000/scalar/v1**

The app will crash on startup if Docker is not running — it cannot connect to the database.

---

## How to test the endpoints

In Scalar:

1. **Empty start** — GET /jobs → expect empty array `[]`
2. **Get a token** — POST /auth/login with `username: employer` and `password: password123` → copy the token
3. **Create a job** — POST /jobs with Bearer token → expect 201 Created
4. **Survival test** — stop the app (Ctrl+C), restart with `dotnet run`, GET /jobs → job is still there
5. **Duplicate guard** — POST the same job again → expect 409 Conflict
6. **Not found** — GET /jobs/{random-guid} → expect 404 Not Found
7. **Delete** — DELETE /jobs/{id} with Bearer token → expect 204, then GET same id → expect 404

---

## A note on async/await

Every endpoint is marked `async`. All database operations use `await` — `ToListAsync()`, `FindAsync()`, `SaveChangesAsync()`. This tells .NET to release the thread while waiting for the database, keeping the API responsive under load.

---

## Git history

To see the branches visually:

```bash
git log --oneline --graph --all
```

---

## How to Test the Endpoints — Assignment 2.2

### Prerequisites

Make sure Docker is running and the app is started:

```bash
docker start careerhub-postgres
cd CareerHub.Api
dotnet run
```

Open **http://localhost:5000/scalar/v1**

---

### Step 1 — Get an Employer token

**POST /Auth/login**

```json
{
  "username": "employer",
  "password": "password123"
}
```

Copy the token from the response. You will use it for all employer actions.

---

### Step 2 — Create a company

**POST /Companies** — add `Authorization: Bearer <employer-token>` header

```json
{ "name": "BitCube", "website": "https://bitcube.co.za", "industry": "Technology" }
```

Expected: **201 Created**. Copy the `id` from the response — this is your `companyId`.

Repeat this step to create 4 more companies (needed for the N+1 test):

```json
{ "name": "Google", "industry": "Technology" }
{ "name": "Amazon", "industry": "Cloud" }
{ "name": "Microsoft", "industry": "Software" }
{ "name": "Netflix", "industry": "Streaming" }
```

---

### Step 3 — Create job listings (one per company)

**POST /Jobs** — add `Authorization: Bearer <employer-token>` header

Create one listing per company using each company's ID:

```json
{
  "title": "Senior Developer",
  "companyId": "PASTE-COMPANY-ID-HERE",
  "location": "Bloemfontein",
  "description": "Build scalable .NET applications for our enterprise platform.",
  "type": "FullTime",
  "salaryMin": 45000,
  "salaryMax": 65000
}
```

Expected: **201 Created** for each. Copy one `id` — you will use it for the apply test.

---

### Step 4 — Verify the list endpoint (N+1 fix proof)

**GET /Jobs** — no token needed

Expected: **200 OK** with all listings. Each listing shows `companyName` and `applicationCount`.

Check the terminal — you should see **one SQL statement** with JOIN clauses. This proves the N+1 fix is working.

---

### Step 5 — Apply as applicant1

**POST /Auth/login** with applicant credentials:

```json
{ "username": "applicant1", "password": "password123" }
```

Copy the applicant token.

**POST /Jobs/{id}/apply** — paste the job `id` in the URL, use the applicant token, no body needed.

Expected: **201 Created** with application details including `status: "Submitted"`.

---

### Step 6 — Verify the application appears

**GET /Jobs/{id}** — paste the same job id, no token needed.

Expected: **200 OK** with an `applications` array containing:
- `applicantName`: Alice Smith
- `submittedAt`: the timestamp
- `status`: Submitted

---

### Step 7 — Duplicate application (409 test)

**POST /Jobs/{id}/apply** again with the same applicant1 token and same job id.

Expected: **409 Conflict** — "You have already applied for this job listing."

---

### Step 8 — Apply as applicant2 (different caller test)

**POST /Auth/login** with applicant2 credentials:

```json
{ "username": "applicant2", "password": "password123" }
```

**POST /Jobs/{id}/apply** — same job id, but now using the applicant2 token.

Expected: **201 Created** — Bob Jones successfully applied to the same job.

**GET /Jobs/{id}** — confirm the `applications` array now shows both Alice Smith and Bob Jones.

---

### Step 9 — Schema correctness (database check)

Open a new terminal and run:

```bash
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "\d companies"
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "\d applicants"
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "\d applications"
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "\d job_listings"
```

Confirm:
- `companies` has a unique index on `Name` and is referenced by `job_listings`
- `applications` has a composite primary key on `(ApplicantId, JobListingId)`
- `job_listings` has a FK to `companies` with `ON DELETE RESTRICT`

---

### Step 10 — Relationship enforcement (database rejects bad data)

Try to insert a job listing with a company ID that does not exist:

```bash
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "INSERT INTO job_listings (\"Id\", \"Title\", \"Description\", \"CompanyId\", \"Location\", \"Type\", \"PostedAt\", \"IsActive\") VALUES (gen_random_uuid(), 'Test', 'Test description', '00000000-0000-0000-0000-000000000099', 'Joburg', 'FullTime', NOW(), true);"
```

Expected: database rejects it with a foreign key violation error.

---

### Credentials summary

| Username | Password | Role | What they can do |
| -------- | -------- | ---- | ---------------- |
| employer | password123 | Employer | Create companies, post jobs, update, delete |
| applicant1 | password123 | Applicant | Apply for jobs (Alice Smith) |
| applicant2 | password123 | Applicant | Apply for jobs (Bob Jones) |



## How to Test — Assignment 2.2

### Prerequisites

```bash
docker start careerhub-postgres
cd CareerHub.Api
dotnet run
```

Open **http://localhost:5000/scalar/v1**

---

### Credentials

| Username | Password | Role | Who they are |
| --- | --- | --- | --- |
| employer | password123 | Employer | Posts and manages jobs |
| applicant1 | password123 | Applicant | Alice Smith |
| applicant2 | password123 | Applicant | Bob Jones |

---

### Test 1 — Schema correctness

Open a new terminal and run:

```bash
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "\d companies"
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "\d applicants"
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "\d applications"
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "\d job_listings"
```

**What to confirm:**
- `companies` has a unique index on `Name` and is referenced by `job_listings`
- `applications` has a composite primary key on `(ApplicantId, JobListingId)`
- `job_listings` has a FK to `companies` with `ON DELETE RESTRICT`
- `applicants` table exists with `applicant1` and `applicant2` already seeded

---

### Test 2 — Relationship enforcement

Try to insert a job with a company ID that does not exist:

```bash
docker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "INSERT INTO job_listings (\"Id\", \"Title\", \"Description\", \"CompanyId\", \"Location\", \"Type\", \"PostedAt\", \"IsActive\") VALUES (gen_random_uuid(), 'Test', 'Test description here', '00000000-0000-0000-0000-000000000099', 'Joburg', 'FullTime', NOW(), true);"
```

**Expected:** database rejects it with a foreign key violation error.

---

### Test 3 and 4 — N+1 proof (before and after)

#### Step 1 — Enable query logging

Open `Program.cs` and update `AddDbContext`:

```csharp
builder.Services.AddDbContext<CareerHubDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
           .LogTo(Console.WriteLine, LogLevel.Information)
);
```

Restart the app with `dotnet run`.

---

#### Step 2 — Show the N+1 BEFORE fix

Open `Controllers/JobsController.cs` and temporarily replace `GetJobsAsync` with this:

```csharp
[HttpGet]
public async Task<ActionResult> GetJobsAsync(CancellationToken cancellationToken)
{
    // TEMPORARY — shows N+1 — remove after demo
    var jobs = await db.JobListings.ToListAsync(cancellationToken);
    var result = new List<object>();
    foreach (var job in jobs)
    {
        var company = await db.Companies.FindAsync([job.CompanyId], cancellationToken);
        result.Add(new { job.Title, CompanyName = company?.Name ?? "" });
    }
    return Ok(result);
}
```

Restart the app. Create 5 companies and 5 job listings (one per company) first — see Test 6 below for the steps. Then call **GET /Jobs**.

**What you see in the terminal — multiple queries:**

```
SELECT j."Id", j."Title", j."CompanyId", ...
FROM job_listings AS j

SELECT c."Id", c."Name", ...
FROM companies AS c
WHERE c."Id" = '...' LIMIT 1

SELECT c."Id", c."Name", ...
FROM companies AS c
WHERE c."Id" = '...' LIMIT 1

-- one query per job listing — this is the N+1 problem
```

**Screenshot this terminal output — this is the BEFORE the examiner needs to see.**

---

#### Step 3 — Show the fix AFTER

Restore the original `GetJobsAsync` from the zip:

```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<JobResponse>>> GetJobsAsync(
    CancellationToken cancellationToken)
{
    var rawJobs = await db.JobListings
        .AsNoTracking()
        .OrderByDescending(j => j.PostedAt)
        .Select(j => new
        {
            j.Id,
            j.Title,
            j.Description,
            CompanyName      = j.Company.Name,
            j.Location,
            j.Type,
            j.SalaryMin,
            j.SalaryMax,
            j.PostedAt,
            j.IsActive,
            ApplicationCount = j.Applications.Count()
        })
        .ToListAsync(cancellationToken);

    return Ok(rawJobs.Select(j => new JobResponse(
        j.Id, j.Title, j.Description, j.CompanyName,
        j.Location, j.Type, j.SalaryMin, j.SalaryMax,
        ComputeSalaryDisplay(j.SalaryMin, j.SalaryMax),
        j.PostedAt, j.IsActive, j.ApplicationCount
    )));
}
```

Restart the app and call **GET /Jobs** again.

**What you see in the terminal — one query with JOINs:**

```
SELECT j."Id", j."Title", j."Description",
       c."Name" AS company_name,
       j."Location", j."Type", j."SalaryMin", j."SalaryMax",
       j."PostedAt", j."IsActive",
       COUNT(a."JobListingId") AS application_count
FROM job_listings AS j
LEFT JOIN companies AS c ON j."CompanyId" = c."Id"
LEFT JOIN applications AS a ON a."JobListingId" = j."Id"
GROUP BY j."Id", c."Name"
ORDER BY j."PostedAt" DESC
```

**Screenshot this terminal output — this is the AFTER.**

#### Step 4 — Remove the logging

Remove `.LogTo(Console.WriteLine, LogLevel.Information)` from `Program.cs` before committing.

---

### Test 5 — Projection proof

Before removing the logging, call **GET /Jobs** and look at the SQL in the terminal.

**Confirm** the SELECT clause only lists the columns in `JobResponse` — no applicant email, no company website, no extra columns from joined tables.

---

### Test 6 — Create companies and job listings

**POST /Auth/login** — get employer token:
```json
{ "username": "employer", "password": "password123" }
```

**POST /Companies** — repeat for each (use employer token):
```json
{ "name": "BitCube", "website": "https://bitcube.co.za", "industry": "Technology" }
{ "name": "Google", "industry": "Technology" }
{ "name": "Amazon", "industry": "Cloud" }
{ "name": "Microsoft", "industry": "Software" }
{ "name": "Netflix", "industry": "Streaming" }
```

Copy each company `id` from the responses.

**POST /Jobs** — one per company (use employer token):
```json
{
  "title": "Senior Developer",
  "companyId": "PASTE-COMPANY-ID-HERE",
  "location": "Bloemfontein",
  "description": "Build scalable .NET applications for our enterprise platform.",
  "type": "FullTime",
  "salaryMin": 45000,
  "salaryMax": 65000
}
```

Expected: **201 Created** for each. Copy one job `id` for the apply tests.

---

### Test 7 — Application tracking

**POST /Auth/login** — get applicant token:
```json
{ "username": "applicant1", "password": "password123" }
```

**POST /Jobs/{id}/apply** — paste a job id in the URL, use applicant token. No body needed.

Expected: **201 Created**
```json
{
  "message": "Application submitted successfully.",
  "jobListingId": "...",
  "applicantId": "a0000000-0000-0000-0000-000000000001",
  "submittedAt": "2026-06-04T...",
  "status": "Submitted"
}
```

**GET /Jobs/{id}** — no token needed.

Expected: **200 OK** with applications array:
```json
{
  "id": "...",
  "title": "Senior Developer",
  "companyName": "BitCube",
  "applications": [
    {
      "applicantName": "Alice Smith",
      "submittedAt": "2026-06-04T...",
      "status": "Submitted"
    }
  ]
}
```

---

### Test 8 — Duplicate application

**POST /Jobs/{id}/apply** again — same applicant1 token, same job id.

Expected: **409 Conflict**
```json
{
  "status": 409,
  "title": "Resource Conflict",
  "detail": "You have already applied for this job listing."
}
```

---

### Test 9 — Different caller applies

**POST /Auth/login** — get applicant2 token:
```json
{ "username": "applicant2", "password": "password123" }
```

**POST /Jobs/{id}/apply** — same job id, applicant2 token.

Expected: **201 Created** — Bob Jones applied successfully.

**GET /Jobs/{id}** — confirm both applicants appear:
```json
{
  "applications": [
    { "applicantName": "Alice Smith", "status": "Submitted" },
    { "applicantName": "Bob Jones",   "status": "Submitted" }
  ]
}
```

---

Seeded Applicants
Two applicants are automatically inserted into the database when migrations are applied. To verify they exist:

bashdocker exec -it careerhub-postgres psql -U postgres -d CareerHub -c "SELECT * FROM applicants;"

Expected output:
                  Id                  |    Name     |        Email         | Username
--------------------------------------+-------------+----------------------+------------
 a0000000-0000-0000-0000-000000000001 | Alice Smith | alice@example.com    | applicant1
 a0000000-0000-0000-0000-000000000002 | Bob Jones   | bob@example.com      | applicant2
If the table is empty it means the migration was applied before the seed data was added. Fix it by running:
bashdotnet ef migrations add SeedApplicants
dotnet ef database update
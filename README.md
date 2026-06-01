# CareerHub API — Assignment 1.3

Continues from Assignment 1.2. Error handling is now centralised — controllers throw domain exceptions, and a single GlobalExceptionHandler translates them into RFC 7807 Problem Details responses. Serilog replaces the default logger with structured, queryable output.

---

## What's inside

```
CareerHub.Api/
├── CareerHub.Api.csproj              the project file (lists the .NET version and packages)
├── Program.cs                        the entry point that starts the app
├── Models/
│   ├── JobListing.cs                 a record that defines what a job 

│   └── JobType.cs                    enum — FullTime, PartTime, 

├── Data/
│   └── JobListingStore.cs            the fake "database" — 4 jobs kept in memory
├── DTOs/
│   ├── CreateJobRequest.cs           what the client sends to create a job
│   ├── UpdateJobRequest.cs           what the client sends to replace a job
│   └── JobResponse.cs                what the API returns (includes SalaryDisplay)
├── Exceptions/
│   ├── JobNotFoundException.cs       thrown when a job ID does not exist
│   └── DuplicateJobListingException.cs  thrown when title + company already exists
├── Middleware/
│   └── GlobalExceptionHandler.cs    translates exceptions to Problem Details responses
├── Controllers/
│   └── JobsController.cs             handles all incoming requests to /jobs
└── Properties/
    └── launchSettings.json           tells `dotnet run` to open Scalar in the browser
```

---

## What it does

| Request             | What happens                        | Response                    |
| ------------------- | ----------------------------------- | --------------------------- |
| `GET /jobs`         | Returns all job listings            | 200 OK                      |
| `GET /jobs/{id}`    | Returns one job by id               | 200 OK if found, 404 if not |
| `POST /jobs`        | Creates a new job listing           | 201 Created, 400, or 409    |
| `PUT /jobs/{id}`    | Fully replaces an existing job      | 200 OK, 400, or 404         |
| `DELETE /jobs/{id}` | Removes a job listing               | 204 No Content or 404       |

The `id` is a GUID (a long random string of letters and numbers,
e.g. `c0a80101-7d9b-4f3a-8c4e-12345abc6def`).

---

## Why I used Controllers and not Minimal APIs

The assignment asked us to pick one and explain why. I went with
**Controllers** because:

- **It's the style we used in class.** Same `[ApiController]`,
  `[Route]`, `[HttpGet]` and `Task<ActionResult<T>>` setup. Doing it
  differently would make the code harder to compare to my class notes.
- **Each URL is easy to find.** The HTTP verb and the route sit right
  above the method that handles them.
- **`[ApiController]` gives us things for free.** It automatically
  returns a 400 error if someone sends bad input, and it figures out
  where each input comes from (route, query string, body) without us
  having to spell it out.
- **It scales.** When CareerHub grows to have Users, Companies and
  Applications, each one gets its own controller file. The pattern
  stays the same.

Minimal APIs would have worked too.

---

## Design Decisions

### Why PostedAt belongs in JobResponse but not CreateJobRequest

PostedAt is set by the server at the exact moment a job is created — the client has no say in when that is. If we allowed the client to supply it, they could submit a job with a past date to make it look more established than it is. Since the server owns this value, it makes sense to include it in the response so the frontend can display things like "Posted 3 days ago", but it must never appear in the request DTO.

### Salary cross-field validation approach

Standard Data Annotations like `[Range]` and `[Required]` only inspect a single field in isolation — they cannot compare two fields against each other. To enforce that SalaryMax must be greater than SalaryMin when both are provided, I implemented `IValidatableObject` on the `CreateJobRequest` and `UpdateJobRequest` records. This interface adds a `Validate()` method that runs automatically after all individual annotations pass. The benefit is that the controller never touches salary logic — if the check fails, `[ApiController]` intercepts it and returns a 400 Problem Details response before the controller method even runs.

### PUT returns 200 OK with body (not 204 No Content)

I chose 200 with the updated `JobResponse` body. The React frontend needs to update its local state after a successful PUT. Returning the updated job means the client immediately has the confirmed server version without making a second GET request to find out what was actually saved. 204 would be semantically clean but would force an extra round-trip on every edit.

### DELETE returns 404 for a missing ID

If a client sends DELETE for a job that does not exist, the API returns 404 Not Found rather than 204. A 204 would imply the operation succeeded — but nothing was actually removed, which is misleading. On a job board, a recruiter managing their listings should know if the job they are trying to delete is already gone — perhaps a colleague removed it, or the client has a stale ID. The 404 provides that useful signal instead of silently pretending the deletion happened.

### Controller Thinning

In Assignment 1.2, every controller method that could fail had to know about HTTP. Writing `return NotFound()` in three different places means three different points where the error shape could differ — one developer adds a detail message, another forgets, a third uses the wrong status code. When you throw `JobNotFoundException` instead, the controller only cares about one question: does this job exist or not? The `GlobalExceptionHandler` is the single place that decides "JobNotFoundException always means 404 with exactly this Problem Details shape." You write that mapping once and it works everywhere, consistently. The controller becomes pure business logic with no web-layer concerns mixed in.

### Structured Logging

`Console.WriteLine` outputs a flat string that cannot be searched, filtered, or alerted on. Serilog writes JSON instead — each log entry is an object with named fields like `RequestPath`, `StatusCode`, `Elapsed`, and `ExceptionType`. In production, a log aggregator like Seq or Datadog can ingest those JSON entries and let you query "show me all 404s in the last hour" or "alert when 500s spike above 10 per minute." With flat strings, that kind of analysis is impossible.

---

### Stateless Authentication — Session vs JWT

Session-based authentication stores the user's login state on the server. Every request hits the server, which looks up the session from a database or memory store. This works fine for a single server, but when you scale horizontally — adding more servers to handle more traffic — each server has its own memory and does not know about sessions stored on another. You would need a shared session store just to make authentication work across instances.

JWT is stateless: the token itself contains all the information — who you are, what your role is, and when it expires. Any server can verify a JWT using only the secret key, with no shared database needed. For CareerHub, if we eventually run three server instances behind a load balancer, all three can validate the same token independently without talking to each other.

---

### 401 Unauthorized vs 403 Forbidden

401 Unauthorized means "I don't know who you are." The client has not sent a token, the token is expired, or the signature does not match. `UseAuthentication()` produces this — before authorisation even runs, the request fails identity verification.

403 Forbidden means "I know exactly who you are, but you are not allowed to do this." The token is valid and the identity is confirmed, but the role on the token does not match what the endpoint requires. `UseAuthorization()` produces this — it only runs after authentication has already succeeded. This is why middleware order matters: `UseAuthentication()` must come before `UseAuthorization()`.

---

### JWT Token Storage

`localStorage` is accessible to any JavaScript running on the page. If the site has even a small XSS vulnerability, an attacker's injected script can read every token in `localStorage` and use it to impersonate the user from anywhere. The safer alternatives are HttpOnly cookies — the browser sends them automatically with every request but JavaScript cannot read them at all, so an XSS attack cannot steal what it cannot see — or in-memory storage, where the token lives only in a JavaScript variable for the page's lifetime and disappears on refresh.

## How to run it

You need the **.NET 10 SDK** installed. Check what you have:

```bash
dotnet --version
```

If it doesn't start with `10.`, install it from
https://dotnet.microsoft.com/download/dotnet/10.0

Then start the app:

```bash
cd CareerHub.Api
dotnet run
```

A browser tab should open automatically at:

> **http://localhost:5000/scalar/v1**

That's the **Scalar UI** — a tool that lets you test the endpoints
straight from the browser, no Postman needed.

Watch the terminal — Serilog will print structured output like:

```
[10:42:15 INF] Starting up the CareerHub API...
[10:42:16 INF] HTTP GET /jobs responded 200 in 12.4 ms
```

---

## How to test the endpoints

In Scalar:

1. **Validation failure** — POST with an empty body → expect 400 Problem Details.
2. **Salary cross-field failure** — POST with SalaryMax less than SalaryMin → expect 400.
3. **Successful creation** — POST a valid job → copy the URL from the Location header → GET it → confirm PostedAt and SalaryDisplay appear correctly.
4. **Duplicate guard** — POST the exact same job again → expect 409 Conflict with the custom message.
5. **Not found** — GET or PUT with a random GUID → expect 404 Problem Details (thrown by controller, handled by GlobalExceptionHandler).
6. **Deletion** — DELETE a job → GET it → expect 404.
7. **Error logging** — after triggering a 404 or 409, check the terminal for the GlobalExceptionHandler log entry.

---

## A note on async/await

Every endpoint is marked `async` even though reading from memory
doesn't actually need to be async. Why?

- `async` and `await` tell .NET to **let go of the thread** while it
  waits for slow things (like a database call). That keeps the API
  responsive when lots of people hit it at once.
- We don't have a database yet, so there's nothing slow to wait for.
  I added `await Task.Delay(200)` to **pretend** there's a slow thing.
- When a real database is added later, `Task.Delay(200)` gets swapped
  for something like `await _dbContext.Jobs.ToListAsync()` — and the
  rest of the code doesn't change.

---

## Git history

To see the branches visually:

```bash
git log --oneline --graph --all
```
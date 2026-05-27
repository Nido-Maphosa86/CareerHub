# CareerHub API — Assignment 1.1

A small .NET 10 Web API that returns a list of fake job postings.
This is the first part of the CareerHub job board backend.

---

## What's inside

```
CareerHub.Api/
├── CareerHub.Api.csproj       the project file (lists the .NET version and packages)
├── Program.cs                 the entry point that starts the app
├── JobListingStore.cs         the fake "database" — 4 jobs kept in memory
├── Models/
│   └── JobListing.cs          a record that defines what a job looks like
├── Controllers/
│   └── JobsController.cs      handles incoming requests to /jobs
└── Properties/
    └── launchSettings.json    tells `dotnet run` to open Scalar in the browser
```

---

## What it does

The API has two endpoints:

| Request           | What happens                  | Response                      |
| ----------------- | ----------------------------- | ----------------------------- |
| `GET /jobs`       | Returns all 4 jobs            | 200 OK                        |
| `GET /jobs/{id}`  | Returns one job by id         | 200 OK if found, 404 if not   |

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

---

## How to test the endpoints

In Scalar:

1. Click **Jobs** in the left sidebar.
2. Click **GET /Jobs** → **Send**. You should see 4 jobs come back.
   Copy the `id` of any one of them.
3. Click **GET /Jobs/{id}** → paste the `id` you just copied → **Send**.
   You should see that single job. (200 OK)
4. Change a few characters of the `id` and click **Send** again. You
   should now see a "Not Found" error. (404)



---

##  async/await

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


To see the branches visually:

```bash
git log --oneline --graph --all
```
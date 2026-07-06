# CareerHub — Full Setup Guide

CareerHub is a job board built as two separate applications that run together:

- **CareerHub.Api** — a .NET 10 REST API backed by PostgreSQL (handles jobs, applications, and auth).
- **careerhub__frontend** — a Next.js 15 (App Router, TypeScript) web app that people actually use in the browser.

This guide takes you from nothing installed to a working app in the browser. Follow it top to
bottom — every command is here, with no steps left to guesswork.

> **Time to complete:** about five minutes once the prerequisites are installed.

---

## 1. Prerequisites

Install these three tools first. Version-check commands are included so you can confirm each one.

| Tool | Why it is needed | Check it is installed |
|---|---|---|
| **.NET 10 SDK** | Runs the backend API | `dotnet --version` (should print 10.x) |
| **Node.js 20+** | Runs the frontend | `node --version` (should print v20 or higher) |
| **Docker Desktop** | Runs the PostgreSQL database | `docker --version`, then open the app |

**Docker Desktop must be running** before you start anything. Open it and wait until its status
indicator is green ("Engine running"). The backend cannot connect to the database if Docker is off.

---

## 2. Clone  repositories

Open a terminal in the folder where you keep your projects and clone both repos side by side:

```bash
git clone https://github.com/Nido-Maphosa86/CareerHub.git
```

The frontend lives inside that same repository, in the `careerhub__frontend` folder, so a single
clone gives you both the API and the web app:

```
CareerHub/
├── CareerHub.Api/          ← the .NET backend
└── careerhub__frontend/    ← the Next.js frontend
```

---

## 3. Start the database (Docker)

The backend needs a PostgreSQL database. Start it in a container.

**First time only** — create the container:

```bash
docker run -d --name careerhub-postgres -e POSTGRES_PASSWORD=postgres -e POSTGRES_DB=CareerHub -p 5432:5432 postgres:16
```

**Every time after that** — just start the existing container:

```bash
docker start careerhub-postgres
```

Confirm it is running:

```bash
docker ps
```

You should see `careerhub-postgres` listed with port `5432`.

---

## 4. Start the backend (CareerHub.Api)

Open a terminal in the API folder:

```bash
cd CareerHub/CareerHub.Api
```

### 4a. Backend environment variables

The API reads its secrets from environment variables / user secrets, not from committed files. Set
the JWT signing key (used to issue login tokens). From the `CareerHub.Api` folder:

```bash
dotnet user-secrets set "Jwt:Key" "REPLACE_WITH_ANY_LONG_RANDOM_STRING_AT_LEAST_32_CHARS"
```

The database connection string already points at the Docker container above
(`Host=localhost;Port=5432;Database=CareerHub;Username=postgres;Password=postgres`). If your Postgres
uses a different password, update it in `appsettings.Development.json`.

### 4b. Create the database tables

Apply the migrations once to build the schema:

```bash
dotnet ef database update
```

> If `dotnet ef` is not recognised, install the tool once with:
> `dotnet tool install --global dotnet-ef`

### 4c. Run the API
 
```bash
cd CareerHub.Api
dotnet run
```

Leave this terminal open. The API is now live at **http://localhost:5000**. You can confirm it by
opening the API docs:

```
http://localhost:5000/openapi/v1.json
```

If that returns JSON, the backend is working.

---

## 5. Start the frontend (careerhub__frontend)

Open a **second** terminal (leave the backend running in the first one):

```bash
cd CareerHub/careerhub__frontend
```

### 5a. Install dependencies

```bash
npm install
```




### 5c. Run the frontend

```bash
npm run dev
```

Open **http://localhost:3000** in your browser. The app is now running.

---

## 6. Logging in (test accounts)

The app ships with four ready-made accounts for testing. Go to **http://localhost:3000/login** and
use any of these — all share the password **`password123`**:

| Username | Password | Role | Lands on |
|---|---|---|---|
| `employer1` | `password123` | Employer | Dashboard |
| `employer2` | `password123` | Employer | Dashboard |
| `alice` | `password123` | Candidate | Jobs |
| `bob` | `password123` | Candidate | Jobs |

- Sign in as **alice** to browse jobs and apply.
- Sign in as **employer1** to manage listings from the dashboard.

---

## 7. Quick start (returning users)

Once everything is installed and configured, the daily startup is just four steps:

```bash
# 1. Open Docker Desktop (wait for green)

# 2. Start the database
docker start careerhub-postgres

# 3. Start the backend  (terminal 1)
cd CareerHub/CareerHub.Api && dotnet run

# 4. Start the frontend (terminal 2)
cd CareerHub/careerhub__frontend && npm run dev
```

Then open **http://localhost:3000**.

---s

## 8. Verifying it works

- `http://localhost:3000` shows the CareerHub home page.
- `http://localhost:3000/jobs` lists jobs pulled from the backend.
- Signing in as `alice` and opening a job shows a working multi-step application form.
- Signing in as `employer1` shows the dashboard with listing controls.

### Run the tests (optional)

From `careerhub__frontend`:

```bash
npm run test:run
```

All 13 tests should pass and the runner should exit on its own.

---

## 9. Troubleshooting

| Symptom | Cause | Fix |
|---|---|---|
| Backend crashes on startup, cannot connect to database | Docker or the Postgres container is not running | Open Docker Desktop, then `docker start careerhub-postgres` |
| `dotnet ef` not recognised | EF Core tool not installed | `dotnet tool install --global dotnet-ef` |
| Frontend loads but `/jobs` is empty or errors | Backend not running, or wrong `NEXT_PUBLIC_API_URL` | Confirm the backend is up at `http://localhost:5000` and the env value ends in `/api/v1` |
| `Module not found` after cloning | Dependencies not installed | Run `npm install` inside `careerhub__frontend` |
| Login always fails | `AUTH_SECRET` missing from `.env.local` | Generate one (section 5b) and restart `npm run dev` |
| Port 3000 already in use | Another app is using it | Next.js will offer the next free port; use the URL it prints |
| Solution file errors on the backend | `.slnx` corrupted (known on shared machines) | `del CareerHub.slnx`, then `dotnet new sln`, then `dotnet sln add` for both projects |

---

## 10. Project structure at a glance

```
CareerHub/
├── CareerHub.Api/                 .NET 10 API
│   ├── Controllers/               Jobs, Applications, Auth, Companies
│   ├── Migrations/                EF Core database schema history
│   └── appsettings.json           Config (connection string, etc.)
│
└── careerhub__frontend/           Next.js 15 web app
    ├── src/app/                    Routes (home, jobs, dashboard, login)
    ├── src/components/             UI (wizard, cards, dialogs, boundaries)
    ├── src/lib/                    API client + typed error handling
    └── src/test/                   Vitest + Testing Library test suite
```

That is everything needed to clone, configure, and run the full CareerHub stack.
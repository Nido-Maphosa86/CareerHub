# CareerHub Frontend

A Next.js 15 frontend for the CareerHub job board API. Built with TypeScript, Tailwind CSS v4, and TanStack Query.

## Assignment progress

This frontend is built across multiple assignments. Each one adds a layer on top of the previous.

### Assignment 1.1 — Project setup and types

Set up a Next.js 15 project with the App Router, TypeScript, and Tailwind v4. Defined the shared `JobListing` interface and `EmploymentType` union in `src/types/index.ts`. Built a basic page that rendered a hardcoded array of jobs as plain cards.

### Assignment 1.2 — Components, badges, theme, selection

Built the visual layer.

- `cn` utility in `src/lib/utils.ts` (combines `clsx` and `tailwind-merge`)
- shadcn-style `Badge` component in `src/components/ui/badge.tsx` with six custom CareerHub variants for each employment type
- `JobStatusBadge` that maps an `EmploymentType` to a badge variant using a typed `Record`
- `JobCard` and `JobList` components
- `ThemeToggle` that reads `localStorage` first, then falls back to the OS preference
- Dark mode wired up via the `@custom-variant dark` directive in `globals.css`
- Card selection persisted to `sessionStorage` using two separate `useEffect` calls — one to restore on mount, one to persist when the selection changes

### Assignment 1.3 — Data fetching with TanStack Query

# Part 1 — Written Decisions

## 1. Server state vs client state

A manual `useEffect + useState + fetch` setup may look like it does the same job as `useQuery`, but it only handles the simplest case: fetching data once and showing it. TanStack Query handles many real-world situations that happen when users interact with the app. Here are four important things it gives you automatically.

**Caching across components and pages.** TanStack Query saves data using a `queryKey` and shares it with any component that uses the same key. For example, if a user goes from the job list page to a job details page and then back, the job list shows immediately from cache while it quietly checks for updates in the background. With manual `useEffect`, every time the component loads, it sends a new request. The result is that the page keeps showing loading screens again and again, even when data has not changed. This makes the app feel slow and annoying.

**Request deduplication.** If two components on the same page request `["jobs"]`, TanStack Query sends only one request and shares the result. With a manual setup, two identical requests are sent at the same time. This wastes data, slows down the page (especially on mobile), and puts extra load on the server.

**Automatic refetch on window focus and network reconnect.** If a user leaves the tab and comes back later, TanStack Query checks if the data is still fresh and updates it in the background if needed. The old data stays visible during this process. With manual `useEffect`, the data never updates unless the page is refreshed. This means users might see outdated information, like a job that is already closed still showing as open.

**Built-in loading, error, and success state.** `useQuery` gives you `isPending`, `isError`, `error`, and `data`, and they always stay consistent. A manual setup needs multiple `useState` variables and a `try / catch / finally` block, and it is easy to make mistakes. For example, you might forget to stop loading when an error happens. This can cause issues like a spinner that never stops or an error message that stays even after success.

## 2. The queryKey contract

TanStack Query uses the `queryKey` as the **unique ID for a piece of server data in the cache**. Every query is saved under its key, and all actions (read, update, refetch, delete) use this key. It also decides if two `useQuery` calls are the same or different based on the key. Same key = shared data and request. Different key = separate queries.

**Failure mode A — two components share a key they should not share.**
Example: A `JobList` page uses `["jobs"]` to fetch all jobs, and a `MyApplications` page also uses `["jobs"]` but fetches only applied jobs. The keys are the same, but the data is different. TanStack Query treats them as the same, so one result overwrites the other. The user may see wrong data depending on which page was opened first. The fix is to use clear keys like `["jobs", "all"]` and `["jobs", "applied", userId]`.

**Failure mode B — a component uses a unique key when it should share one.**
Example: A navbar shows job count using `["jobsCount"]`, while the main page uses `["jobs"]`, but both call the same API. TanStack Query treats them as different and sends two requests. The user may briefly see different numbers in different parts of the app. The fix is to use the same key so they share data.

## 3. Why fetch does not throw on HTTP errors

The `fetch` API treats HTTP errors (like 404, 500) as **successful responses that contain errors**. From its view, the request worked because the server responded. It only fails if the request never reaches the server (like no internet, DNS issues, or blocked requests).

That is why we check `res.ok` (true for status 200–299) and throw an error manually when it is false.

If we remove the `res.ok` check, this happens:
If the API returns a 500 error with `{ "error": "Database connection failed" }`, `fetch` still succeeds. The JSON is read correctly, and that error object is returned as data. TanStack Query thinks it is valid data, so `isError` stays false and the error is not handled properly.

The result for the user is bad:

* The app may crash (e.g., trying to use `.map` on an object)
* Or it may show empty results with no error message

The error UI will never show because no error was thrown. That is why checking `!res.ok` is very important.

## 4. Stale-while-revalidate

With TanStack Query’s default `staleTime` (0), when a user returns to the tab, the app marks data as stale and fetches new data in the background — **but keeps the old data visible the whole time**. There is no loading screen or flicker. When new data arrives, it updates smoothly. If nothing changed, the user sees no difference.

This is called "stale-while-revalidate": show old data immediately, update it in the background, then replace it when ready. The user always sees something useful.

With a normal `useEffect(..., [])`, data loads only once when the component starts. Returning to the tab does nothing, so the user may see outdated data. If the user leaves and comes back (causing reload), the app fetches again but resets data first, showing loading screens again.

So the user either sees outdated data or constant reloading, with no smooth experience. TanStack Query solves this by giving a balanced and better experience.





Replaced the hardcoded array with a real data-fetching layer.

**What was added:**

1. **TanStack Query installed** — `@tanstack/react-query` plus `@tanstack/react-query-devtools` for development.

2. **Environment variable** — `.env.local` with `NEXT_PUBLIC_API_URL` so the API base URL is configurable. Today it points at the mock route handler. When the real CareerHub backend is ready, only this one line changes.

3. **Mock backend route handler** — `src/app/api/jobs/route.ts` is a Next.js Route Handler that returns seed `JobListing` data as JSON at `GET /api/jobs`. This lets the frontend be built before the real backend is wired in.

4. **`fetchJobs` function** — `src/lib/api.ts` is the single bridge between the frontend and the backend. It builds the URL from the env var, checks `res.ok` explicitly, and throws on non-2xx responses so TanStack Query catches the error.

5. **`Providers` component** — `src/app/providers.tsx` creates a `QueryClient` via `useState` initialiser so each browser session gets its own cache. This avoids one user's cache leaking into another user's session. The `Providers` component is a Client Component because TanStack Query relies on React Context and browser state.

6. **Root layout wraps in Providers** — `src/app/layout.tsx` stays a Server Component but renders `<Providers>` around `children`. Only the providers subtree becomes client-rendered.

7. **`JobCardSkeleton` and `JobListSkeleton`** — `src/components/JobCardSkeleton.tsx` mirrors the real `JobCard` layout so when the skeleton is replaced by the real card, the layout does not jump.

8. **`useQuery` in `page.tsx`** — `src/app/page.tsx` replaced the hardcoded array with a `useQuery({ queryKey: ["jobs"], queryFn: fetchJobs })` call. The page now renders one of three branches:
   - `isPending` — show `JobListSkeleton`
   - `isError` — show a red error box with the error message
   - `data` (renamed to `jobs`) — render the real `JobList`

9. **Components untouched from 1.2** — `JobCard`, `JobList`, `JobStatusBadge`, `ThemeToggle`, `badge`, `utils`, and `globals.css` did not change in 1.3. Only the data source changed. This proves the components were built with a clean separation between data and presentation.

## How to run it

```bash
npm install
npm run dev
```

Open http://localhost:3000.

## How to verify Assignment 1.3 works

1. **Mock backend on its own** — Open http://localhost:3000/api/jobs. You should see raw JSON, an array of six jobs.

2. **Success branch** — Refresh http://localhost:3000. A skeleton flashes briefly, then real cards appear with coloured badges.

3. **Loading branch** — Open DevTools, Network tab, set throttling to Slow 3G, refresh. The skeleton stays visible for a few seconds.

4. **Error branch** — Temporarily change `fetchJobs` to call `/jobsXXX` instead of `/jobs`. Refresh. A red error box should appear. Change it back when done.

5. **TanStack Query DevTools** — Look for the small floating icon at the bottom-left of the page. Click it to see the `["jobs"]` query in the cache, its status, and the cached data.

6. **Selection persists (from 1.2)** — Click a card to select it, refresh, same card stays selected.

7. **Dark mode works (from 1.2)** — Click the moon icon, the whole page switches. Refresh, the theme persists.

## Project structure

```
careerhub__frontend/
├── src/
│   ├── app/
│   │   ├── api/jobs/route.ts    Mock backend — GET /api/jobs
│   │   ├── globals.css          Tailwind v4 + dark mode
│   │   ├── layout.tsx           Root layout, Server Component
│   │   ├── page.tsx             Main page with useQuery
│   │   └── providers.tsx        QueryClient setup, Client Component
│   ├── components/
│   │   ├── ui/badge.tsx         shadcn-style Badge with CareerHub variants
│   │   ├── JobCard.tsx
│   │   ├── JobCardSkeleton.tsx  Loading state (1.3)
│   │   ├── JobList.tsx
│   │   ├── JobStatusBadge.tsx
│   │   └── ThemeToggle.tsx
│   ├── lib/
│   │   ├── api.ts               fetchJobs (1.3)
│   │   └── utils.ts             cn helper
│   └── types/index.ts           JobListing, EmploymentType
├── .env.local                   NEXT_PUBLIC_API_URL
├── next.config.ts
├── package.json
├── postcss.config.mjs
└── tsconfig.json
```

## Environment

`.env.local` controls which backend the frontend talks to.

```
NEXT_PUBLIC_API_URL=http://localhost:3000/api
```

The default points at the built-in mock route handler at `/api/jobs`. When the real CareerHub .NET API is connected in a later assignment, change this URL to point at that instead — no other code changes are needed.

## Tech stack

- Next.js 15 (App Router)
- React 19
- TypeScript
- Tailwind CSS v4
- TanStack Query 5
- shadcn-style components with `class-variance-authority`
- Lucide icons
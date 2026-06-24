# Assignment 2.1 — CareerHub App Router

This project changes CareerHub from one page into a multi-page app using the App Router.
It now uses Server Components to fetch data on the server, has a dynamic route `/jobs/[id]`, a shared `/dashboard` layout, and a not-found page.

The `ApplicationForm` from Assignment 1.4 is reused without changes.

`JobCard`, `JobList`, `JobStatusBadge`, `ThemeToggle`, and `JobCardSkeleton` were not changed.

## Backend note

The app uses the real CareerHub API:

* Job list → `GET /api/v1/Jobs`
* Job details → `GET /api/v1/Jobs/{id}`

If a job is missing, the API returns **404**.
If the request method is not GET, it returns **405**.

There is also a mock route `/api/jobs/[id]` to show how 200 / 404 / 405 works, but the real app uses the real backend.

---

## Part 1 — Written Decisions

### 1. `cache: "no-store"` vs the default

`cache: "no-store"` means **do not cache data on the server**.
Every time the page loads, it fetches fresh data.

Important:

* This cache is on the **server**, not the browser.
* The browser does NOT see these requests.

Use default caching when:

* Data does not change often (like country lists or products)

Use `no-store` when:

* Data changes often (like job listings)

Difference from TanStack Query:

* TanStack Query cache → in the **browser (per user)**
* Next.js cache → on the **server (shared by all users)**

---

### 2. The `"use client"` boundary and what crosses it

`"use client"` makes the whole file run in the browser.

For `/jobs/some-id`:

* **Server Component (`page.tsx`)**

  * Runs on the server
  * Fetches job data
  * Sends HTML to the browser

* **Client Component (`ApplicationForm`)**

  * Runs in the browser
  * Adds interactivity (form, validation, events)

So:

* Job details → come as HTML (show immediately)
* Form behaviour → comes from JavaScript (after loading)

---

### 3. Why `params.id` is always a string

A URL is always text.

Example:
`/jobs/42` → "42" is a string

Next.js cannot know the type, so it always gives a **string**.

In this project:

* The API expects a **string GUID**
* So no conversion is needed

---

### 4. What "layout persists" actually means

When moving between pages with the same layout:

* The layout does NOT reload
* It does NOT lose state
* Only the inner content changes

Example:
Sidebar stays the same while page content changes.

To show dynamic data (like job count):

* Fetch inside the layout (server-side), OR
* Use a small Client Component inside it for live updates

---

## README Updates

### 1. The composition pattern in `/jobs/[id]`

Steps:

1. Server Component runs first
2. Fetches job data
3. Sends HTML (job details + form structure)
4. Browser shows content immediately
5. JavaScript loads and activates the form

If JavaScript is OFF:

* User still sees job details and form
* But form will NOT work (no validation or submit)

---

### 2. Why `JobLinkCard` has no `"use client"`

`JobLinkCard`:

* Uses `<Link>`
* Has NO state or event handlers

Even though `<Link>` uses client features:

* It handles that itself
* Parent does NOT need `"use client"`

But `JobCard`:

* Has `onClick`
* Needs browser interaction
* So it MUST be a Client Component

---

### 3. `loading.tsx` vs a manual loading state

With `useQuery`:

* Component loads first
* Shows loading state
* Then updates with data

With `loading.tsx`:

* Uses Suspense
* Shows loading screen BEFORE page renders
* Real content replaces it after

Key difference:

* `useQuery` → loading inside component
* `loading.tsx` → loading before component appears

---

### 4. Gate — build output

The project builds successfully with:

* No TypeScript errors
* No ESLint errors

Summary:

* `/` → Static page, very small
* `/jobs` → Server-rendered, little JavaScript
* `/dashboard/listings` → Server-rendered
* `/jobs/[id]` → Has form, more JavaScript (34 kB)

Only `/jobs/[id]` needs more JavaScript because it includes the interactive form.

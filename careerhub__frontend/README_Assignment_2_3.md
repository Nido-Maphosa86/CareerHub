# Assignment 2.3 — CareerHub Authentication & Smart State

Auth.js v5 with a credentials provider, role-based route protection, URL filter state with
nuqs, and a session-level Zustand store.

> **Architecture note.** This app already authenticates against the real CareerHub.Api backend
> for the apply flow and the close action (a backend-issued JWT held client-side). Assignment 2.3
> adds a *separate* Auth.js identity layer for page-level identity, role, and route protection.
> The two answer different questions — Auth.js proves "who is using the Next.js app and what may
> they see", the backend JWT proves "who is calling the API" — and they coexist without conflict.

---

## Part 1 — Written Decisions

### 1. Mapping CareerHub roles to route protection rules

| Route | Who can access | If wrong role / no session | Enforced in |
|---|---|---|---|
| `/jobs` | Everyone (public) | n/a — never blocked | neither |
| `/jobs/[id]` | Everyone (public) | n/a — page is viewable by all; only the apply *form* is gated | page component |
| `/dashboard` and below | Employers only | candidate → `/jobs`; signed out → `/login` | middleware |
| `/login` | Signed-out users | already signed in → employer to `/dashboard/listings`, candidate to `/jobs` | middleware |

`/jobs/[id]` is handled in the **page**, not middleware, because the route itself is public — an
employer is allowed to read a job's details. What differs by role is a *part of the page* (the
application form), not access to the page. Middleware decides whole-route access; it cannot
cleanly express "show this route but hide one section of it", and it would have to re-fetch or
re-derive the job to do so. The page already has the session (via `auth()`) and the job, so the
form-level decision belongs there.

**Why the two redirects are different problems.** Redirecting an *unauthenticated employer to
`/login`* is an **authentication** problem: there is no identity at all, so the answer is "go
prove who you are". Redirecting an *authenticated candidate away from `/dashboard`* is an
**authorisation** problem: the identity is known and valid, but it lacks the required role, so the
answer is "you are signed in, but this is not for you — go to your home surface (`/jobs`)".
Sending the candidate to `/login` would be wrong (they are already logged in and would just bounce
back); sending the unauthenticated user to `/jobs` would be wrong (they would silently lose the
thing they were trying to reach). Different causes, different destinations.

### 2. The session object design

**What goes on the session:** the user's `id`, `name`, and `role`. That is everything the UI
needs to greet the user, show a role badge, pick nav links, and gate the apply form.

**What is deliberately left off:** the password (never leaves `authorize`), and there is **no
`backendToken`** — the assignment warns against adding fields you do not use, and the app's API
calls use the separate backend JWT, not this session.

**Cost of putting too much on the session:** the session is encoded into the JWT stored in a
cookie that is sent on every request. Bloating it makes every request heavier, risks exceeding
cookie size limits, and bakes data into a token that only refreshes on re-login — so anything
that can change (e.g. a profile field) would go stale until the user signs out and back in.

**What breaks if you set role on the JWT but forget the session callback:** `token.role` would be
correct, but `session.user.role` would be `undefined`. Every component calling `auth()` reads the
*session*, not the token, so the nav, the apply gate, and middleware's role check would all see no
role — the candidate/employer distinction would silently collapse.

**The exact three-step relay:**
1. **authorize** returns `{ id, name, role }` after matching the mock user.
2. **jwt callback** receives that object as `user` on first sign-in and copies `token.role = user.role` so the role is persisted in the signed cookie.
3. **session callback** copies `session.user.role = token.role` so any `auth()` caller can read it.

### 3. Choosing the state tool for job filters

| Filter | Tool | Why |
|---|---|---|
| Keyword (`q`) | **nuqs (URL state)** | Shareable and bookmarkable; survives refresh; back/forward works |
| Location | **nuqs (URL state)** | Same reasons — a filtered list is a "view" worth linking to |
| Status (Open/All) | **nuqs (URL state)** | Belongs in the same shareable view as the others |

**On refresh:** because all three live in the URL, refreshing re-reads them from the query string
and the filtered view is reproduced exactly. With `useState` the values would reset to defaults on
every refresh.

**On sharing the URL:** a recipient who opens `/jobs?q=engineer&status=open` sees the identical
filtered result. `useState` holds the value only in the sender's browser memory, so a shared link
would show the unfiltered page.

**Does the employer dashboard need these filters?** No. They are candidate-side concerns on
`/jobs`. The dashboard has its own, unrelated view preferences (table/grid, show-closed), which is
why those use Zustand, not the URL — they are private UI state, not a shareable view.

**What nuqs buys over useState:** `useState` keeps a value in component memory only — it vanishes
on refresh, cannot be shared, and is invisible to the URL/back button. nuqs makes the URL the
single source of truth, so the filtered view becomes a first-class, linkable, refresh-proof,
history-aware piece of application state — exactly what a "filtered search" should be.

### 4. What the nav bar knows

**Why `await auth()` in `layout.tsx` is not a performance problem:** with the JWT strategy,
`auth()` verifies the signed session cookie *in memory* — there is no database query and no
network round-trip. It is essentially a signature check and a decode, so calling it on every page
render is cheap.

**If the session were needed in a deeply nested Client Component:** I would not prop-drill it
through every layer. I would either read it with the `useSession()` hook (available because
`SessionProvider` wraps the app) or pass just the needed field down from the nearest Server
Component that already has `auth()`.

**Why `useSession()` exists alongside `auth()`:** `auth()` is for the *server* (Server Components,
route handlers, middleware, Server Actions) — it reads the session where there are no hooks.
`useSession()` is for *Client* Components, where you cannot call `auth()` but you can use a hook,
and where you may want the session to react to client-side sign-in/out without a full reload. Reach
for `auth()` on the server; reach for `useSession()` in interactive client code.

---

## README Updates

### The role redirect decision

The post-login destination is role-based because the two roles have different home surfaces: an
employer manages listings (`/dashboard/listings`), a candidate browses jobs (`/jobs`). Sending
everyone to one page would dump employers on a candidate page or vice-versa.

The problem: `signIn()` performs its redirect *before* the session cookie is written, so at the
moment we must choose `redirectTo`, there is no session to read the role from. I solved it by
determining the destination from the **same source `authorize()` trusts** — the username. In the
login Server Action I look the submitted username up in a small role map (`roleForUsername`) and
compute `redirectTo` *before* calling `signIn`. `signIn` then validates the password and only
actually performs the redirect if the credentials are valid; a wrong password throws, which I
catch and turn into `/login?error=CredentialsSignin`. So the role-to-destination decision is made
from the credentials, not from a session that does not exist yet.

### Middleware vs page-level guards

**In middleware:** the `/dashboard` employers-only rule. This is whole-route, role-based access —
the cleanest place to stop a candidate or an anonymous user before the route renders at all.

**In the page:** the `/jobs/[id]` apply-form gate. The route is public (employers may view it), so
middleware must let everyone through; only a *section* of the page differs by role, and the page
already holds both the session and the job needed to decide.

**The general principle:** if the decision is "may this identity reach this route at all", it
belongs in middleware. If the decision is "this route is allowed, but what it renders depends on
identity", it belongs in the page. Whole-route access → middleware; within-page variation → page.

### Why URL state for job filters

nuqs over `useState` or Zustand because a filtered job search is a *shareable view*, not private
component state. **Sharing:** `/jobs?q=react&status=open` reproduces the exact result for anyone.
**Back/forward:** each filter change is reflected in the URL, so the browser's history buttons move
between filter states naturally. **Bookmarking:** a bookmarked filtered search reopens to the same
results later. `useState` gives none of these — it is memory-only and resets on refresh. Zustand
would share the value across the app but still would not put it in the URL, so it too would fail
sharing and bookmarking.

### Why Zustand without persist for the dashboard view

The table/grid choice and the show-closed toggle are *session-level UI preferences*: they should
survive navigating around the app (Zustand keeps them in memory across route changes) but it is
fine — even expected — for them to reset to defaults on a hard refresh, because they are not user
data. No `persist` middleware is therefore used.

If it *did* need to persist, the right mechanism for a pure UI preference would be the `persist`
middleware writing to `localStorage` under a key like `careerhub-dashboard-prefs` with a
`{ view, showClosedJobs }` shape. The tradeoff: `localStorage` is instant, free, and per-device,
but it does not follow the user to another browser or machine and is not known to the server. A
user-preferences API endpoint would make the choice durable and cross-device and let the server
render the right view immediately, at the cost of a network round-trip, backend storage, and
auth-scoped reads/writes. For a transient layout toggle, that is far too much machinery —
session-level memory is the right fit.

### The async Server Component / store boundary

`ListingsTable` is an async Server Component, so it runs on the server during streaming, where
React hooks do not exist — and `useStore`/`useDashboardStore` are hooks that read a browser-side
store. A Server Component therefore cannot call `useStore`.

The bridge: the store's values are passed into `ListingsTable` as **props** (`view`,
`showClosedJobs`). Since the page is also a Server Component and cannot read the store either, a
thin **Client** wrapper (`DashboardView`) reads the store with selectors and chooses which output
to show. Concretely, the page pre-renders the four `ListingsTable` variants (table/grid ×
show/hide closed) on the server and hands them to `DashboardView` as props; the client wrapper
subscribes to the store and renders the matching variant. The identical fetches inside those four
instances are de-duplicated by Next.js within a single render, so there is no four-times network
cost. This is the standard "pass Server Components into a Client Component as slots" pattern: the
client decides *which* server-rendered output is visible without ever needing to fetch or to read
the store on the server.

### Dashboard close button trust (Part 5)

`CloseJobButton` is rendered on the dashboard with no extra role check, and that is correct:
middleware guarantees only employers ever reach `/dashboard`, so by the time the table renders, the
viewer is already known to be an employer. Re-checking the role here would be redundant. (The
Stretch A note describes adding a defence-in-depth `auth()` check inside `ListingsTable` for the
hypothetical case where it is ever rendered on a non-protected route.)

---

## Gate

`npm run build` must complete with zero TypeScript and zero ESLint errors. Because every route now
calls `auth()`, all routes are server-rendered on demand, so the build does not require the backend
to be reachable. Verified: `npx tsc --noEmit` → 0 errors; `next build` → compiled successfully,
lint passed, all 8 routes built. Paste your own build output below:

```
PASTE THE OUTPUT OF: npm run build
```

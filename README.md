# Assignment 3.2 — CareerHub Testing

This project tests how the application works from the user’s point of view. It tests the wizard steps, login checks, review page, submit process, and closing a job. MSW is used to handle fake API requests, and GitHub Actions runs the tests automatically.

> **One change from 3.1.**
> The wizard now gets the user status using `useSession()` inside the component instead of receiving it as a prop. This allows testing login behaviour using a fake session.

---

## Part 1 — Written Decisions

### Question 1 — What is worth testing?

**Category A — important behaviours.**

1. **Next button blocked when required fields are empty.**
   If this breaks, users can skip required fields and submit incomplete applications.

2. **Login check stops signed-out users after step 1.**
   If this breaks, users can complete everything but fail only at the end, losing their work.

3. **Back button keeps previous answers.**
   Users should not lose their data when moving between steps.

4. **Review page shows all values, including "Not provided".**
   Users must see everything before submitting.

5. **Submit behaviour (success vs error).**

* On success → form resets
* On error → values stay
  Both are important and easy to break.

---

**Category B — not worth testing.**

1. **Exact styles (like colours or Tailwind classes).**
   Users do not depend on colours. Testing this makes tests fragile.

2. **Number of HTML elements (like divs).**
   Users do not see this. It only tests code structure, not behaviour.

---

**Category C — real vs mocked localStorage.**
I use the real jsdom `localStorage`.

This allows tests to:

* Save a draft
* Reload and check if it is restored
* Submit or discard and check if it is removed

This proves the feature actually works.

A mock (`vi.spyOn`) would only check if a function was called, not if the data really saves and loads.

---

### Question 2 — Mocking the session

**Approach 1 — mock the hook (`vi.mock`).**
Replace `useSession()` with a fake version that returns what the test needs.

**Approach 2 — real provider with fake session.**
Use the real `SessionProvider` but give it fake data.

**Chosen approach:**
Approach 1, because it is simpler and focuses only on what matters — the session value.

---

### Question 3 — MSW scope

Network requests used:

| When         | Method | URL                        | Response           |
| ------------ | ------ | -------------------------- | ------------------ |
| On load      | none   | —                          | No request happens |
| On submit    | POST   | `/applications/:listingId` | returns success    |
| After submit | GET    | `/Jobs`                    | returns empty list |

**What MSW cannot test:**

* Step navigation
* Form validation
* Login check
* Toast messages
* localStorage

These do not use HTTP, so MSW cannot handle them.

---

### Question 4 — Test naming

* **a)** "currentStep equals schedule" → **implementation**
  Better: "moves to schedule step after completing step 1"

* **b)** "shows Schedule heading" → **behaviour** (keep)

* **c)** "calls localStorage.setItem" → **implementation**
  Better: "keeps progress when user leaves and returns"

* **d)** "draft is available when user returns" → **behaviour** (keep)

* **e)** "renders 3 divs" → **implementation**
  Better: "shows loading placeholders while loading"

---

## README Updates

### 1. What makes a test important

Important tests are ones where a bug would:

* lose user data
* allow bad submissions

These include validation, login check, saving progress, review page, and submit behaviour.

I did not test styles or layout because they do not affect how the app works.

---

### 2. Session mocking approach

I used `vi.mock("next-auth/react")` to control what `useSession()` returns.

This allows testing:

* logged-in users
* logged-out users

It does not test real authentication (like login or cookies), only how the component reacts.

---

### 3. localStorage choice

I used real jsdom `localStorage`.

This proves:

* drafts save correctly
* drafts restore correctly
* drafts are removed when needed

It does not test real browser limits (like storage size or private mode).

---

### 4. One surprising test

The review test failed because "LinkedIn" appeared twice:

* once as a label
* once as a value

This was not a bug. The test was wrong.

The fix was to check that at least one match exists instead of expecting only one.

This helped me understand the UI better.

---

## Running the tests

```bash
npm test         # run in watch mode
npm run test:run # run once (used in CI)
```

## Gate

* `npx tsc --noEmit` → 0 errors
* `npm run test:run` → all tests passed

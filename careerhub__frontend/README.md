# CareerHub Frontend

A Next.js 15 frontend for the CareerHub job board. TypeScript, Tailwind v4,
TanStack Query, React Hook Form, and Zod. Dark, lime-on-black identity with a
responsive card grid and an accessible job application form.

## Run it

```bash
npm install
npm run dev
```

Open http://localhost:3000.

## Configuration

```
NEXT_PUBLIC_API_URL=http://localhost:3000
```

Assignment 1.4 runs self-contained: jobs come from the mock route at
`/api/jobs` and applications post to `/api/applications`, both Next.js route
handlers on this origin. To use the live .NET backend for jobs instead, set
`NEXT_PUBLIC_API_URL=http://localhost:5000/api/v1` and change the path in
`fetchJobs` (`src/lib/api.ts`) from `/api/jobs` to `/Jobs`.

---

## Assignment 1.4 — Applications & Mutations

### Part 1 — Written Decisions

#### 1. Why `@hookform/resolvers` is a separate package

React Hook Form deliberately knows nothing about any specific validation
library, and Zod knows nothing about forms. If RHF shipped Zod support
directly it would have to ship adapters for Yup, Joi, Valibot, Superstruct,
and every other validator too, and bump its own version every time any of
those released a breaking change. Pulling the adapters into a separate
`@hookform/resolvers` package lets each side evolve independently: a Zod major
release only requires a new resolvers release, not a new RHF release, and a
project that uses Yup never pulls in Zod code.

At runtime, `zodResolver(schema)` returns a **resolver function**. RHF calls
that function on submit with the signature `(values, context, options)`, where
`values` is the current form values object. The resolver calls
`schema.safeParseAsync(values)` on the Zod schema. It returns an object of
shape `{ values, errors }`: on success, `{ values: <parsed, coerced data>,
errors: {} }`; on failure, `{ values: {}, errors: <map keyed by field path,
each entry { type, message }> }`. RHF reads that `errors` map to populate
`formState.errors`, which is how Zod messages end up next to the right fields.

#### 2. The number input problem

`<input type="number" />` hands back a string. Two ways to get a number:

- **Solution A — `valueAsNumber: true`** converts at the **RHF layer**. RHF
  runs `Number(value)` in its `onChange` before the value is ever stored, so by
  the time the resolver runs, the field is already a number and `z.number()`
  passes.
- **Solution B — `z.coerce.number()`** converts at the **Zod layer**. The value
  stays a string inside RHF; when validation runs, Zod wraps it as
  `Number(value)` and validates the result.

Both produce an identical `z.infer<typeof schema>` because `z.infer` reflects
the schema's **output** type, and both schemas output `number`
(`z.number()` outputs number; `z.coerce.number()` also outputs number — the
coercion changes the input it accepts, not the type it emits). The layer where
the string becomes a number is irrelevant to the static output type.

**I use Solution B (`z.coerce.number()`).** The assignment requires
`{...register("fieldName")}` with no options, so all conversion and validation
must live in the schema. Solution B keeps the `register` calls option-free and
puts every rule in one place.

(Side note: because `z.coerce.number()`'s **input** type is `unknown` while its
**output** is `number`, the form uses RHF's three-generic form —
`useForm<z.input, unknown, z.output>` — so the fields type as input and
`handleSubmit` hands `onValid` the coerced output.)

#### 3. `mutate` vs `mutateAsync` — the isSubmitting timing bug

`handleSubmit(onValid)` **awaits** whatever `onValid` returns, and keeps
`isSubmitting` true until that awaited value settles. `mutation.mutate(data)`
returns **`void`** — it is fire-and-forget. So if `onValid` calls `mutate` and
returns, there is nothing to await; `handleSubmit`'s await resolves
immediately, and `isSubmitting` flips back to `false` while the 800ms request
is still in flight. The button re-enables mid-request.

`mutation.mutateAsync(data)` returns a **Promise** that resolves or rejects
when the request actually completes. By writing `await
mutation.mutateAsync(payload)` inside `onValid`, `onValid` stays pending until
the request finishes, so `handleSubmit` keeps `isSubmitting` true for the whole
request. That is the fix.

#### 4. `onSuccess` placement

The two placements differ when **the component unmounts before the request
resolves**. `onSuccess` in the `useMutation` options (Option A) is tied to the
mutation observer the QueryClient owns, so it still runs even if the form
unmounted. `onSuccess` passed to `mutate(data, { onSuccess })` (Option B) is
tied to that specific call site, and React Query **does not** run it if the
component that called `mutate` has unmounted by the time the request resolves.

**I use Option A** to invalidate `["jobs"]` and `reset()`. Invalidating the
jobs cache should happen on every success regardless of whether the form is
still on screen — the applicant count must refresh even if the user navigated
away — and keeping the success behaviour in one place (the mutation
definition) means there is a single source of truth rather than logic
duplicated at each call site.

### README Updates

#### 1. Schema design decisions (phone / linkedInUrl)

`z.string().optional()` alone does not work because an HTML input that is left
blank does not submit `undefined` — it submits `""` (an empty string).
`optional()` only makes `undefined` an accepted value; the empty string still
flows into the `.regex(...)` / `.url()` check and **fails validation**, so a
blank optional field would show an error.

`.or(z.literal(""))` adds the empty string as an explicitly valid value, so a
blank field passes. Combined with `.optional()`, the final inferred type is
`string | undefined`. Before building the API payload I convert `""` to
`undefined` (`data.phone ? data.phone : undefined`) so the server receives an
absent field rather than an empty string, matching the `phone?: string` shape.

#### 2. The cross-field refine

`.refine(predicate, options)` receives the **entire parsed object** as the
first argument to its predicate, and the predicate returns a boolean (`true` =
valid). That object-level access is exactly why it can compare two fields:
`(data) => data.availableImmediately || data.noticePeriodWeeks > 0`.

The `path: ["noticePeriodWeeks"]` option attaches the resulting error to that
field, so `errors.noticePeriodWeeks.message` renders beside the notice-period
input. **Omit `path`** and Zod attaches the error to the form root rather than
any field, so the field-level UI never shows it and the user sees nothing.

A field-level `.min(1)` on `noticePeriodWeeks` cannot express this because the
requirement is **conditional on another field**. A single-field check has no
access to `availableImmediately`; it would force a notice period even for
candidates who are available immediately. Only an object-level `.refine` can
read both fields at once.

#### 3. The two loading flags

Timeline from click to response:

1. Click → RHF sets `isSubmitting = true`, runs Zod validation.
2. Validation passes → `onValid` runs → `mutateAsync` fires →
   `mutation.isPending = true`.
3. ~800ms pass. **Both flags are true** for this whole window — this is what
   `isBusy = isSubmitting || mutation.isPending` covers.
4. Response arrives → `mutation.isPending = false`, `onSuccess` runs.
5. The promise from `mutateAsync` resolves → `onValid`'s `await` completes →
   `isSubmitting = false`.

So there is a brief tail (between steps 4 and 5) where `mutation.isPending` is
already `false` while `isSubmitting` is still `true`. The OR keeps the button
disabled across it.

**Can `mutation.isPending` outlast `isSubmitting`?** No — not when `mutateAsync`
is awaited. `isPending` goes false the instant the request settles, and
`isSubmitting` only goes false *after* the awaited `mutateAsync` promise
resolves, which is later. So `isSubmitting` outlasts `isPending`, never the
other way around. (With `mutate` instead of `mutateAsync`, `isSubmitting` would
end *first* — the timing bug from Q3.)

#### 4. Gate — build output

`npm run build` completes with zero TypeScript errors and zero ESLint errors:

```
> careerhub-frontend@0.1.0 build
> next build
   ▲ Next.js 15.1.0
   - Environments: .env.local
   Creating an optimized production build ...
 ✓ Compiled successfully
   Linting and checking validity of types ...
   Collecting page data ...
   Generating static pages (6/6)
   Finalizing page optimization ...

Route (app)                              Size     First Load JS
┌ ○ /                                    45.7 kB         157 kB
├ ○ /_not-found                          979 B           106 kB
├ ƒ /api/applications                    140 B           105 kB
└ ƒ /api/jobs                            140 B           105 kB
+ First Load JS shared by all            105 kB

○  (Static)   prerendered as static content
ƒ  (Dynamic)  server-rendered on demand
```

---

## Earlier assignments (summary)

- **1.1** — Next.js 15 setup, `JobListing` / `EmploymentType` types.
- **1.2** — Components, badges, theme toggle, sessionStorage selection.
- **1.3** — TanStack Query data layer: `fetchJobs`, `Providers` with a
  `QueryClient` via `useState`, skeletons, `useQuery` with three render
  branches.
- **Live API** — connected to the real .NET backend; `fetchJobs` unwraps the
  paginated response. (Toggled to mock for 1.4 — see Configuration.)
- **Redesign** — responsive card grid, lime-on-black palette.

## Structure

```
src/
├── app/
│   ├── api/applications/route.ts   POST submit, 405 on GET, 400, 800ms, 201
│   ├── api/jobs/route.ts           Mock jobs (paginated shape)
│   ├── globals.css                 Tailwind v4, lime/black tokens
│   ├── layout.tsx                  Root layout, dark by default
│   ├── page.tsx                    Grid + selection panel + ApplicationForm
│   └── providers.tsx               QueryClient via useState
├── components/
│   ├── ui/badge.tsx
│   ├── ApplicationForm.tsx         Zod schema, RHF, useMutation, all states
│   ├── JobCard.tsx                 (unchanged in 1.4)
│   ├── JobCardSkeleton.tsx         (unchanged in 1.4)
│   ├── JobList.tsx                 (unchanged in 1.4)
│   ├── JobStatusBadge.tsx          (unchanged in 1.4)
│   └── ThemeToggle.tsx             (unchanged in 1.4)
├── lib/
│   ├── api.ts                      fetchJobs + submitApplication
│   └── utils.ts                    cn helper
└── types/index.ts                  JobListing, EmploymentType,
                                    ApplicationRequest, ApplicationResponse
```

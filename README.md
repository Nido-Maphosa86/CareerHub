# Assignment 3.2 — Testing & CI/CD Pipelines

This section documents the test suite added in Assignment 3.2 and the
written decisions required by the spec.

---

## Part 1 — Written Decisions

### 1. Unit test vs Integration test

**Salary range validation in JobListingService.CreateAsync** — Unit test.
This is pure business logic that lives in one method. A unit test with
NSubstitute can verify it in under 10ms. An integration test cannot
catch what the unit test misses here — it would also test database
behaviour, controller routing, and serialization, none of which matter
for "does the service reject SalaryMax < SalaryMin." A unit test cannot
verify the rule still fires when called through the real HTTP pipeline,
but for a guard clause that lives entirely inside one method, that
extra confidence is not worth the slowdown.

**[Authorize] attribute on POST /api/v1/jobs** — Integration test.
The attribute is metadata that does nothing on its own. It only fires
when ASP.NET Core's authentication middleware is wired into the
pipeline, the JWT scheme is registered, and the controller is actually
routed. A unit test on the controller method cannot verify any of this
because it does not run the middleware. WebApplicationFactory starts
the real pipeline and catches misconfiguration like a missing
UseAuthentication() call.

**SalaryMax > SalaryMin check constraint in the database** —
TestContainers integration test. This constraint is enforced by
PostgreSQL itself, not by C#. A unit test cannot verify it because
mocks do not execute SQL. The EF Core in-memory provider cannot verify
it because it does not enforce check constraints. Only a real
PostgreSQL connection running real migrations proves the constraint
exists and fires. The constraint matters because raw SQL inserts,
database admin operations, and bypassed services all reach the
database directly — the constraint is the last line of defence.

**api-supported-versions: 1.0 header on every response** — Integration
test. The header is added by the Asp.Versioning middleware after the
response is built. A unit test on a controller cannot see middleware
output because the middleware does not run. WebApplicationFactory runs
the full pipeline and lets us inspect real response headers.

**HasAppliedAsync compiled query returning the correct boolean** —
TestContainers repository test. Compiled queries translate the
expression tree to SQL once and reuse it. A bug in the translation
would produce wrong SQL but no exception. Only running it against
real PostgreSQL with real data verifies the SQL behaves correctly.
The in-memory provider runs the LINQ in C# without translating to
SQL at all, so a translation bug would be invisible.

### 2. Why the in-memory EF Core provider is insufficient

**Check constraints (Assignment 2.4)** — The CareerHub database has
constraints like `ck_job_listings_salarymax_gt_min`,
`ck_job_listings_closingdate_after_postedat`, and
`ck_applications_submittedat_not_future`. The EF Core in-memory
provider does not implement constraint enforcement at all — every
SaveChangesAsync succeeds regardless of the data. A test suite using
the in-memory provider would pass even if every constraint was
removed from the database.

**Full-text search with tsvector (Assignment 2.4)** — The
JobListings table has a `SearchVector` computed column of type
tsvector, with a GIN index, queried using `EF.Functions.ToTsQuery`.
The in-memory provider has no concept of tsvector or any PostgreSQL-
specific type. Calls that compile against the in-memory provider
throw at runtime, and tests using the provider would never exercise
the actual stemming and ranking behaviour.

**Compiled queries (Assignment 2.4)** — `HasAppliedAsync` is a
compiled query that produces a specific SQL plan. The in-memory
provider does not translate LINQ to SQL — it runs the expression
tree directly against in-memory collections. A compiled query that
generates broken SQL would still pass an in-memory test because
the SQL never runs.

### 3. Test isolation

A test is isolated when its result depends only on its own arrange
step — not on what previous tests did. Isolation matters because
tests run in parallel and in unspecified order. If Test A inserts
a listing and Test B counts listings, then running B alone gives
one answer and running A then B gives a different one. The test
that should pass deterministically becomes flaky.

The specific problem with shared rows: imagine Test A inserts a
listing then deletes it, leaving the database empty. Test B asserts
the listing count is exactly 1. If A runs before B, A's delete
clears the row and B fails. If B runs before A, both pass. The
outcome depends on order — that is not a test, it is a coin flip.

TestContainers solves this by giving each test class its own
PostgreSQL container that starts empty. Per-test data seeding
solves it within a class — each test creates the exact data it
needs and never relies on what was there before.

### 4. The purpose of a CI pipeline

Running tests locally proves the code works ON YOUR MACHINE with
YOUR uncommitted changes. A CI pipeline proves the code works on
a clean machine with EVERYTHING that has been merged so far,
including changes made by other people.

The two-developer merge scenario: Developer A adds a method
`GetListingsBySalaryRange` and tests it. All local tests pass.
Developer B independently renames `SalaryMin` to `MinSalary` and
updates every usage they know about. All B's local tests pass.
A and B push to feature branches. CI passes on each branch
because each contains only its own changes.

When both branches merge to main, A's new method references
`SalaryMin` — the column B renamed. Main breaks. Neither A nor B
ran the combined code locally because neither knew about the
other's change.

A CI pipeline with "Require branches to be up to date before
merging" catches this. The setting forces each PR to be rebased
on the latest main before it can be merged. After A merges first,
B's branch is no longer up to date — B must rebase, run CI again
on the combined code, and only then can merge.

---

## Part 6 — Branch Protection Setup

To configure branch protection on the main branch:

1. Go to your GitHub repository
2. Click **Settings**
3. Click **Branches** in the left sidebar
4. Click **Add branch protection rule** (or **Add rule**)
5. In **Branch name pattern** type: `main`
6. Check **Require a pull request before merging**
7. Check **Require status checks to pass before merging**
8. In the search box that appears, type and select: `Build and Test`
   (this is the job name from `.github/workflows/ci.yml`)
9. Check **Require branches to be up to date before merging**
10. Check **Do not allow bypassing the above settings**
11. Click **Create** or **Save changes**

**Why "Require branches to be up to date before merging" matters:**
A status check that passed yesterday does not prove the code still
works today. If main has changed since the PR last ran CI, the PR
might break main after merging — even though its own CI was green.
This setting forces every PR to be rebased on the current main and
re-tested before merging, catching the silent merge conflicts that
two passing branches can create together.

**Why "Do not allow bypassing the above settings" matters:**
By default, repository administrators can override branch protection
and push directly to main. This setting removes that exception —
even admins must open a PR and pass CI. Without it, the rules apply
to everyone except the people most likely to be debugging at 2am
and tempted to push a "quick fix" that skips tests. Real protection
covers everyone.

---

## Part 7 — Test Coverage Analysis

### What the unit tests do not cover

**Database transaction rollback when SaveChangesAsync throws** — the
unit tests use NSubstitute fakes that always succeed. A real database
transaction rolling back after a constraint violation requires the
actual DbContext and EF Core's change tracker. This needs a
TestContainers repository test.

**Response headers added by middleware** — headers like
`api-supported-versions` and `X-Total-Count` are written by
middleware and controller code together as the response leaves the
pipeline. The unit test sees a `JobResponse` object, not an HTTP
response. WebApplicationFactory integration tests verify these.

### What the integration tests do not cover

**Behaviour under sustained load and concurrent requests** —
WebApplicationFactory runs the full pipeline but handles one
request at a time in a test thread. Race conditions in PATCH
under concurrent updates from two users will not surface here.
Load testing tools like k6 or JMeter catch these.

### What TestContainers tests do not cover

**Browser-side behaviour like CORS preflight rejection** — the
TestContainers tests verify SQL, constraints, and EF Core
translation. A browser refusing to send a request because the
CORS configuration is wrong is a behaviour of the BROWSER, not
of PostgreSQL. End-to-end tests using Playwright or Cypress
running against a real browser catch this.

---

## Test Pyramid for CareerHub

```
              ╱╲
             ╱  ╲       Repository (TestContainers)
            ╱ 10 ╲          10 tests
           ╱──────╲
          ╱        ╲     Integration (WebApplicationFactory)
         ╱   10     ╲         10 tests
        ╱────────────╲
       ╱              ╲   Unit (NSubstitute)
      ╱      16        ╲       16 tests
     ╱──────────────────╲
```

The unit tests outnumber the others because they are the cheapest
to write, the fastest to run, and the most precise about what
broke when they fail. Every guard clause, every conditional, every
business rule gets a unit test. The repository and integration
layers test fewer, broader behaviours — they catch the things unit
tests cannot, but they are slower and more fragile.

This shape matches the classic test pyramid: many small fast tests
at the base, fewer slow expensive tests at the top. The opposite
shape (mostly integration tests) is called an "ice cream cone" and
is a known anti-pattern — slow feedback, flaky failures, and
expensive to maintain.

---

## What each test layer would catch

**Unit tests caught**: the PatchAsync conditional guard. Writing
the test `PatchAsync_WhenOnlyTitleChanged_DoesNotThrowSalaryException`
forced the implementation to actually be conditional. Without the
test, a refactor that moved salary validation to always run would
break only when a real user patched a title and got a confusing
"salary range invalid" error in production.

**Integration tests caught**: the [Authorize] attribute on POST
endpoints. Writing `PostJob_WithoutToken_Returns401` proved the
authentication middleware was wired into the pipeline. Without
this test, removing `app.UseAuthentication()` from Program.cs
would silently make every endpoint public — the unit tests would
all still pass.

**Repository tests caught**: the check constraints. Writing
`CheckConstraint_RejectsSalaryMaxLessThanSalaryMin` proved the
constraint exists in the actual migration applied to the
production database. Without this test, dropping the constraint
in a migration would not be detected until corrupt data appeared.

---

## The `public partial class Program {}` change

`public partial class Program { }` at the bottom of Program.cs is
required for `WebApplicationFactory<Program>` to work.

When you use top-level statements (the modern Program.cs with no
class and no Main method), the C# compiler generates a hidden
`Program` class behind the scenes. That generated class is internal
to the assembly that contains it. The test project lives in a
different assembly and cannot see internal types.

Adding `public partial class Program { }` merges with the
compiler-generated class via the `partial` keyword and changes its
accessibility to `public`. The test assembly can now reference it
by name.

This line adds no methods, no fields, no behaviour. The compiled
output is identical except for the access modifier on the Program
type. The production runtime is unaffected.

---

## CI and the merge queue problem

Imagine four developers — A, B, C, D — each working on a feature
branch off main. All four push branches with passing CI. The CI
that ran on each branch tested THAT branch against the version of
main at the time of the last rebase.

Developer A merges first. Main now contains A's changes. B, C,
and D's branches are now stale — their CI runs were against the
old main. Their code might break when combined with A's changes,
but the green checkmarks on their branches still say "passing."

Without "Require branches to be up to date before merging," B can
merge their stale branch and break main. Then C merges and the
broken state compounds.

With "Require branches to be up to date before merging" enabled,
B's merge button stays disabled until B rebases on the new main.
The rebase triggers a fresh CI run that tests B's changes against
A's changes combined. Only after that combined CI passes can B
merge. Same for C and D.

The cost is that developers must rebase more often. The benefit is
that main is never broken by a merge.

---

## Test Naming Convention

The convention is `MethodName_Scenario_ExpectedResult`:

`CreateAsync_WhenSalaryMaxLessThanSalaryMin_ThrowsInvalidListingException`
tells you the method tested, the input scenario, and what should
happen. If this test fails by itself with no further context, you
already know exactly what behaviour is broken.

`PatchAsync_WhenOnlyTitleChanged_DoesNotThrowSalaryException`
documents the conditional guard. The name itself is the
specification: "patching only the title must not run salary
validation."

`HasAppliedAsync_WhenNoApplicationExists_ReturnsFalse` describes
the negative case for the compiled query. Without the scenario in
the name you would not know whether the test covered the true
branch or the false branch.

Named `Test1`, `Test2`, `Test3` instead, the CI failure log would
say only "Test2 failed" — you would have to open the source file
to find out what Test2 actually checks. Every minute of debugging
starts with that lookup. With descriptive names, the failure
message in the CI log IS the explanation.
..................
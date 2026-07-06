# Assignment 3.1 — CareerHub Rich UI & Form Patterns

This project includes toast messages, a multi-step application form with auto-save (draft), a confirmation dialog for risky actions, loading skeletons, and two different empty states.

---

## Part 1 — Written Decisions

### 1. Draft persistence strategy

**Storage key.**
The key used is `careerhub-application-${jobId}`. It is linked to a specific job because a draft belongs to one application only. This prevents drafts from mixing.

*Two jobs at once:*
If a user opens job A and job B, each job has its own draft (`careerhub-application-A` and `careerhub-application-B`). Typing in one will not affect the other. If there was only one key, one draft would overwrite the other.

*Different device:*
`localStorage` only works on one browser and device. A draft saved on a laptop will not appear on a phone. This is expected because draft saving is only for preventing data loss on the same device, not for syncing across devices.

**When the draft is cleared.**
The draft is deleted in these cases:

1. **After successful submission** — the draft becomes a real application, so keeping it could cause duplicate submissions.
2. **When the user clicks "Discard draft"** — the user chooses to delete it.
3. **Not cleared when leaving the page** — this is intentional so the draft is not lost.

**Which fields are stored.**
All fields are stored: name, email, phone, cover letter, LinkedIn URL, and referral source. These are safe because they are not sensitive. No passwords or tokens are saved.

---

### 2. The skeleton loader contract

**Matching size and layout.**
The skeleton must look like the real job card. It should have the same shape, spacing, and layout so that when the real data loads, nothing jumps or shifts.

**3 jobs but 6 skeletons.**
If you show 6 skeletons but only 3 real jobs load, the page shrinks, which can confuse users. However, the system does not know the number of jobs before loading, so a fixed number (6) is used as a design choice.

**Paired component pattern.**
A skeleton must match its real component exactly. For example, `JobCardSkeleton` must match `JobLinkCard`. If they do not match, the layout will shift when loading finishes, which looks bad.

---

### 3. AlertDialog vs other options

**Closing a job → AlertDialog.**
Closing a job is permanent. The confirmation dialog makes sure the user really wants to do it.

**Discarding a draft → AlertDialog.**
Deleting a draft cannot be undone, so confirmation is needed.

(A normal dialog is for simple actions. Inline confirmation is for small actions. These are not suitable for permanent actions.)

**Problem with Server Action.**
The dialog is rendered outside the form (in a portal). This means a submit button inside it does not work because it is not connected to the form.

**Solution: `useTransition`.**
Instead of using form submission, the action is called directly in JavaScript using `onClick`. The form data is created manually and passed to the server action. `useTransition` is used to handle loading state.

This works because it does not depend on the form structure.

---

### 4. Empty state types

**Why two types.**
There are two situations:

* No jobs exist at all
* Jobs exist, but filters removed them

These look the same (no results), but they mean different things.

**Where the check happens.**
The server checks if there are jobs before filtering. It sends:

* the filtered jobs
* a flag (`databaseEmpty`)

The UI then decides which message to show.

---

## README Updates

### Draft storage key decision

The key uses the job ID so each job has its own draft. This prevents overwriting when applying to multiple jobs.

If the job changes later, the draft is still valid because it only stores user answers, not job data. The user can review before submitting.

---

### Solving AlertDialog with a Server Action

The problem is that the dialog is outside the form, so submit does not work.

The solution is to call the server action directly using `onClick` and `useTransition`. This avoids relying on form submission.

---

### The Back button and validation

The Back button does not validate.

Back means the user wants to go back, not confirm data. If validation was required, the user could get stuck.

Only the Next button should validate.

---

### Skeleton count justification

Six skeletons are shown because:

* Too few looks empty
* Too many looks misleading

Six gives a good balance and fills the screen nicely.

---

### Empty state explanation

The server checks if there are jobs before filtering.

If no jobs exist → show "No jobs available"
If jobs exist but filters remove them → show "No jobs match your search"

This must be done on the server because only the server knows the full list.

---

## Gate

The project must build with no errors.

Commands used:

* `npx tsc --noEmit` → 0 errors
* `next build` → success

(Some warnings may appear, but they are not errors.)

```
PASTE THE OUTPUT OF: npm run build
```


//summarry

Toasts replace clunky inline banners — feedback appears in the corner and disappears on its own.
AlertDialog forces deliberate confirmation before permanent actions — the portal problem was solved with useTransition.
The wizard guides candidates step by step — auto-saves every keystroke, restores on refresh, validates only the current step, shows a full review before submitting.
The skeleton is paired with the real card — no layout shift when data arrives.
Two distinct empty states tell the user the right thing — one for a genuinely empty database, one for a search with no results.
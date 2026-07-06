# Assignment 3.3 — CareerHub Performance & SEO

This assignment improves speed and SEO. It adds metadata, Open Graph tags, image optimisation using `next/image`, and code splitting using dynamic import.

---

## Part 1 — Written Decisions

### Question 1 — Image audit

| Location             | Source                          | Above fold? | Use next/image?      |
| -------------------- | ------------------------------- | ----------- | -------------------- |
| Home page hero image | `/public/hero-illustration.svg` | Yes         | Yes — main LCP image |
| Job cards            | No image                        | n/a         | n/a                  |
| Job detail page      | No image                        | n/a         | n/a                  |
| Dashboard            | No image                        | n/a         | n/a                  |
| Navbar logo          | Inline SVG letter "C"           | Yes         | No — just an icon    |

Right now, there are no external images. The API does not return company logos — only text (`companyName`).

If logos are added later, they should be used in job cards and handled with `next/image` using remote settings.

**Most important image for `priority`:**
The hero image on the home page.

Reason:

* It is the biggest visible element when the page loads
* Without `priority`, it loads slowly (lazy loading)
* With `priority`, it loads earlier using preload
  → This improves LCP (Largest Contentful Paint)

---

### Question 2 — ApplicationWizard loading

**a. Should `ssr: false` be used?**
Yes.

The wizard uses:

* `useSession()` (client-only)
* `localStorage` (browser-only)
* React hooks like `useTransition`

These do not work on the server. Using SSR would cause errors.

---

**b. Does loading it early affect users who cannot apply?**
Yes.

Users like employers or logged-out users:

* only want to read job details
* do not need the form

If the wizard loads early:

* extra JavaScript is downloaded
* page becomes slower (TTI and TBT increase)

So, it should load only when needed.

---

**c. Why do tests still pass after dynamic import?**
Because tests import the component directly:

```ts
import { ApplicationWizard } from "@/components/ApplicationWizard";
```

This means:

* tests do not use `dynamic()`
* they load the component normally

So nothing changes for testing.

---

### Question 3 — Static vs dynamic metadata

| Page         | Type                         | Reason                       |
| ------------ | ---------------------------- | ---------------------------- |
| `/` (home)   | Static                       | Same for all users           |
| `/jobs`      | Static                       | Page meaning does not change |
| `/jobs/[id]` | Dynamic (`generateMetadata`) | Depends on job data          |

Job detail pages need dynamic metadata because:

* title
* description
* Open Graph data
  all depend on the job from the API

---

**Request deduplication.**
Both the page and `generateMetadata` call `getJob(id)`.

Next.js will:

* detect same request (same URL + options)
* send only one network request
* reuse the result

This only works if:

* both use the same function or same request setup

If they differ, two requests will be made.

---

### Question 4 — Lighthouse baseline


```
HOME PAGE
  Performance:  ___
  LCP:          ___ ms
  CLS:          ___
  INP:          ___
  SEO:          ___

JOB DETAIL PAGE (/jobs/[id])
  Performance:  ___
  LCP:          ___ ms
  CLS:          ___
  INP:          ___
  SEO:          ___
  SEO flags:    ___
```

---

## README Updates

### Before/after Lighthouse results



| Metric            | Before | After |
| ----------------- | ------ | ----- |
| Performance score | ___    | ___   |
| LCP               | ___    | ___   |
| CLS               | ___    | ___   |
| INP               | ___    | ___   |
| SEO score         | ___    | ___   |

---

**Most important improvement:**
The `priority` prop on the hero image.

* It improves LCP directly
* loads the image earlier

Dynamic import helps:

* reduce JavaScript load
* improve TTI and TBT

Metadata helps:

* improve SEO score

---

### Image audit summary

There is only one image:

* home page hero image

It uses `next/image` with `priority` because it is above the fold.

There are no:

* company logos
* profile images

If added later, they should use `next/image` with remote config.

Inline SVG icons were not changed because:

* they are not normal images
* `next/image` is not needed

---

### Deduplication explanation

When both metadata and page call `getJob(id)`:

* Next.js sees same request
* sends only one API call
* shares result

This works only if:

* URL is the same
* fetch options are the same

It breaks if:

* URL changes
* headers change
* cache options change

---

### One metric that did not change

CLS may not change in development mode.

Reason:

* dev mode does not behave like production

In production:

* layout shifts may happen if skeleton size is wrong

To fix:

* match skeleton size exactly
  or
* use `min-height`

A CDN could also help reduce loading time.

---

### Bundle analyzer

Run:

```bash id="0b0b9e"
npm run analyze
```

You should see:

* `ApplicationWizard` in a separate bundle

---

## Gate

* `npx tsc --noEmit` → 0 errors
* `npm run test:run` → all tests passed
* `npm run build` → successful build

Paste output:

```
PASTE: npm run build output
```


summary
Two goals: fast and findable.
Findable means search engines like Google can read each page properly — a real title, a real description, and Open Graph tags so the page looks good when shared on WhatsApp or LinkedIn.
Fast means real users get content on screen quicker — measured by three Core Web Vitals: LCP (how fast the biggest thing loads), CLS (whether the page jumps around while loading), and TTI (how quickly the page becomes interactive).
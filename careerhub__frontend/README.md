# CareerHub Frontend — Assignment 1.2

## What this assignment covers

This assignment adds four important features to improve the app:

* **shadcn/ui** → gives reusable and consistent UI components that you own
* **cn utility** → helps combine Tailwind classes correctly without conflicts
* **useEffect with sessionStorage** → keeps the selected job even after refreshing the page
* **Class-based dark mode** → lets the whole app switch between light and dark mode with one button

These changes make the app more stable, easier to manage, and better for users.

---

## File structure (simple explanation)

The project is organised into folders so each part has a clear role:

* **app/** → handles layout and main pages
* **components/** → reusable UI parts like JobCard and JobList
* **lib/** → helper functions like cn
* **types/** → TypeScript definitions

Each file has one clear responsibility, which makes the project easier to maintain.

---

## Part 1 — Written Decisions

### 1. The shadcn/ui ownership model

With libraries like **MUI**, components are stored in *node_modules*.
If the library updates and changes something (for example renaming a prop), your app can break automatically.

With **shadcn/ui**, it works differently:

* Components are copied into your project
* You fully **own the code**
* Updates only happen when **you choose to update**

This gives you full control and avoids unexpected errors.

---

### 2. Why cn exists

Using Tailwind classes with normal strings can cause problems.

Example:
"border-gray-200 border-blue-500"

Both classes control the same thing (border color).
The browser may choose the wrong one depending on CSS order.

**cn solves this problem:**

* **clsx** → handles conditions (like if something is selected)
* **tailwind-merge** → removes conflicting classes

So the final result is always correct and predictable.

---

### 3. Event handler vs useEffect for sessionStorage

An **event handler** only works when a user clicks.

Problem:

* When the page reloads, no click happens
* The saved job in sessionStorage is never used

**useEffect fixes this:**

* Runs automatically when the component loads
* Reads sessionStorage
* Restores the selected job

That’s why useEffect is necessary for this feature.

---

### 4. Source of truth for dark mode

Dark mode is controlled by this:

document.documentElement.classList

If the **"dark" class** exists → dark mode is active.

Important:

* React state (**isDark**) is only used for button text
* The actual styling depends on the **HTML class**, not React

Even if the component reloads, dark mode stays because the class is still there.

---

## Part 2 — shadcn/ui setup

After setting up shadcn/ui, you get:

1. **components.json** → configuration file
2. **utils.ts** → contains the cn function
3. **badge.tsx** → Badge component code

The Badge uses **cva (class-variance-authority)**:

* Maps different variants (like colors) to class names
* Makes styling reusable and structured

---

## Part 3 — JobStatusBadge

This component only handles:

* Job type (FullTime, PartTime, etc.)
* Status (Active or Closed)

Why separate it?

* Keeps code clean
* Makes updates easier

Example:
If you change the color for "Contract", you only update it in one place.

TypeScript ensures:

* Every job type must have a style
* If one is missing → build fails immediately

---

## Part 4 — Tailwind design improvements

Changes made:

* Replaced template strings with **cn** for safer styling
* Added dark mode styles to all colors

Examples:

* text-gray-900 → dark:text-gray-100
* bg-white → dark:bg-gray-800
* border-gray-200 → dark:border-gray-700

Closed jobs are easier to see:

* Lower opacity (faded look)
* Italic title

This improves user experience when scanning job listings.

---

## Part 5 — sessionStorage persistence

Two separate useEffect hooks are used:

### Effect 1 ([])

* Runs once when the page loads
* Reads sessionStorage
* Restores the selected job

### Effect 2 ([selectedId])

* Runs when the selected job changes
* Saves the job ID
* Removes it if nothing is selected

Why separate them?
If combined:

* The stored value would be deleted before being read
* The selection would never be restored

---

## Part 6 — Dark mode toggle

When the app loads, it checks:

1. **localStorage** → user’s saved preference
2. **OS settings** → system dark mode
3. Default → light mode

When toggling:

* Saves the choice in localStorage
* Adds/removes "dark" class

The button label shows the **action** (e.g. "Switch to dark mode"), not the current state.

---


## Component responsibility table

| Component | Owns state | Receives via props |
|---|---|---|
| Home | selectedId: string or null | nothing |
| JobList | nothing | jobs, selectedId, onSelect |
| JobCard | nothing | job, isSelected, onSelect |
| JobStatusBadge | nothing | employmentType, isActive |
| ThemeToggle | isDark: boolean (label only) | nothing |

---

## Effect table

| Effect | Dependency array | Runs when | Purpose |
|---|---|---|---|
| Restore | [] | Once on mount | Reads sessionStorage and restores selected job if ID is still valid |
| Persist | [selectedId] | Every selectedId change | Writes or removes the session key |
| Merged (wrong) | [selectedId] | Mount and every change | Immediately clears the key on mount before restore can read it |

---

## How to run

```bash
npm install
npm run dev
```

Open http://localhost:3000 in your browser.

---

## How to test

### Test 1 — shadcn/ui Badge is owned not installed

Open src/components/ui/badge.tsx. Confirm the file exists in your source code
and not in node_modules. Open node_modules and confirm there is no shadcn folder.
This proves shadcn/ui copies source rather than installing a package.

### Test 2 — Employment type badges render with distinct colours

Look at the job listing grid. Each card should show a coloured badge. Confirm
that FullTime shows blue, PartTime shows purple, Contract shows orange, and
Internship shows teal. The Vodacom listing is Closed — it should show both a
FullTime badge and a red Closed badge side by side.

### Test 3 — Active listings show no Closed badge

Inspect any active listing in the browser Elements panel. Confirm there is no
hidden or invisible element where the Closed badge would be. The element must
not exist in the DOM at all — not display: none, not empty, completely absent.

### Test 4 — applicantCount of 0 renders nothing

The FNB React Developer listing has applicantCount: 0. Open that card. Confirm
the number 0 does not appear anywhere in the card. Inspect the element — no
paragraph element for the applicant count should exist in the DOM.

### Test 5 — cn replaces all template literals

Open src/components/JobCard.tsx and src/components/JobList.tsx. Use Ctrl+F to
search for the backtick character. Zero results must appear in both files — no
template literals remain anywhere in either file.

### Test 6 — sessionStorage persistence

Click any job card. The summary panel appears at the top. Refresh the page with
F5. The same job should still be selected and the summary panel should reappear.
Open DevTools, go to Application, then Session Storage, then localhost:3000.
Confirm the careerhub-selected-job key is present with the correct ID. Click the
same card again to deselect. Refresh. No job is selected and no summary panel
appears. Check Session Storage again — the key must be gone.

### Test 7 — Dark mode toggle

Click the dark mode button in the header. Every surface should switch — header,
page background, cards, badges, summary panel. Refresh the page. Dark mode should
still be active. Open a new tab to http://localhost:3000 — dark mode is active.
Open DevTools, go to Application, then Local Storage. Confirm careerhub-theme
is set to "dark". Clear Local Storage, then refresh. The app should use your OS
dark mode preference on the next load.

### Test 8 — Selected card visual state in both modes

Select a job in light mode. The selected card should have a blue border and ring.
Toggle to dark mode. The selected card should still be visually distinct with an
adapted border and ring colour. The unselected cards should look different from
the selected one in both modes.

### Test 9 — Closed listing visual state

Find the Vodacom Backend Engineer listing — it has isActive: false. Confirm it
shows the Closed badge. Confirm the card appears visually different from active
listings — it should be slightly faded (opacity) with an italic title. Toggle dark
mode and confirm the closed state is still clearly visible in dark mode.

### Test 10 — Build passes with zero errors

```bash
npm run build
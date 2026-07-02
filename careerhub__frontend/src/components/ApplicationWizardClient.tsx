// src/components/ApplicationWizardClient.tsx
// Assignment 3.3 — Part 4: the dynamic import wrapper.
//
// next/dynamic with ssr: false cannot live in a Server Component. This thin
// Client Component exists solely to hold that dynamic import. The page (a Server
// Component) renders this wrapper, and this wrapper loads the real wizard
// dynamically in the browser. The wizard uses useSession(), localStorage, and
// other browser-only APIs — ssr: false prevents Next.js from trying to render
// it on the server, which would throw.

"use client";

import dynamic from "next/dynamic";

// Named export requires the .then(mod => ...) pattern because dynamic() expects
// a default export. We pull ApplicationWizard out of its named export here.
const ApplicationWizard = dynamic(
  () =>
    import("@/components/ApplicationWizard").then((mod) => ({
      default: mod.ApplicationWizard,
    })),
  {
    // ssr: false — wizard uses useSession(), localStorage, browser-only APIs.
    ssr: false,

    // Loading skeleton reserves the wizard's approximate height so the layout
    // does not jump when the bundle loads — this directly targets CLS.
    loading: () => (
      <div
        className="h-96 w-full animate-pulse rounded-xl border border-zinc-200 bg-zinc-100 dark:border-zinc-800 dark:bg-zinc-900"
        aria-label="Loading application form"
      />
    ),
  }
);

// Re-export with the same props interface so the page can use it identically
// to how it used ApplicationWizard directly.
interface Props {
  jobId: string;
  jobTitle: string;
}

export function ApplicationWizardClient({ jobId, jobTitle }: Props) {
  return <ApplicationWizard jobId={jobId} jobTitle={jobTitle} />;
}
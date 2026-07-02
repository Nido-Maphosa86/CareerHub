// src/app/jobs/[id]/page.tsx
// Assignment 3.3 — Part 2 (Step 2) & Part 4 (Step 2).
//
// Two performance/SEO additions on top of the 2.3 / 3.1 work:
//
// 1. generateMetadata (Part 2, Step 2)
//    Fetches the job and produces title/description/og tags specific to that job.
//    The title "Senior Frontend Engineer" is composed with the layout template to
//    produce "Senior Frontend Engineer | CareerHub" in the browser tab — which is
//    exactly what candidates searching for that role see in Google results.
//    DEDUPLICATION: both generateMetadata and the page component call getJob().
//    Next.js deduplicates the underlying fetch because it is tagged with the same
//    cache tag ("jobs", `job-${id}`) and the same URL. The second call returns the
//    cached result — zero extra network requests.
//
// 2. Dynamic import of ApplicationWizard (Part 4, Step 2)
//    ApplicationWizard brings in React Hook Form, Zod, TanStack Query, and
//    AlertDialog — a substantial JS payload. A user who is not logged in, or who
//    is an employer, never interacts with the wizard. Loading it eagerly harms
//    their Time to Interactive (TTI) and delays the job details they actually
//    came for. Dynamic import with ssr: false defers that bundle to a separate
//    chunk that only downloads after the main page paints. ssr: false is required
//    because the wizard uses useSession(), localStorage, and browser-only APIs
//    that throw when Next.js tries to render them server-side.
//    The loading skeleton targets CLS: it reserves the wizard's approximate
//    height (h-96) so the layout does not shift when the bundle loads.

import type { Metadata } from "next";
import dynamic from "next/dynamic";
import Link from "next/link";
import { notFound } from "next/navigation";
import { JobListing } from "@/types";
import { JobStatusBadge } from "@/components/JobStatusBadge";
import { auth } from "@/auth";
import { ArrowLeft, MapPin, Lock, ShieldAlert } from "lucide-react";

// ── Types ─────────────────────────────────────────────────────────────────────

interface Props {
  params: Promise<{ id: string }>;
}

// ── Data fetching (shared by generateMetadata and the page component) ─────────

// Extracted into a standalone function so both generateMetadata and the page
// component call the same function — a requirement for Next.js request
// deduplication to work. If each called fetch() independently with a raw URL
// the caches might not align and two requests would fire.
async function getJob(id: string): Promise<JobListing | null> {
  const res = await fetch(`${process.env.NEXT_PUBLIC_API_URL}/Jobs/${id}`, {
    // Cache tags make the result shareable across deduplication. The same tags
    // are used here and in generateMetadata so Next.js recognises it is the
    // same logical resource and collapses the two calls into one.
    next: { tags: ["jobs", `job-${id}`] },
  });

  // 404 is expected for bad IDs; anything else is a real error.
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Failed to fetch job: ${res.status}`);

  return res.json();
}

// ── generateMetadata (Part 2, Step 2) ────────────────────────────────────────

export async function generateMetadata({ params }: Props): Promise<Metadata> {
  const { id } = await params;
  const job = await getJob(id);

  // If the job does not exist, tell search engines the page is not found.
  if (!job) {
    return { title: "Job Not Found" };
  }

  // Build a descriptive sentence from the job data so the search snippet is
  // informative rather than a generic placeholder.
  const description = `Apply for ${job.title} at ${job.companyName} in ${job.location}. ${job.salaryDisplay ? `Salary: ${job.salaryDisplay}.` : ""}`;

  return {
    // The template in layout.tsx turns this into "Senior Frontend Engineer | CareerHub".
    title: job.title,
    description,
    openGraph: {
      title: job.title,
      description,
      type: "website",
    },
  };
}

// ── Dynamic import of ApplicationWizard (Part 4, Step 2) ─────────────────────

// Named export requires the .then(mod => ...) pattern — dynamic() expects a
// default export from the imported module, so we pull the named export out.
const ApplicationWizard = dynamic(
  () =>
    import("@/components/ApplicationWizard").then((mod) => ({
      default: mod.ApplicationWizard,
    })),
  {
    // ssr: false because the wizard uses useSession(), localStorage, and other
    // browser-only APIs. Rendering it server-side would throw at runtime.
    ssr: false,

    // The loading skeleton reserves h-96 so the page layout does not jump when
    // the wizard bundle loads. This directly targets CLS (Cumulative Layout Shift).
    loading: () => (
      <div
        className="h-96 w-full animate-pulse rounded-xl border border-zinc-200 bg-zinc-100 dark:border-zinc-800 dark:bg-zinc-900"
        aria-label="Loading application form"
      />
    ),
  }
);

// ── Page component ────────────────────────────────────────────────────────────

export default async function JobDetailPage({ params }: Props) {
  const { id } = await params;

  // Run the job fetch and session read in parallel — no reason to wait for one
  // before starting the other. The job fetch is deduplicated with generateMetadata.
  const [job, session] = await Promise.all([getJob(id), auth()]);

  // Use the page-level notFound() rather than returning null so Next.js renders
  // the closest not-found.tsx boundary instead of a blank page.
  if (!job) {
    notFound();
  }

  // Derive the three flags that drive what the apply area renders.
  const isClosed = job.status === "Closed";
  const role = session?.user?.role ?? null;
  const isSignedIn = !!session;
  const isEmployer = role === "employer";

  return (
    <div className="mx-auto max-w-3xl">
      {/* Back link. */}
      <Link
        href="/jobs"
        className="mb-6 inline-flex items-center gap-1.5 text-sm font-medium text-zinc-500 transition-colors hover:text-lime-600 dark:text-zinc-400 dark:hover:text-lime-400"
      >
        <ArrowLeft className="h-4 w-4" />
        Back to jobs
      </Link>

      {/* Job details card. */}
      <div className="rounded-xl border border-zinc-200 bg-white p-6 dark:border-zinc-800 dark:bg-zinc-950 sm:p-8">
        {/* Company eyebrow. */}
        <div className="text-xs font-semibold uppercase tracking-widest text-lime-600 dark:text-lime-400">
          {job.companyName}
        </div>

        {/* The h1 uses the job title — this is what search engines index as the
            primary heading of the page and what generateMetadata also reports. */}
        <h1 className="mt-1 text-3xl font-black tracking-tight text-zinc-900 dark:text-zinc-50">
          {job.title}
        </h1>

        <div className="mt-3 flex flex-wrap items-center gap-3">
          <JobStatusBadge type={job.type} />
          <span className="flex items-center gap-1.5 text-sm text-zinc-500 dark:text-zinc-400">
            <MapPin className="h-3.5 w-3.5" />
            {job.location}
          </span>
          <span className="text-sm font-medium text-zinc-400 dark:text-zinc-500">
            {job.status}
          </span>
        </div>

        <p className="mt-6 whitespace-pre-line text-sm leading-relaxed text-zinc-600 dark:text-zinc-300">
          {job.description}
        </p>

        <div className="mt-6 border-t border-zinc-100 pt-4 dark:border-zinc-800">
          <span className="text-base font-semibold text-zinc-900 dark:text-zinc-100">
            {job.salaryDisplay}
          </span>
        </div>
      </div>

      {/* Apply area — role-gated (Assignment 2.3 Part 5).
          The wizard is dynamically imported so its bundle is separate from the
          above-the-fold job details. Users who cannot apply still see the details
          instantly while the wizard chunk loads (or not at all, for employers). */}
      <div className="mt-6">
        {isClosed ? (
          // Closed listing — no form at all.
          <div className="flex items-start gap-3 rounded-xl border border-zinc-200 bg-zinc-50 p-6 text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900/50 dark:text-zinc-300">
            <Lock className="mt-0.5 h-5 w-5 shrink-0" />
            <div>
              <p className="font-semibold text-zinc-800 dark:text-zinc-100">
                Applications closed
              </p>
              <p className="mt-1 text-sm">
                This listing is no longer accepting applications.
              </p>
            </div>
          </div>
        ) : isEmployer ? (
          // Employers can view but not apply.
          <div className="flex items-start gap-3 rounded-xl border border-amber-300 bg-amber-50 p-6 text-amber-800 dark:border-amber-900 dark:bg-amber-950/40 dark:text-amber-200">
            <ShieldAlert className="mt-0.5 h-5 w-5 shrink-0" />
            <div>
              <p className="font-semibold">Employers cannot apply for jobs.</p>
              <p className="mt-1 text-sm">
                This account manages listings. Applications are for candidate accounts.
              </p>
            </div>
          </div>
        ) : !isSignedIn ? (
          // Signed-out: show the wizard with a note. The wizard itself blocks
          // advancing past step 1 until the user has a candidate session.
          <div>
            <div className="mb-4 rounded-xl border border-zinc-200 bg-zinc-50 px-4 py-3 text-sm text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900/50 dark:text-zinc-300">
              You must be signed in to apply.{" "}
              <Link
                href="/login"
                className="font-semibold text-lime-600 underline-offset-2 hover:underline dark:text-lime-400"
              >
                Sign in here.
              </Link>
            </div>
            {/* Dynamic import means the wizard bundle only downloads after the
                job details have painted — not-signed-in users see the details
                immediately even if the wizard chunk is still loading. */}
            <ApplicationWizard jobId={job.id} jobTitle={job.title} />
          </div>
        ) : (
          // Authenticated candidate — the wizard renders normally after the
          // dynamic chunk loads.
          <ApplicationWizard jobId={job.id} jobTitle={job.title} />
        )}
      </div>
    </div>
  );
}

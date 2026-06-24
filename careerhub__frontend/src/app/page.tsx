// src/app/page.tsx
// Assignment 2.1 — Part 6: the home landing page.
//
// This is now a Server Component: no "use client", no useState/useEffect/
// useQuery, no tab logic. It renders static marketing content and two navigation
// links, so it ships no JavaScript bundle of its own.

import Link from "next/link";
import { ArrowRight, Briefcase, LayoutDashboard } from "lucide-react";

export default function Home() {
  return (
    <div className="mx-auto max-w-3xl py-12 text-center sm:py-20">
      {/* Eyebrow. */}
      <div className="mb-4 inline-flex items-center rounded-full border border-lime-300 bg-lime-50 px-3 py-1 text-xs font-semibold uppercase tracking-widest text-lime-700 dark:border-lime-400/30 dark:bg-lime-400/10 dark:text-lime-300">
        Find your next role
      </div>

      {/* Headline. */}
      <h1 className="text-4xl font-black tracking-tight text-zinc-900 dark:text-zinc-50 sm:text-5xl">
        Career<span className="text-lime-500 dark:text-lime-400">Hub</span>
      </h1>
      <p className="mx-auto mt-4 max-w-xl text-base text-zinc-600 dark:text-zinc-300 sm:text-lg">
        A modern job board. Browse open positions, view full details, and apply
        in minutes — built on a typed .NET API and the Next.js App Router.
      </p>

      {/* Two link buttons. */}
      <div className="mt-8 flex flex-col items-center justify-center gap-3 sm:flex-row">
        <Link
          href="/jobs"
          className="inline-flex w-full items-center justify-center gap-2 rounded-lg bg-lime-400 px-6 py-3 text-sm font-semibold text-black transition-colors hover:bg-lime-300 sm:w-auto"
        >
          <Briefcase className="h-4 w-4" />
          Browse Jobs
          <ArrowRight className="h-4 w-4" />
        </Link>
        <Link
          href="/dashboard/listings"
          className="inline-flex w-full items-center justify-center gap-2 rounded-lg border border-zinc-300 px-6 py-3 text-sm font-semibold text-zinc-800 transition-colors hover:border-lime-400 hover:text-lime-600 dark:border-zinc-700 dark:text-zinc-100 dark:hover:border-lime-400/50 dark:hover:text-lime-400 sm:w-auto"
        >
          <LayoutDashboard className="h-4 w-4" />
          Employer Dashboard
        </Link>
      </div>
    </div>
  );
}

// src/app/page.tsx
// Assignment 3.3 — Part 2 & Part 3: adds static metadata and a next/image hero.
//
// Static metadata is the right choice here because the home page content never
// changes per-request and does not depend on API data. The title, description,
// and og tags are the same for every visitor, so they can be exported as a plain
// constant — no async generateMetadata needed. Next.js picks it up at build time.
//
// The hero illustration is served through next/image rather than a plain <img>:
//   - next/image converts the SVG to the most efficient format the browser
//     supports (WebP/AVIF for raster; SVGs are served as-is).
//   - The `priority` prop prevents the image from being lazy-loaded. Because it
//     is above the fold on first paint, preloading it directly improves the
//     Largest Contentful Paint (LCP) score — the main CWV metric for load speed.
//   - Explicit width and height let the browser reserve the correct space before
//     the image loads, eliminating Cumulative Layout Shift (CLS).

import type { Metadata } from "next";
import Link from "next/link";
import Image from "next/image";
import { ArrowRight, Briefcase, LayoutDashboard } from "lucide-react";

// ── Static metadata (Part 2) ─────────────────────────────────────────────────
// The title "Jobs | CareerHub" would be produced by the template if the page
// exported `title: "Jobs"`. Home is a special case: we want the full brand name
// as the tab title, which is already the layout's `default`, so we override with
// a specific string instead.
export const metadata: Metadata = {
  title: "CareerHub — Find Your Next Role",
  description:
    "Browse open positions from top companies. View full job details and apply in minutes on CareerHub.",
  openGraph: {
    title: "CareerHub — Find Your Next Role",
    description:
      "Browse open positions from top companies. View full job details and apply in minutes on CareerHub.",
    type: "website",
  },
};

export default function Home() {
  return (
    <div className="mx-auto max-w-3xl py-12 text-center sm:py-20">
      {/* Eyebrow badge. */}
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

      {/* Hero illustration (Part 3, Candidate A).
          priority: this image is the largest visible element on first paint, so
          it is the Largest Contentful Paint (LCP) element. The priority prop
          tells the browser to preload it rather than lazy-load it, which is the
          single biggest lever for improving LCP on an image-heavy above-the-fold
          area. width/height match the SVG viewBox so the browser reserves the
          correct space before the image loads — preventing CLS. */}
      <div className="mx-auto mt-10 max-w-2xl overflow-hidden rounded-2xl border border-zinc-200 dark:border-zinc-800">
        <Image
          src="/hero-illustration.svg"
          alt="A stylised job card showing the CareerHub interface"
          width={800}
          height={400}
          priority
          className="w-full"
        />
      </div>

      {/* CTA buttons. */}
      <div className="mt-10 flex flex-col items-center justify-center gap-3 sm:flex-row">
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

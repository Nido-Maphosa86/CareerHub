// src/app/layout.tsx
// Assignment 3.3 — Part 2 (Step 1): adds the global title template and Open
// Graph metadata so every page in the app is correctly titled and shareable.
//
// The title template works like a nameplate system:
//   - Each page exports its own title (e.g. "Browse Jobs").
//   - Next.js automatically appends " | CareerHub" via the template.
//   - If a page exports no title, the `default` string is used as-is.
// This means the layout sets the pattern once and individual pages only need to
// declare their own short title — no duplication of the site name.
//
// metadataBase tells Next.js the origin so it can resolve relative Open Graph
// URLs (og:image paths etc.) to absolute URLs, which social-media crawlers need.

import type { Metadata } from "next";
import Link from "next/link";
import "./globals.css";
import { Providers } from "./providers";
import { auth } from "@/auth";
import { NavBar } from "@/components/NavBar";
import { ThemeToggle } from "@/components/ThemeToggle";
import { Toaster } from "sonner";

// ── Global metadata (Part 2, Step 1) ────────────────────────────────────────
export const metadata: Metadata = {
  // template: applied to every page that exports its own title.
  // default: used when no page-level title is set (e.g. home page).
  title: {
    template: "%s | CareerHub",
    default: "CareerHub — Find Your Next Role",
  },

  // A one-sentence summary shown in search-engine snippets and when the page is
  // shared on social platforms that do not yet have an og:description.
  description:
    "Browse open positions, view full details, and apply in minutes on CareerHub — a modern job board built for candidates and employers.",

  // The base URL Next.js uses to make relative og:image paths absolute.
  // In production this should be your deployed domain (e.g. https://careerhub.vercel.app).
  metadataBase: new URL(
    process.env.NEXT_PUBLIC_SITE_URL ?? "http://localhost:3000"
  ),

  // Open Graph tags are read by Facebook, LinkedIn, Slack, and most social
  // platforms when someone pastes a link. Without them the preview is a blank box.
  openGraph: {
    siteName: "CareerHub",
    type: "website",
  },
};

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  // Read the session once for the whole layout. auth() verifies the JWT cookie
  // in memory — no network call — so it is cheap on every render.
  const session = await auth();

  return (
    <html lang="en" className="dark" suppressHydrationWarning>
      <body className="min-h-screen antialiased">
        <Providers>
          <div className="min-h-screen">
            {/* Sticky top nav — present on every page. */}
            <header className="border-b border-zinc-200 dark:border-zinc-800">
              <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-4">
                {/* Brand mark links back to the home page. */}
                <Link href="/" className="flex items-center gap-2.5">
                  <div className="flex h-8 w-8 items-center justify-center rounded-md bg-lime-400">
                    <span className="text-base font-black text-black">C</span>
                  </div>
                  <span className="text-xl font-black tracking-tight text-zinc-900 dark:text-zinc-50">
                    Career<span className="text-lime-500 dark:text-lime-400">Hub</span>
                  </span>
                </Link>

                {/* Role-aware nav bar: different links per session role. */}
                <div className="flex items-center gap-3">
                  <NavBar
                    username={session?.user?.name ?? null}
                    role={session?.user?.role ?? null}
                  />
                  <ThemeToggle />
                </div>
              </div>
            </header>

            {/* Page content renders here. */}
            <main className="mx-auto max-w-6xl px-4 py-8">{children}</main>
          </div>

          {/* App-wide toast host (Assignment 3.1 Part 2).
              Top-right avoids the nav bar at the top-left. */}
          <Toaster position="top-right" richColors closeButton />
        </Providers>
      </body>
    </html>
  );
}

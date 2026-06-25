// src/app/layout.tsx
// Root layout — a Server Component. It provides the shared shell every route
// sits inside: the <html>/<body>, the client providers, a persistent header
// with navigation, and the outer <main> with page padding.
//
// The header lives here (not in a page) so it persists across navigations and
// never re-mounts. The interactive bits inside it (NavLinks, AuthStatus,
// ThemeToggle) are their own Client Components; the layout stays a Server
// Component and adds no "use client".

import type { Metadata } from "next";
import Link from "next/link";
import "./globals.css";
import { Providers } from "./providers";
import { NavLinks } from "@/components/NavLinks";
import { AuthStatus } from "@/components/AuthStatus";
import { ThemeToggle } from "@/components/ThemeToggle";

export const metadata: Metadata = {
  title: "CareerHub — Find your next role",
  description: "Browse open positions and apply on CareerHub.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" className="dark" suppressHydrationWarning>
      <body className="min-h-screen antialiased">
        <Providers>
          <div className="min-h-screen">
            {/* Persistent app header. */}
            <header className="border-b border-zinc-200 dark:border-zinc-800">
              <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-4">
                {/* Brand — now a Link to the home page. */}
                <Link href="/" className="flex items-center gap-2.5">
                  <div className="flex h-8 w-8 items-center justify-center rounded-md bg-lime-400">
                    <span className="text-base font-black text-black">C</span>
                  </div>
                  <span className="text-xl font-black tracking-tight text-zinc-900 dark:text-zinc-50">
                    Career<span className="text-lime-500 dark:text-lime-400">Hub</span>
                  </span>
                </Link>

                {/* Navigation + auth + theme. */}
                <div className="flex items-center gap-3">
                  <NavLinks />
                  <AuthStatus />
                  <ThemeToggle />
                </div>
              </div>
            </header>

            {/* Outer content shell — pages render here. */}
            <main className="mx-auto max-w-6xl px-4 py-8">{children}</main>
          </div>
        </Providers>
      </body>
    </html>
  );
}

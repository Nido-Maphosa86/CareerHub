// src/app/layout.tsx
// Assignment 2.3 — Part 5: the root layout is now an async Server Component.
//
// It calls await auth() once per request to read the session, then shows nav
// that matches the signed-in user's role. auth() here is cheap: it verifies the
// signed JWT cookie in memory — there is no database or network call — so doing
// it on every page render is not a performance problem.

import type { Metadata } from "next";
import Link from "next/link";
import "./globals.css";
import { Providers } from "./providers";
import { auth } from "@/auth";
import { NavBar } from "@/components/NavBar";
import { ThemeToggle } from "@/components/ThemeToggle";
import { Toaster } from "sonner";

export const metadata: Metadata = {
  title: "CareerHub — Find your next role",
  description: "Browse open positions and apply on CareerHub.",
};

export default async function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  // Read the session once for the whole layout.
  const session = await auth();

  return (
    <html lang="en" className="dark" suppressHydrationWarning>
      <body className="min-h-screen antialiased">
        <Providers>
          <div className="min-h-screen">
            <header className="border-b border-zinc-200 dark:border-zinc-800">
              <div className="mx-auto flex max-w-6xl items-center justify-between gap-4 px-4 py-4">
                <Link href="/" className="flex items-center gap-2.5">
                  <div className="flex h-8 w-8 items-center justify-center rounded-md bg-lime-400">
                    <span className="text-base font-black text-black">C</span>
                  </div>
                  <span className="text-xl font-black tracking-tight text-zinc-900 dark:text-zinc-50">
                    Career<span className="text-lime-500 dark:text-lime-400">Hub</span>
                  </span>
                </Link>

                <div className="flex items-center gap-3">
                  {/* Role-aware nav: links + identity + sign out. */}
                  <NavBar
                    username={session?.user?.name ?? null}
                    role={session?.user?.role ?? null}
                  />
                  <ThemeToggle />
                </div>
              </div>
            </header>

            <main className="mx-auto max-w-6xl px-4 py-8">{children}</main>
          </div>

          {/* Assignment 3.1 — Part 2: app-wide toast host. Placed top-right.
              The nav sits top-left/centre, so top-right toasts do not overlap it.
              richColors gives success/error their own colour treatment. */}
          <Toaster position="top-right" richColors closeButton />
        </Providers>
      </body>
    </html>
  );
}

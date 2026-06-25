// src/components/NavLinks.tsx
// Assignment 2.1 — Stretch A: active link highlighting.
//
// usePathname() is a hook, so any component that calls it must be a Client
// Component — hence "use client". This is the ONLY client island the header
// needs for navigation; the root layout around it stays a Server Component.

"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import { cn } from "@/lib/utils";

const links = [
  { href: "/jobs", label: "Jobs" },
  { href: "/dashboard/listings", label: "Dashboard" },
];

export function NavLinks() {
  const pathname = usePathname();

  return (
    <nav className="flex items-center gap-1">
      {links.map((link) => {
        // Highlight the link whose path prefixes the current route, so
        // /jobs/[id] still marks "Jobs" active and /dashboard/* marks "Dashboard".
        const isActive =
          pathname === link.href || pathname.startsWith(link.href + "/") ||
          (link.href === "/dashboard/listings" && pathname.startsWith("/dashboard"));

        return (
          <Link
            key={link.href}
            href={link.href}
            className={cn(
              "rounded-lg px-3 py-1.5 text-sm font-medium transition-colors",
              isActive
                ? "bg-lime-100 text-lime-700 dark:bg-lime-400/10 dark:text-lime-300"
                : "text-zinc-600 hover:text-lime-600 dark:text-zinc-300 dark:hover:text-lime-400"
            )}
          >
            {link.label}
          </Link>
        );
      })}
    </nav>
  );
}

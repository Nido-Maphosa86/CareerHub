// src/components/NavBar.tsx
// Assignment 2.3 — Part 5: the role-aware navigation.
//
// This is a Server Component. It receives the username and role from the layout
// (which already read the session) and renders the correct links:
//   - signed out  -> "Sign In" link
//   - candidate   -> "Jobs" link + name + candidate badge + Sign Out
//   - employer    -> "Dashboard" link + name + employer badge + Sign Out
//
// Candidates never see the Dashboard link; employers never see the Jobs link.
// Sign Out is an inline Server Action so no client JS is needed for it.


// navigation bar that shows different things depending on who is signed in.
import Link from "next/link";
import { signOut } from "@/auth";
import { LogOut, User } from "lucide-react";

interface Props {
  username: string | null;
  role: string | null;
}

export function NavBar({ username, role }: Props) {
  // Signed out — only a Sign In link.
  if (!username) {
    return (
      <Link
        href="/login"
        className="rounded-lg bg-lime-400 px-3 py-1.5 text-sm font-bold text-black transition-colors hover:bg-lime-300"
      >
        Sign In
      </Link>
    );
  }

  const isEmployer = role === "employer";

  return (
    <div className="flex items-center gap-3">
      {/* Role-specific link. */}
      {isEmployer ? (
        <Link
          href="/dashboard/listings"
          className="text-sm font-medium text-zinc-600 transition-colors hover:text-lime-600 dark:text-zinc-300 dark:hover:text-lime-400"
        >
          Dashboard
        </Link>
      ) : (
        <Link
          href="/jobs"
          className="text-sm font-medium text-zinc-600 transition-colors hover:text-lime-600 dark:text-zinc-300 dark:hover:text-lime-400"
        >
          Jobs
        </Link>
      )}

      {/* Identity + role badge. */}
      <span className="hidden items-center gap-1.5 text-sm text-zinc-600 dark:text-zinc-300 sm:flex">
        <User className="h-3.5 w-3.5" />
        {username}
        <span className="rounded-full bg-lime-100 px-2 py-0.5 text-xs font-semibold text-lime-700 dark:bg-lime-400/10 dark:text-lime-300">
          {role}
        </span>
      </span>

      {/* Sign Out — inline Server Action. redirect to a home page*/}
      <form
        action={async () => {
          "use server";
          await signOut({ redirectTo: "/" });
        }}
      >
        <button
          type="submit"
          className="flex items-center gap-1.5 rounded-lg border border-zinc-300 px-2.5 py-1.5 text-sm font-medium text-zinc-600 transition-colors hover:border-red-400 hover:text-red-600 dark:border-zinc-700 dark:text-zinc-300 dark:hover:border-red-500 dark:hover:text-red-400"
        >
          <LogOut className="h-3.5 w-3.5" />
          Sign Out
        </button>
      </form>
    </div>
  );
}

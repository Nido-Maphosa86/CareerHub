// src/app/login/page.tsx
// Assignment 2.3 — Part 3: the login page (a Server Component).
//
// The form posts to an inline Server Action that calls Auth.js signIn().
//
// The role-based redirect problem: signIn() runs BEFORE the session cookie is
// written, so we cannot read the new user's role from a session at the moment we
// must decide where to send them. The fix is to determine the redirect target
// from the SAME source authorize() uses — the credentials themselves — by
// looking the username up in the mock users list before calling signIn, then
// passing the chosen destination as redirectTo. signIn validates the password
// and performs the redirect only if the credentials are actually valid.

import { signIn } from "@/auth";
import { AuthError } from "next-auth";
import { redirect } from "next/navigation";
import { roleForUsername, redirectForRole } from "@/lib/loginRedirect";
import { AlertCircle } from "lucide-react";

interface Props {
  searchParams: Promise<{ error?: string; callbackUrl?: string }>;
}

export default async function LoginPage({ searchParams }: Props) {
  const { error } = await searchParams;

  // The inline Server Action that runs when the form is submitted.
  async function authenticate(formData: FormData) {
    "use server";


    //when the form is submitted
    //reads username and passowrd from the form data
    const username = String(formData.get("username") ?? "");
    const password = String(formData.get("password") ?? "");

    // Decide the destination from the username before signIn (see file header).
    const role = roleForUsername(username);
    const redirectTo = redirectForRole(role);
   

    //call sign in with the credentials and the chosen destination
    //if error redirects to thelogin page
    try {
      await signIn("credentials", { username, password, redirectTo });
    } catch (err) {
      // A successful signIn throws a special redirect error that Next.js must
      // be allowed to propagate. Only real auth failures get turned into the
      // ?error=CredentialsSignin query the page reads below.
      if (err instanceof AuthError) {
        redirect("/login?error=CredentialsSignin");
      }
      throw err;
    }
  }

  const hasError = error === "CredentialsSignin";

  return (
    <div className="mx-auto max-w-sm py-12">
      <div className="rounded-2xl border border-zinc-200 bg-white p-8 dark:border-zinc-800 dark:bg-zinc-950">
        <h1 className="text-2xl font-black tracking-tight text-zinc-900 dark:text-zinc-50">
          Sign in
        </h1>
        <p className="mt-1 text-sm text-zinc-500 dark:text-zinc-400">
          Use your CareerHub account to continue.
        </p>

        {hasError && (
          <div className="mt-5 flex items-start gap-2 rounded-xl border border-red-300 bg-red-50 px-4 py-3 text-sm text-red-700 dark:border-red-900 dark:bg-red-950/40 dark:text-red-300">
            <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
            <span>Invalid username or password. Please try again.</span>
          </div>
        )}

        <form action={authenticate} className="mt-6 flex flex-col gap-4">
          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="username"
              className="text-xs font-semibold uppercase tracking-widest text-zinc-500 dark:text-zinc-400"
            >
              Username
            </label>
            <input
              id="username"
              name="username"
              required
              autoComplete="username"
              className="rounded-lg border border-zinc-300 bg-white px-4 py-2.5 text-sm text-zinc-900 outline-none transition-colors focus:border-lime-500 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-100"
            />
          </div>

          <div className="flex flex-col gap-1.5">
            <label
              htmlFor="password"
              className="text-xs font-semibold uppercase tracking-widest text-zinc-500 dark:text-zinc-400"
            >
              Password
            </label>
            <input
              id="password"
              name="password"
              type="password"
              required
              autoComplete="current-password"
              className="rounded-lg border border-zinc-300 bg-white px-4 py-2.5 text-sm text-zinc-900 outline-none transition-colors focus:border-lime-500 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-100"
            />
          </div>

          <button
            type="submit"
            className="mt-2 rounded-xl bg-lime-400 px-4 py-2.5 text-sm font-bold text-black transition-colors hover:bg-lime-300"
          >
            Sign in
          </button>
        </form>
      </div>
    </div>
  );
}

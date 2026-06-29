// src/middleware.ts
// Assignment 2.3 — Part 4: route protection by role and session state.
//
// Middleware runs on the edge before a route renders. It is the right place for
// coarse, whole-route access rules: "only employers may reach /dashboard". It
// reads the session from the JWT cookie (via auth()) and redirects when needed.
//
// What it does NOT do: gate the apply FORM on /jobs/[id]. That is a within-page
// distinction (employers may VIEW the detail, only candidates see the form), so
// it lives in the page component, not here.

import { auth } from "@/auth";
import { NextResponse } from "next/server";


//everytime someone navigates to a url, the middle intercept the request
//and check the session, and decides whether to let it through or redirect it somewhere else.
export default auth((req) => {
  const { nextUrl } = req;
  const session = req.auth;
  const role = session?.user?.role;
  const isLoggedIn = !!session;

  const path = nextUrl.pathname;
  const isDashboard = path.startsWith("/dashboard");
  const isLogin = path === "/login";

  // /dashboard/* — employers only.
  if (isDashboard) {
    if (!isLoggedIn) {
      // Unauthenticated -> send to login.
      return NextResponse.redirect(new URL("/login", nextUrl));
    }
    if (role !== "employer") {
      // Logged in but wrong role (candidate) -> send to jobs.
      return NextResponse.redirect(new URL("/jobs", nextUrl));
    }
  }

  // /login — already signed-in users should not see login page again.
  if (isLogin && isLoggedIn) {
    const dest = role === "employer" ? "/dashboard/listings" : "/jobs";
    return NextResponse.redirect(new URL(dest, nextUrl));
  }

  // Everything else (/jobs, /jobs/[id], public pages) passes through untouched.
  return NextResponse.next();
});

// The matcher excludes static assets and the Auth.js API so middleware only
// runs on real navigations.
export const config = {
  matcher: ["/((?!_next/static|_next/image|favicon.ico|api/auth).*)"],
};

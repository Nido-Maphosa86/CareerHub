// src/app/actions/closeJob.ts
// Assignment 2.2 — Part 6: the Server Action that closes a job listing.
//
// "use server" marks every export in this file as a Server Action — a function
// that runs ONLY on the server but can be invoked from a Client Component
// (here, via a <form action={...}>). The browser never sees this code; it sends
// the form data to the server, the server runs the function, and only the
// return value comes back.
//
// ADAPTATION (real backend): the assignment's demo PATCHes a mock endpoint. Our
// jobs are real, so to make the close visible on the real /jobs page we close
// via the real CareerHub.Api. That endpoint (DELETE /Jobs/{id}) requires an
// Employer token, so the action logs in as the seeded employer server-side to
// obtain one. revalidateTag then clears the cached jobs data so both the
// candidate and employer views refetch fresh.

"use server";

import { revalidateTag } from "next/cache";

// Discriminated union — the action returns exactly one of these shapes.
export type CloseJobState =
  | { status: "success"; jobTitle: string }
  | { status: "error"; message: string }
  | null;

const API = process.env.NEXT_PUBLIC_API_URL;

// Log in as the seeded employer to obtain a token for the close call.
// In a full multi-tenant app the action would use the authenticated employer's
// own identity; for this single-seeded-employer demo a server-side login keeps
// the action self-contained.
async function getEmployerToken(): Promise<string | null> {
  const res = await fetch(`${API}/auth/login`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ username: "employer", password: "password123" }),
    cache: "no-store",
  });
  if (!res.ok) return null;
  const data = (await res.json()) as { token?: string };
  return data.token ?? null;
}

export async function closeJobListing(
  _prevState: CloseJobState,
  formData: FormData
): Promise<CloseJobState> {
  // 1. Read the job id from the submitted form. No id -> error, no network call.
  const jobId = (formData.get("jobId") as string | null)?.trim();
  if (!jobId) {
    return { status: "error", message: "No job selected to close." };
  }

  // 2. Get an employer token.
  const token = await getEmployerToken();
  if (!token) {
    return { status: "error", message: "Could not authenticate to close the listing." };
  }

  const auth = { Authorization: `Bearer ${token}` };

  // 3. Read the job first so we can confirm it exists and report its title.
  const getRes = await fetch(`${API}/Jobs/${jobId}`, { cache: "no-store" });
  if (getRes.status === 404) {
    return { status: "error", message: "That job no longer exists." };
  }
  if (!getRes.ok) {
    return { status: "error", message: `Could not load the job (${getRes.status}).` };
  }
  const job = (await getRes.json()) as { title: string };

  // 4. Close it on the real backend.
  const closeRes = await fetch(`${API}/Jobs/${jobId}`, {
    method: "DELETE",
    headers: auth,
    cache: "no-store",
  });

  if (!closeRes.ok) {
    // Surface the API's Problem Details `detail` if present.
    const problem = await closeRes.json().catch(() => ({}));
    const message =
      (problem as { detail?: string; title?: string }).detail ??
      (problem as { detail?: string; title?: string }).title ??
      `Failed to close the listing (${closeRes.status}).`;
    return { status: "error", message };
  }

  // 5. Invalidate every cached response tagged "jobs" (the candidate /jobs list
  //    and the dashboard) plus this job's own detail page. This is what makes
  //    the close visible across routes — it runs on the server before the
  //    action returns, so the next request to either page fetches fresh.
  revalidateTag("jobs");
  revalidateTag(`job-${jobId}`);

  // 6. Success — report which listing was closed.
  return { status: "success", jobTitle: job.title };
}

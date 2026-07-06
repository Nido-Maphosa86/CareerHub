// src/test/msw/handlers.ts
// Assignment 3.2 — Part 4: MSW request handlers (the happy path).
//
// MSW intercepts real HTTP calls during tests and returns these canned
// responses, so the submit flow is tested end to end (component -> fetch ->
// response) without a live backend. The URLs are built from the SAME env var the
// API client uses, so a handler can never silently miss because of a URL typo.

import { http, HttpResponse } from "msw";


//This setup makes sure the tests can run the full submit/close/login 
// flows without needing a real backend. Each handler matches the same URLs your API client uses, so tests stay consistent
const API = process.env.NEXT_PUBLIC_API_URL;

export const handlers = [
  // POST an application — the wizard's submit. Returns 201 + a mock response.
  http.post(`${API}/applications/:listingId`, () =>
    HttpResponse.json(
      {
        jobListingId: "job-1",
        jobTitle: "Software Developer",
        companyName: "Acme",
        applicantId: "applicant-1",
        applicantName: "Alice Candidate",
        submittedAt: new Date().toISOString(),
        status: "Submitted",
      },
      { status: 201 }
    )
  ),

  // GET the jobs list — used by the ["jobs"] invalidation after a submit and by
  // the stats endpoint. Returns an empty list (nothing depends on its contents
  // in these tests).
  http.get(`${API}/Jobs`, () => HttpResponse.json({ data: [] })),

  // GET a single job — the close action reads the job (for its title) first.
  http.get(`${API}/Jobs/:id`, ({ params }) =>
    HttpResponse.json({
      id: params.id,
      title: "Software Developer",
      companyName: "Acme",
      location: "Remote",
      status: "Active",
    })
  ),

  // DELETE a job — the close action's actual close call. 204 No Content.
  http.delete(`${API}/Jobs/:id`, () => new HttpResponse(null, { status: 204 })),

  // POST login — the close action logs in server-side to get an employer token.
  http.post(`${API}/auth/login`, () =>
    HttpResponse.json({ token: "fake-employer-token" })
  ),
];

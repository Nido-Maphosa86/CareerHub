// src/test/ApplicationWizard.test.tsx
// Assignment 3.2 — Parts 3 & 4: behaviour tests for the application wizard.
//
// Every test describes what the USER experiences (what appears on screen),
// never how the component is built internally. Queries prefer getByRole and
// getByLabelText so the tests also verify accessible names and label wiring.

import { describe, it, expect } from "vitest";
import { screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { server } from "./msw/server";
import { renderWithProviders } from "./utils";
import { ApplicationWizard } from "@/components/ApplicationWizard";

const API = process.env.NEXT_PUBLIC_API_URL;

// Standard props for the wizard under test.
const props = { jobId: "job-1", jobTitle: "Software Developer" };

// Walks the wizard from step 1 to the review step, filling required fields.
// Reused by the submit tests so the fill logic is not duplicated.
async function fillAllSteps(user: ReturnType<typeof userEvent.setup>) {
  // Step 1
  await user.type(screen.getByLabelText(/full name/i), "Alice Candidate");
  await user.type(screen.getByLabelText(/email address/i), "alice@example.com");
  await user.click(screen.getByRole("button", { name: /next/i }));
  // Step 2 (source is required)
  await user.selectOptions(screen.getByLabelText(/how did you hear/i), "LinkedIn");
  await user.click(screen.getByRole("button", { name: /next/i }));
  // Now on step 3 (review)
}

// ---- Smoke test (Part 2) ----------------------------------------------
describe("ApplicationWizard smoke test", () => {
  it("renders the step 1 heading", () => {
    renderWithProviders(<ApplicationWizard {...props} />);
    expect(screen.getByRole("heading", { name: "Your Details" })).toBeVisible();
  });
});

// ---- Step navigation (Part 3) -----------------------------------------
describe("ApplicationWizard — step navigation", () => {
  // Test 1
  it("renders the step 1 heading on mount", () => {
    renderWithProviders(<ApplicationWizard {...props} />);
    expect(screen.getByRole("heading", { name: "Your Details" })).toBeVisible();
  });

  // Test 2
  it("blocks advancement when required step 1 fields are empty", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ApplicationWizard {...props} />);

    await user.click(screen.getByRole("button", { name: /next/i }));

    // Validation errors for the required fields appear...
    expect(await screen.findByText(/full name must be at least 2 characters/i)).toBeVisible();
    expect(screen.getByText(/enter a valid email address/i)).toBeVisible();
    // ...and we are still on step 1.
    expect(screen.getByRole("heading", { name: "Your Details" })).toBeVisible();
  });

  // Test 3
  it("advances to step 2 when step 1 required fields are filled", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ApplicationWizard {...props} />);

    await user.type(screen.getByLabelText(/full name/i), "Alice Candidate");
    await user.type(screen.getByLabelText(/email address/i), "alice@example.com");
    await user.click(screen.getByRole("button", { name: /next/i }));

    expect(await screen.findByRole("heading", { name: "Your Application" })).toBeVisible();
  });

  // Test 4
  it("preserves step 1 values when Back is clicked", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ApplicationWizard {...props} />);

    await user.type(screen.getByLabelText(/full name/i), "Alice Candidate");
    await user.type(screen.getByLabelText(/email address/i), "alice@example.com");
    await user.click(screen.getByRole("button", { name: /next/i }));

    // On step 2 — go back.
    await screen.findByRole("heading", { name: "Your Application" });
    await user.click(screen.getByRole("button", { name: /back/i }));

    // Step 1 values are intact.
    expect(screen.getByDisplayValue("Alice Candidate")).toBeInTheDocument();
    expect(screen.getByDisplayValue("alice@example.com")).toBeInTheDocument();
  });
});

// ---- Auth gate (Part 3) -----------------------------------------------
describe("ApplicationWizard — auth gate", () => {
  // Test 5
  it("shows the sign-in message and does not advance when the user is not authenticated", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ApplicationWizard {...props} />, { session: null });

    await user.type(screen.getByLabelText(/full name/i), "Alice Candidate");
    await user.type(screen.getByLabelText(/email address/i), "alice@example.com");
    await user.click(screen.getByRole("button", { name: /next/i }));

    // The sign-in prompt appears...
    expect(
      await screen.findByText(/signed in as a candidate to apply/i)
    ).toBeVisible();
    // ...and step 2 is NOT shown.
    expect(
      screen.queryByRole("heading", { name: "Your Application" })
    ).not.toBeInTheDocument();
  });

  // Test 6
  it("advances normally when the user is authenticated as a candidate", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ApplicationWizard {...props} />); // default candidate session

    await user.type(screen.getByLabelText(/full name/i), "Alice Candidate");
    await user.type(screen.getByLabelText(/email address/i), "alice@example.com");
    await user.click(screen.getByRole("button", { name: /next/i }));

    expect(await screen.findByRole("heading", { name: "Your Application" })).toBeVisible();
  });
});

// ---- Review step (Part 3) ---------------------------------------------
describe("ApplicationWizard — review step", () => {
  // Test 7
  it("shows all entered values and 'Not provided' for blank optionals", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ApplicationWizard {...props} />);

    await fillAllSteps(user);

    // Entered values appear on the review screen.
    expect(await screen.findByRole("heading", { name: "Review & Submit" })).toBeVisible();
    expect(screen.getByText("Alice Candidate")).toBeInTheDocument();
    expect(screen.getByText("alice@example.com")).toBeInTheDocument();
    // "LinkedIn" appears as both the LinkedIn-URL row label and the "Heard via"
    // value, so assert at least one match rather than a unique one.
    expect(screen.getAllByText("LinkedIn").length).toBeGreaterThan(0);
    // Optional fields left blank (phone, cover letter, linkedin url) show this.
    expect(screen.getAllByText("Not provided").length).toBeGreaterThan(0);
  });
});

// ---- Submit flow with MSW (Part 4) ------------------------------------
describe("ApplicationWizard — submit flow", () => {
  // Test 8
  it("resets to step 1 with cleared fields after a successful submission", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ApplicationWizard {...props} />);

    await fillAllSteps(user);
    await user.click(screen.getByRole("button", { name: /submit application/i }));

    // The wizard resets to step 1 on success.
    expect(await screen.findByRole("heading", { name: "Your Details" })).toBeVisible();
    // The form was reset — the name field is empty.
    expect(screen.getByLabelText(/full name/i)).toHaveValue("");
  });

  // Test 9
  it("keeps the entered values when the API returns an error", async () => {
    // Force the submit endpoint to fail for this test only.
    server.use(
      http.post(`${API}/applications/:listingId`, () =>
        new HttpResponse(null, { status: 500 })
      )
    );

    const user = userEvent.setup();
    renderWithProviders(<ApplicationWizard {...props} />);

    await fillAllSteps(user);
    await user.click(screen.getByRole("button", { name: /submit application/i }));

    // Wait for the submit button to settle back to its idle label.
    await screen.findByRole("button", { name: /submit application/i });

    // The form was NOT reset — walking back to step 1 shows the typed value.
    await user.click(screen.getByRole("button", { name: /back/i })); // step 2
    await user.click(screen.getByRole("button", { name: /back/i })); // step 1
    expect(screen.getByLabelText(/full name/i)).toHaveValue("Alice Candidate");
  });
});

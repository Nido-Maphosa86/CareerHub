// src/test/CloseJobButton.test.tsx
// Assignment 3.2 — Part 4 Step 5: behaviour tests for the close-job confirmation.
//
// The close Server Action calls revalidateTag from next/cache, which only exists
// inside the Next.js runtime — so it is mocked to a no-op here. The action's
// network calls (employer login, read job, delete job) are intercepted by MSW.

import { describe, it, expect, vi } from "vitest";
import { screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { http, HttpResponse } from "msw";
import { server } from "./msw/server";
import { renderWithProviders } from "./utils";
import { CloseJobButton } from "@/components/CloseJobButton";

// revalidateTag is a server-only API; make it a no-op in tests.
vi.mock("next/cache", () => ({
  revalidateTag: vi.fn(),
}));

const API = process.env.NEXT_PUBLIC_API_URL;

describe("CloseJobButton", () => {

  // Test 10 clicking Close opens the confirmation dialog
  it("opens the confirmation dialog when the close button is clicked", async () => {
    const user = userEvent.setup();
    renderWithProviders(<CloseJobButton jobId="job-1" currentStatus="Active" />);

    // The dialog title is not present until the button is clicked.
    expect(screen.queryByText("Close this listing?")).not.toBeInTheDocument();

    await user.click(screen.getByRole("button", { name: /close/i }));

    // The AlertDialog (rendered in a portal, but still queryable via screen)
    // now shows its title.
    expect(screen.getByText("Close this listing?")).toBeVisible();
  });

  // Test 11 clicking Close then confirming actually calls the delete API
  it("calls the close API when the user confirms", async () => {
    let deleteCalled = false;

    // Track that the DELETE actually fired.
    server.use(
      http.delete(`${API}/Jobs/:id`, () => {
        deleteCalled = true;
        return new HttpResponse(null, { status: 204 });
      })
    );

    const user = userEvent.setup();
    renderWithProviders(<CloseJobButton jobId="job-1" currentStatus="Active" />);

    // Open the dialog, then confirm.
    await user.click(screen.getByRole("button", { name: /^close$/i }));
    await user.click(screen.getByRole("button", { name: /close listing/i }));

    // The close endpoint was hit through MSW.
    await waitFor(() => expect(deleteCalled).toBe(true));
  });
});

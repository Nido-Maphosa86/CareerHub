// src/components/CloseJobButton.tsx
// Assignment 2.2 — Part 6: the client button that triggers the close action.
//
// "use client" because it uses the useActionState hook and renders an
// interactive form. useActionState wires a Server Action to a <form>: it gives
// back the latest returned state, a formAction to put on the form, and an
// isPending flag that is true while the action is in flight.

"use client";

import { useActionState } from "react";
import { closeJobListing, type CloseJobState } from "@/app/actions/closeJob";
import { CheckCircle2 } from "lucide-react";

interface Props {
  jobId: string;
  currentStatus: string;
}

export function CloseJobButton({ jobId, currentStatus }: Props) {
  // Hooks must run unconditionally and in the same order every render, so the
  // hook call comes before any early return.
  // null is the initial state (matches the action's CloseJobState union).
  const [state, formAction, isPending] = useActionState<CloseJobState, FormData>(
    closeJobListing,
    null
  );

  // Already-closed jobs show nothing in the Action column.
  if (currentStatus === "Closed") {
    return null;
  }

  // After a successful close, replace the button with a confirmation.
  if (state?.status === "success") {
    return (
      <span className="inline-flex items-center gap-1.5 text-xs font-medium text-lime-600 dark:text-lime-400">
        <CheckCircle2 className="h-3.5 w-3.5" />
        Closed “{state.jobTitle}”
      </span>
    );
  }

  return (
    <form action={formAction} className="flex flex-col items-start gap-1">
      {/* The action reads jobId from the form data. */}
      <input type="hidden" name="jobId" value={jobId} />

      <button
        type="submit"
        disabled={isPending}
        className="rounded-md border border-zinc-300 px-2.5 py-1 text-xs font-semibold text-zinc-700 transition-colors hover:border-red-400 hover:text-red-600 disabled:cursor-not-allowed disabled:opacity-50 dark:border-zinc-700 dark:text-zinc-200 dark:hover:border-red-500 dark:hover:text-red-400"
      >
        {isPending ? "Closing…" : "Close"}
      </button>

      {/* On error, show the message and keep the button active for retry. */}
      {state?.status === "error" && (
        <span className="text-xs text-red-600 dark:text-red-400">
          {state.message}
        </span>
      )}
    </form>
  );
}

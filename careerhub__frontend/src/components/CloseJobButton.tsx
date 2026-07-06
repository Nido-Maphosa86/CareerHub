// src/components/CloseJobButton.tsx
// Assignment 2.2 — close action. Assignment 3.1 — Part 4a: add an AlertDialog
// confirmation and replace inline banners with toasts.
//
// THE PORTAL PROBLEM. AlertDialogContent renders in a Radix portal at the end of
// <body> — OUTSIDE this component's <form>. So a <button type="submit"> placed in
// AlertDialogAction would not belong to any form and would submit nothing. The
// old version relied on a form submit to fire the Server Action; that no longer
// works once the confirm button lives in the portal.
//
// THE FIX (useTransition approach). Keep the existing Server Action, but stop
// driving it with a form submit. Instead call it programmatically from the
// confirm button's onClick, wrapped in startTransition so React tracks the
// pending state. The dialog open state is local useState. On the result we fire
// a success or error toast — no inline banners.

"use client";

//clicking the close button opens a confirmation dialog
import { useState, useTransition } from "react";
import { closeJobListing } from "@/app/actions/closeJob";
import { toast } from "sonner";
import {
  AlertDialog,
  AlertDialogTrigger,
  AlertDialogContent,
  AlertDialogHeader,
  AlertDialogFooter,
  AlertDialogTitle,
  AlertDialogDescription,
  AlertDialogAction,
  AlertDialogCancel,
} from "@/components/ui/alert-dialog";

interface Props {
  jobId: string;
  currentStatus: string;
}

export function CloseJobButton({ jobId, currentStatus }: Props) {
  const [open, setOpen] = useState(false);
  const [isPending, startTransition] = useTransition();

  // Already-closed jobs show nothing in the Action column.
  if (currentStatus === "Closed") {
    return null;
  }

  function handleConfirm() {
    // Build the FormData the Server Action expects, then call it directly.
    // startTransition keeps isPending true while the server work runs.
    startTransition(async () => {
      const formData = new FormData();
      formData.set("jobId", jobId);

      const result = await closeJobListing(null, formData);

      if (result?.status === "success") {
        toast.success(`Listing closed: ${result.jobTitle}`);
        setOpen(false);
      } else {
        toast.error(result?.message ?? "Could not close the listing.");
        // Keep the dialog open so the employer can retry or cancel.
      }
    });
  }

  return (
    <AlertDialog open={open} onOpenChange={setOpen}>
      <AlertDialogTrigger asChild>
        <button
          type="button"
          className="rounded-md border border-zinc-300 px-2.5 py-1 text-xs font-semibold text-zinc-700 transition-colors hover:border-red-400 hover:text-red-600 dark:border-zinc-700 dark:text-zinc-200 dark:hover:border-red-500 dark:hover:text-red-400"
        >
          Close
        </button>
      </AlertDialogTrigger>

      <AlertDialogContent>
        <AlertDialogHeader>
          <AlertDialogTitle>Close this listing?</AlertDialogTitle>
          <AlertDialogDescription>
            This listing will be marked as closed and removed from the public jobs
            board. This cannot be undone.
          </AlertDialogDescription>
        </AlertDialogHeader>
        <AlertDialogFooter>
          {/* Cancel just closes the dialog — the listing is untouched. */}
          <AlertDialogCancel disabled={isPending}>Keep listing</AlertDialogCancel>
          {/* Confirm calls the Server Action via onClick (not a submit).
              onSelect preventDefault stops Radix from auto-closing so the
              dialog stays open if the action errors. */}
          <AlertDialogAction
            onClick={(e) => {
              e.preventDefault();
              handleConfirm();
            }}
            disabled={isPending}
          >
            {isPending ? "Closing…" : "Close listing"}
          </AlertDialogAction>
        </AlertDialogFooter>
      </AlertDialogContent>
    </AlertDialog>
  );
}

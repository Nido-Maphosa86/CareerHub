// src/components/ApplyPanel.tsx
// Client wrapper around the apply experience. The detail page is a Server
// Component and cannot read auth state (useAuth is a hook), so this small
// Client Component does the auth/role gating and then renders the existing
// ApplicationForm unchanged.
//
//   - not logged in            -> LoginPanel
//   - logged in, not Applicant -> informational message
//   - logged in as Applicant   -> ApplicationForm (from Assignment 1.4)
//
// ApplicationForm itself is NOT modified.

"use client";

import { useAuth } from "@/lib/auth";
import { ApplicationForm } from "@/components/ApplicationForm";
import { LoginPanel } from "@/components/LoginPanel";
import { ShieldAlert } from "lucide-react";

interface Props {
  listingId: string;
  jobTitle: string;
}

// The ApplyPanel is a Client Component that checks the user's authentication and role status, and renders the appropriate content based on that status.
export function ApplyPanel({ listingId, jobTitle }: Props) {
  const { isAuthenticated, isApplicant } = useAuth();

  //checks if the user is logged in and if they are an applicant. If not, it shows the appropriate message or form.
  if (!isAuthenticated) {
    return <LoginPanel />;
  }


  // Logged in, but not an applicant — show a message.
  if (!isApplicant) {
    return (
      <div className="flex items-start gap-3 rounded-xl border border-amber-300 bg-amber-50 p-6 text-amber-800 dark:border-amber-900 dark:bg-amber-950/40 dark:text-amber-200">
        <ShieldAlert className="mt-0.5 h-5 w-5 shrink-0" />
        <div>
          <p className="font-semibold">Applicant account required</p>
          <p className="mt-1 text-sm">
            You&apos;re logged in, but only applicant accounts can apply for jobs.
            Log out and sign in as an applicant.
          </p>
        </div>
      </div>
    );
  }
  
  // Logged in as applicant — show the form
  return <ApplicationForm listingId={listingId} jobTitle={jobTitle} />;
}

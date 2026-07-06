// src/components/ApplicationWizard.tsx
// Assignment 3.1 — Part 3 & Part 4b: the multi-step application wizard that
// replaces the old single-page ApplicationForm.
//
// Three steps: Your Details -> Your Application -> Review & Submit. One Zod
// schema covers every field, with a cross-step refine on the LinkedIn URL. Step
// validation uses trigger() with an explicit field list so "Next" only checks
// the CURRENT step. The form auto-saves a draft to localStorage on every change
// and restores it (with a visible banner) on mount. A "Discard draft" button
// (AlertDialog-confirmed) appears only when a draft exists.

"use client";

import { useEffect, useState } from "react";
import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { z } from "zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { toast } from "sonner";
import { submitApplication } from "@/lib/api";
import { ApplicationRequest } from "@/types";
import { useAuth } from "@/lib/auth";
import { cn } from "@/lib/utils";
import Link from "next/link";
import { Info, X } from "lucide-react";
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

// ---- Schema (single, covers all steps) --------------------------------

const HOW_HEARD = [
  "Job board",
  "LinkedIn",
  "Referral",
  "Company website",
  "Other",
] as const;

//all are validated by the zod schema,
// but only the current step is validated on Next
const schema = z
  .object({
    fullName: z.string().min(2, "Full name must be at least 2 characters"),
    email: z.string().email("Enter a valid email address"),
    phone: z.string().optional(),
    coverLetter: z.string().optional(),
    linkedinUrl: z.string().optional(),
    source: z.string().min(1, "Please choose an option"),
  })
  // Cross-step rule: if a LinkedIn URL is given, it must be a real LinkedIn URL.
  .refine(
    (data) =>
      !data.linkedinUrl ||
      data.linkedinUrl.startsWith("https://linkedin.com/") ||
      data.linkedinUrl.startsWith("https://www.linkedin.com/"),
    {
      message: "Must start with https://linkedin.com/ or https://www.linkedin.com/",
      path: ["linkedinUrl"],
    }
  );

type WizardData = z.infer<typeof schema>;

const STORAGE_PREFIX = "careerhub-application-";

// Empty defaults used for first load and after discard.
const emptyDefaults: WizardData = {
  fullName: "",
  email: "",
  phone: "",
  coverLetter: "",
  linkedinUrl: "",
  source: "",
};

interface Props {
  jobId: string;
  jobTitle: string;
  isCandidate: boolean; // from the Auth.js session on the server page
}

export function ApplicationWizard({ jobId, jobTitle, isCandidate }: Props) {
  const storageKey = `${STORAGE_PREFIX}${jobId}`;
  const queryClient = useQueryClient();
  const { token } = useAuth();

  const [step, setStep] = useState<1 | 2 | 3>(1);
  const [draftRestored, setDraftRestored] = useState(false);
  const [hasDraft, setHasDraft] = useState(false);
  const [showSignInPrompt, setShowSignInPrompt] = useState(false);

  const form = useForm<WizardData>({
    resolver: zodResolver(schema),
    defaultValues: emptyDefaults,
    mode: "onTouched",
  });

  const { register, trigger, getValues, reset, formState } = form;
  const { errors } = formState;

  // ---- Draft restore on mount ----------------------------------------
  useEffect(() => {
    try {
      const saved = localStorage.getItem(storageKey);
      if (saved) {
        const values = JSON.parse(saved) as WizardData;
        reset(values);            // restore into the form
        setDraftRestored(true);   // show the "restored" banner
        setHasDraft(true);        // enable the Discard button
      }
    } catch {
      // Corrupt draft — ignore and start fresh.
    }
    // run once on mount
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // ---- Draft auto-save on every change -------------------------------
  // form.watch returns a subscription; subscribing in an effect (and
  // unsubscribing on cleanup) means the callback runs on real changes, not on
  // every render.
  useEffect(() => {
    const subscription = form.watch((value) => {
      try {
        localStorage.setItem(storageKey, JSON.stringify(value));
        setHasDraft(true);
      } catch {
        // storage full / unavailable — non-fatal for the form.
      }
    });
    return () => subscription.unsubscribe();
  }, [form, storageKey]);

  // ---- Submit (real backend) -----------------------------------------
  const mutation = useMutation({
    mutationFn: (payload: ApplicationRequest) => {
      if (!token) {
        throw new Error("You must be signed in to apply.");
      }
      return submitApplication(jobId, payload, token);
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ["jobs"] });
      // Clear the draft now that it is submitted.
      localStorage.removeItem(storageKey);
      setHasDraft(false);
      reset(emptyDefaults);
      setStep(1);
      toast.success(`Application submitted for ${jobTitle}`);
    },
    onError: (err) => {
      toast.error(err instanceof Error ? err.message : "Could not submit application.");
    },
  });

  //uses trigger() for step by step validation
  //step1 does compalin about step  validation
  // ---- Step navigation -----------------------------------------------
  async function handleNext() {
    if (step === 1) {
      // Gate: must be a candidate to go past step 1. Do not redirect.
      if (!isCandidate) {
        setShowSignInPrompt(true);
        return;
      }
      const ok = await trigger(["fullName", "email", "phone"]);
      if (ok) setStep(2);
      return;
    }
    if (step === 2) {
      const ok = await trigger(["coverLetter", "linkedinUrl", "source"]);
      if (ok) setStep(3);
      return;
    }
  }

  // Back never re-validates — see README. It simply moves to the previous step
  // so a half-finished current step cannot trap the user on it.
  function handleBack() {
    setStep((s) => (s === 3 ? 2 : 1) as 1 | 2 | 3);
  }

  function handleSubmitFinal() {
    const data = getValues();
    const payload: ApplicationRequest = {
      fullName: data.fullName,
      email: data.email,
      phone: data.phone || undefined,
      // The wizard does not collect these, so send sensible defaults; the
      // backend ApplyRequest still requires them.
      yearsOfExperience: 0,
      coverLetter: data.coverLetter?.trim()
        ? data.coverLetter
        : "No cover letter provided.",
      linkedInUrl: data.linkedinUrl || undefined,
      availableImmediately: true,
      noticePeriodWeeks: 0,
    };
    mutation.mutate(payload);
  }

  // ---- Discard draft (Part 4b) ---------------------------------------
  function handleDiscard() {
    localStorage.removeItem(storageKey);
    reset(emptyDefaults);
    setStep(1);
    setHasDraft(false);
    setDraftRestored(false);
    toast.success("Draft discarded");
  }

  // ---- Styling helpers ------------------------------------------------
  const inputBase =
    "w-full rounded-lg border bg-white px-3 py-2 text-sm text-zinc-900 placeholder-zinc-400 transition-colors focus:outline-none focus:ring-2 focus:ring-lime-400 dark:bg-zinc-900 dark:text-zinc-100 dark:placeholder-zinc-500";
  const labelBase = "mb-1 block text-sm font-medium text-zinc-700 dark:text-zinc-300";
  const errorText = "mt-1 text-xs text-red-600 dark:text-red-400";
  const borderFor = (e: boolean) =>
    e ? "border-red-400 dark:border-red-500" : "border-zinc-300 dark:border-zinc-700";

  const values = getValues();
  const shown = (v?: string) => (v && v.trim() ? v : "Not provided");

  return (
    <div className="rounded-xl border border-zinc-200 bg-white p-6 dark:border-zinc-800 dark:bg-zinc-950">
      {/* Header + step indicator. */}
      <div className="mb-5 flex items-start justify-between gap-4">
        <div>
          <h3 className="text-lg font-bold text-zinc-900 dark:text-zinc-50">
            Apply for this role
          </h3>
          <p className="text-sm text-zinc-500 dark:text-zinc-400">{jobTitle}</p>
        </div>
        <div className="text-xs font-semibold uppercase tracking-widest text-zinc-400">
          Step {step} of 3
        </div>
      </div>

      {/* Progress bar. */}
      <div className="mb-6 h-1.5 w-full overflow-hidden rounded-full bg-zinc-200 dark:bg-zinc-800">
        <div
          className="h-full rounded-full bg-lime-400 transition-all"
          style={{ width: `${(step / 3) * 100}%` }}
        />
      </div>

      {/* Draft-restored banner (dismissible). */}
      {draftRestored && (
        <div className="mb-5 flex items-start justify-between gap-3 rounded-lg border border-lime-300 bg-lime-50 px-4 py-3 text-sm text-lime-800 dark:border-lime-400/30 dark:bg-lime-400/10 dark:text-lime-200">
          <span className="flex items-start gap-2">
            <Info className="mt-0.5 h-4 w-4 shrink-0" />
            You have a saved draft for this application. Restored automatically.
          </span>
          <button
            type="button"
            onClick={() => setDraftRestored(false)}
            className="shrink-0 rounded p-0.5 hover:bg-lime-200/50 dark:hover:bg-lime-400/20"
            aria-label="Dismiss"
          >
            <X className="h-4 w-4" />
          </button>
        </div>
      )}

      {/* ---- STEP 1: Your Details ---- */}
      {step === 1 && (
        <div className="space-y-4">
          <div>
            <label htmlFor="fullName" className={labelBase}>Full name</label>
            <input id="fullName" {...register("fullName")} className={cn(inputBase, borderFor(!!errors.fullName))} />
            {errors.fullName && <p className={errorText}>{errors.fullName.message}</p>}
          </div>
          <div>
            <label htmlFor="email" className={labelBase}>Email address</label>
            <input id="email" type="email" {...register("email")} className={cn(inputBase, borderFor(!!errors.email))} />
            {errors.email && <p className={errorText}>{errors.email.message}</p>}
          </div>
          <div>
            <label htmlFor="phone" className={labelBase}>
              Phone number <span className="text-zinc-400">(optional)</span>
            </label>
            <input id="phone" type="tel" {...register("phone")} className={cn(inputBase, borderFor(!!errors.phone))} />
            {errors.phone && <p className={errorText}>{errors.phone.message}</p>}
          </div>

          {/* Sign-in gate message (shown only after a blocked Next). */}
          {showSignInPrompt && (
            <div className="rounded-lg border border-amber-300 bg-amber-50 px-4 py-3 text-sm text-amber-800 dark:border-amber-900 dark:bg-amber-950/40 dark:text-amber-200">
              You need to be signed in as a candidate to apply.{" "}
              <Link href="/login" className="font-semibold underline-offset-2 hover:underline">
                Sign in here.
              </Link>
            </div>
          )}
        </div>
      )}

      {/* ---- STEP 2: Your Application ---- */}
      {step === 2 && (
        <div className="space-y-4">
          <div>
            <label htmlFor="coverLetter" className={labelBase}>
              Cover letter <span className="text-zinc-400">(optional)</span>
            </label>
            <textarea id="coverLetter" rows={5} {...register("coverLetter")} className={cn(inputBase, borderFor(!!errors.coverLetter), "resize-y")} />
            {errors.coverLetter && <p className={errorText}>{errors.coverLetter.message}</p>}
          </div>
          <div>
            <label htmlFor="linkedinUrl" className={labelBase}>
              LinkedIn profile URL <span className="text-zinc-400">(optional)</span>
            </label>
            <input id="linkedinUrl" type="url" placeholder="https://www.linkedin.com/in/your-name" {...register("linkedinUrl")} className={cn(inputBase, borderFor(!!errors.linkedinUrl))} />
            {errors.linkedinUrl && <p className={errorText}>{errors.linkedinUrl.message}</p>}
          </div>
          <div>
            <label htmlFor="source" className={labelBase}>How did you hear about this role?</label>
            <select id="source" {...register("source")} className={cn(inputBase, borderFor(!!errors.source))}>
              <option value="">Select an option…</option>
              {HOW_HEARD.map((h) => (
                <option key={h} value={h}>{h}</option>
              ))}
            </select>
            {errors.source && <p className={errorText}>{errors.source.message}</p>}
          </div>
        </div>
      )}

      {/* ---- STEP 3: Review & Submit ---- */}
      {step === 3 && (
        <div className="space-y-3">
          <p className="text-sm text-zinc-500 dark:text-zinc-400">
            Please review your application before submitting.
          </p>
          <dl className="divide-y divide-zinc-100 rounded-lg border border-zinc-200 dark:divide-zinc-800 dark:border-zinc-800">
            {[
              ["Full name", shown(values.fullName)],
              ["Email", shown(values.email)],
              ["Phone", shown(values.phone)],
              ["Cover letter", shown(values.coverLetter)],
              ["LinkedIn", shown(values.linkedinUrl)],
              ["Heard via", shown(values.source)],
            ].map(([label, value]) => (
              <div key={label} className="flex gap-4 px-4 py-2.5 text-sm">
                <dt className="w-32 shrink-0 font-medium text-zinc-500 dark:text-zinc-400">{label}</dt>
                <dd className="text-zinc-800 dark:text-zinc-200 break-words">{value}</dd>
              </div>
            ))}
          </dl>
        </div>
      )}

      {/* ---- Footer controls ---- */}
      <div className="mt-6 flex items-center justify-between gap-3">
        <div className="flex items-center gap-2">
          {step > 1 && (
            <button
              type="button"
              onClick={handleBack}
              className="rounded-lg border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-700 transition-colors hover:bg-zinc-100 dark:border-zinc-700 dark:text-zinc-200 dark:hover:bg-zinc-800"
            >
              Back
            </button>
          )}

          {/* Discard draft — only when a draft exists. */}
          {hasDraft && (
            <AlertDialog>
              <AlertDialogTrigger asChild>
                <button
                  type="button"
                  className="rounded-lg border border-zinc-300 px-4 py-2 text-sm font-medium text-zinc-500 transition-colors hover:border-red-400 hover:text-red-600 dark:border-zinc-700 dark:text-zinc-400 dark:hover:border-red-500 dark:hover:text-red-400"
                >
                  Discard draft
                </button>
              </AlertDialogTrigger>
              <AlertDialogContent>
                <AlertDialogHeader>
                  <AlertDialogTitle>Discard your draft?</AlertDialogTitle>
                  <AlertDialogDescription>
                    Your saved application progress will be permanently deleted. This cannot be undone.
                  </AlertDialogDescription>
                </AlertDialogHeader>
                <AlertDialogFooter>
                  <AlertDialogCancel>Keep draft</AlertDialogCancel>
                  <AlertDialogAction onClick={handleDiscard}>Discard draft</AlertDialogAction>
                </AlertDialogFooter>
              </AlertDialogContent>
            </AlertDialog>
          )}
        </div>

        <div>
          {step < 3 ? (
            <button
              type="button"
              onClick={handleNext}
              className="rounded-lg bg-lime-400 px-5 py-2 text-sm font-bold text-black transition-colors hover:bg-lime-300"
            >
              Next
            </button>
          ) : (
            <button
              type="button"
              onClick={handleSubmitFinal}
              disabled={mutation.isPending}
              className={cn(
                "rounded-lg px-5 py-2 text-sm font-bold transition-colors",
                mutation.isPending
                  ? "cursor-not-allowed bg-zinc-200 text-zinc-400 dark:bg-zinc-800 dark:text-zinc-600"
                  : "bg-lime-400 text-black hover:bg-lime-300"
              )}
            >
              {mutation.isPending ? "Submitting…" : "Submit application"}
            </button>
          )}
        </div>
      </div>
    </div>
  );
}

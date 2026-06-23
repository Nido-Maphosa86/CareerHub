// src/components/ApplicationForm.tsx
// Job application form. Zod validates in the browser; on valid input
// useMutation POSTs to the real backend at /applications/{listingId} with the
// applicant's Bearer token. The UI reflects loading / server-error / success.

"use client";

import { useForm } from "react-hook-form";
import { zodResolver } from "@hookform/resolvers/zod";
import { useMutation, useQueryClient } from "@tanstack/react-query";
import { z } from "zod";
import { submitApplication } from "@/lib/api";
import { ApplicationRequest } from "@/types";
import { useAuth } from "@/lib/auth";
import { cn } from "@/lib/utils";
import { CheckCircle2, AlertCircle } from "lucide-react";

//rules defines what the application should look like
// ---- Schema -----------------------------------------------------------

//This defines the Zod schema — the rules for what valid input looks like.
const applicationSchema = z
  .object({
    fullName: z
      .string()
      .min(2, "Full name must be at least 2 characters")
      .max(100, "Full name must be at most 100 characters"),

    email: z.string().email("Enter a valid email address"),

    phone: z
      .string()
      .regex(/^\+?[\d\s\-()\d]{8,15}$/, "Enter a valid phone number")
      .or(z.literal(""))
      .optional(),

    yearsOfExperience: z
      .coerce.number()
      .int("Years of experience must be a whole number")
      .min(0, "Cannot be negative")
      .max(50, "That seems too high"),

    coverLetter: z
      .string()
      .min(50, "Cover letter must be at least 50 characters — tell us why you're a strong fit")
      .max(2000, "Cover letter must be at most 2000 characters"),

    linkedInUrl: z
      .string()
      .url("Enter a valid URL")
      .includes("linkedin.com", { message: "URL must be a LinkedIn profile" })
      .or(z.literal(""))
      .optional(),

    availableImmediately: z.boolean(),

    noticePeriodWeeks: z
      .coerce.number()
      .int("Notice period must be a whole number")
      .min(0, "Cannot be negative"),
  })
  .refine(
    (data) => data.availableImmediately || data.noticePeriodWeeks > 0,
    {
      message: "Notice period must be greater than 0 if not available immediately",
      path: ["noticePeriodWeeks"],
    }
  );
   //refine() adds a custom rule across multiple fields.
// Rule: If availableImmediately is false, noticePeriodWeeks must be > 0.
// Error message is attached to noticePeriodWeeks field.
// This matches Assignment 1.4’s cross-field requirement.


type ApplicationFormData = z.infer<typeof applicationSchema>;
// 👉 ApplicationFormData is automatically generated from the schema.
// 👉 Ensures the form data matches the schema exactly.

type ApplicationFormInput = z.input<typeof applicationSchema>;
// 👉 ApplicationFormInput is the raw input type before Zod transforms it.
// 👉 Example: yearsOfExperience comes in as a string, but Zod coerces it to a number.



interface Props {
  listingId: string; // The job ID we’re applying to.
  jobTitle: string;  // The job title (used in success message).
}


export function ApplicationForm({ listingId, jobTitle }: Props) {
  const queryClient = useQueryClient();
  const { token } = useAuth();

  const {
    register,
    handleSubmit,
    reset,
    formState: { errors, isSubmitting },
  } = useForm<ApplicationFormInput, unknown, ApplicationFormData>({
    resolver: zodResolver(applicationSchema),
    defaultValues: {
      fullName: "",
      email: "",
      phone: "",
      yearsOfExperience: 0,
      coverLetter: "",
      linkedInUrl: "",
      availableImmediately: true,
      noticePeriodWeeks: 0,
    },
  });

  const mutation = useMutation({
    // The mutation function carries the listingId and token alongside the body.
    mutationFn: (payload: ApplicationRequest) => {
      if (!token) throw new Error("You must be logged in to apply.");
      return submitApplication(listingId, payload, token);
    },
    onSuccess: () => {
      // Refresh the jobs list so the applicant count updates.
      queryClient.invalidateQueries({ queryKey: ["jobs"] });
      reset();
    },
  });

  const isBusy = isSubmitting || mutation.isPending;

  async function onValid(data: ApplicationFormData) {
    const payload: ApplicationRequest = {
      fullName: data.fullName,
      email: data.email,
      phone: data.phone ? data.phone : undefined,
      yearsOfExperience: data.yearsOfExperience,
      coverLetter: data.coverLetter,
      linkedInUrl: data.linkedInUrl ? data.linkedInUrl : undefined,
      availableImmediately: data.availableImmediately,
      noticePeriodWeeks: data.noticePeriodWeeks,
    };
    await mutation.mutateAsync(payload);
  }

  const inputBase =
    "w-full rounded-lg border bg-white px-3 py-2 text-sm text-zinc-900 placeholder-zinc-400 transition-colors focus:outline-none focus:ring-2 focus:ring-lime-400 dark:bg-zinc-900 dark:text-zinc-100 dark:placeholder-zinc-500";
  const labelBase =
    "mb-1 block text-sm font-medium text-zinc-700 dark:text-zinc-300";
  const errorText = "mt-1 text-xs text-red-600 dark:text-red-400";

  function borderFor(hasError: boolean) {
    return hasError
      ? "border-red-400 dark:border-red-500"
      : "border-zinc-300 dark:border-zinc-700";
  }

  // ---- Success state --------------------------------------------------
  if (mutation.isSuccess) {
    return (
      <div className="rounded-xl border border-lime-300 bg-lime-50 p-6 dark:border-lime-400/30 dark:bg-lime-400/10">
        <div className="flex items-start gap-3">
          <CheckCircle2 className="mt-0.5 h-6 w-6 shrink-0 text-lime-600 dark:text-lime-400" />
          <div>
            <h3 className="text-lg font-bold text-lime-800 dark:text-lime-200">
              Application submitted
            </h3>
            <p className="mt-1 text-sm text-lime-700 dark:text-lime-300">
              Thanks — your application for{" "}
              <span className="font-semibold">{jobTitle}</span> has been received.
            </p>
            <button
              type="button"
              onClick={() => mutation.reset()}
              className="mt-4 rounded-lg border border-lime-400 px-3 py-1.5 text-sm font-medium text-lime-700 transition-colors hover:bg-lime-100 dark:text-lime-300 dark:hover:bg-lime-400/10"
            >
              Apply to another role
            </button>
          </div>
        </div>
      </div>
    );
  }

  return (
    <form
      onSubmit={handleSubmit(onValid)}
      noValidate
      className="space-y-4 rounded-xl border border-zinc-200 bg-white p-6 dark:border-zinc-800 dark:bg-zinc-950"
    >
      <div>
        <h3 className="text-lg font-bold text-zinc-900 dark:text-zinc-50">
          Apply for this role
        </h3>
        <p className="text-sm text-zinc-500 dark:text-zinc-400">{jobTitle}</p>
      </div>

      {mutation.isError && (
        <div className="flex items-start gap-2 rounded-lg border border-red-300 bg-red-50 p-3 text-sm text-red-800 dark:border-red-900 dark:bg-red-950/50 dark:text-red-200">
          <AlertCircle className="mt-0.5 h-4 w-4 shrink-0" />
          <span>{mutation.error.message}</span>
        </div>
      )}

      <div>
        <label htmlFor="fullName" className={labelBase}>Full name</label>
        <input
          id="fullName"
          type="text"
          {...register("fullName")}
          aria-invalid={!!errors.fullName}
          className={cn(inputBase, borderFor(!!errors.fullName))}
        />
        {errors.fullName && <p className={errorText}>{errors.fullName.message}</p>}
      </div>

      <div>
        <label htmlFor="email" className={labelBase}>Email</label>
        <input
          id="email"
          type="email"
          {...register("email")}
          aria-invalid={!!errors.email}
          className={cn(inputBase, borderFor(!!errors.email))}
        />
        {errors.email && <p className={errorText}>{errors.email.message}</p>}
      </div>

      <div>
        <label htmlFor="phone" className={labelBase}>
          Phone <span className="text-zinc-400">(optional)</span>
        </label>
        <input
          id="phone"
          type="tel"
          {...register("phone")}
          aria-invalid={!!errors.phone}
          className={cn(inputBase, borderFor(!!errors.phone))}
        />
        {errors.phone && <p className={errorText}>{errors.phone.message}</p>}
      </div>

      <div>
        <label htmlFor="yearsOfExperience" className={labelBase}>
          Years of experience
        </label>
        <input
          id="yearsOfExperience"
          type="number"
          {...register("yearsOfExperience")}
          aria-invalid={!!errors.yearsOfExperience}
          className={cn(inputBase, borderFor(!!errors.yearsOfExperience))}
        />
        {errors.yearsOfExperience && (
          <p className={errorText}>{errors.yearsOfExperience.message}</p>
        )}
      </div>

      <div>
        <label htmlFor="coverLetter" className={labelBase}>Cover letter</label>
        <textarea
          id="coverLetter"
          rows={5}
          {...register("coverLetter")}
          aria-invalid={!!errors.coverLetter}
          className={cn(inputBase, borderFor(!!errors.coverLetter), "resize-y")}
        />
        {errors.coverLetter && (
          <p className={errorText}>{errors.coverLetter.message}</p>
        )}
      </div>

      <div>
        <label htmlFor="linkedInUrl" className={labelBase}>
          LinkedIn URL <span className="text-zinc-400">(optional)</span>
        </label>
        <input
          id="linkedInUrl"
          type="url"
          {...register("linkedInUrl")}
          aria-invalid={!!errors.linkedInUrl}
          className={cn(inputBase, borderFor(!!errors.linkedInUrl))}
        />
        {errors.linkedInUrl && (
          <p className={errorText}>{errors.linkedInUrl.message}</p>
        )}
      </div>

      <div className="flex items-center gap-2">
        <input
          id="availableImmediately"
          type="checkbox"
          {...register("availableImmediately")}
          className="h-4 w-4 rounded border-zinc-300 text-lime-500 focus:ring-lime-400 dark:border-zinc-600"
        />
        <label
          htmlFor="availableImmediately"
          className="text-sm font-medium text-zinc-700 dark:text-zinc-300"
        >
          Available immediately
        </label>
      </div>

      <div>
        <label htmlFor="noticePeriodWeeks" className={labelBase}>
          Notice period (weeks)
        </label>
        <input
          id="noticePeriodWeeks"
          type="number"
          {...register("noticePeriodWeeks")}
          aria-invalid={!!errors.noticePeriodWeeks}
          className={cn(inputBase, borderFor(!!errors.noticePeriodWeeks))}
        />
        {errors.noticePeriodWeeks && (
          <p className={errorText}>{errors.noticePeriodWeeks.message}</p>
        )}
      </div>

      <button
        type="submit"
        disabled={isBusy}
        className={cn(
          "w-full rounded-lg px-4 py-2.5 text-sm font-semibold transition-colors",
          isBusy
            ? "cursor-not-allowed bg-zinc-200 text-zinc-400 dark:bg-zinc-800 dark:text-zinc-600"
            : "bg-lime-400 text-black hover:bg-lime-300"
        )}
      >
        {isBusy ? "Submitting…" : "Submit Application"}
      </button>
    </form>
  );
}

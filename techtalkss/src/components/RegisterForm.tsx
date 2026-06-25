"use client"

import { useForm } from 'react-hook-form'
import { zodResolver } from '@hookform/resolvers/zod'
import { z } from 'zod'
import { useMutation, useQueryClient } from '@tanstack/react-query'
import { createRegistration } from '@/lib/mock-data'

const schema = z.object({
  attendeeName: z.string().min(2, 'Name must be at least 2 characters'),
  attendeeEmail: z.string().email('Please enter a valid email'),
  talkId: z.number(),
})

type FormValues = z.infer<typeof schema>

interface Props {
  talkId: number
}

export function RegisterForm({ talkId }: Props) {
  const queryClient = useQueryClient()

  const {
    register,
    handleSubmit,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({
    resolver: zodResolver(schema),
    defaultValues: { talkId },
  })

  const mutation = useMutation({
    mutationFn: createRegistration,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['talks'] }),
  })

  const onSubmit = (values: FormValues) => {
    mutation.mutate(values)
  }

  return (
    <div className="rounded-2xl border border-[#2a2a35] bg-[#131318] p-6">
      <h2 className="mb-6 text-lg font-bold text-white">Register for this Talk</h2>

      {mutation.isSuccess && (
        <div className="mb-4 rounded-xl border border-violet-500/30 bg-violet-500/10 px-4 py-3 text-sm text-violet-400">
          You are registered! See you there.
        </div>
      )}

      {mutation.isError && (
        <div className="mb-4 rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
          {mutation.error.message}
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="flex flex-col gap-4">
        <input type="hidden" {...register('talkId', { valueAsNumber: true })} />

        <div className="flex flex-col gap-1">
          <label className="text-xs font-semibold uppercase tracking-widest text-[#6b6b80]">
            Full Name
          </label>
          <input
            {...register('attendeeName')}
            placeholder="John Doe"
            className="rounded-lg border border-[#2a2a35] bg-[#0c0c0f] px-4 py-2.5 text-sm text-white placeholder-[#6b6b80] outline-none focus:border-violet-500 transition-colors"
          />
          {errors.attendeeName && (
            <span className="text-xs text-red-400">{errors.attendeeName.message}</span>
          )}
        </div>

        <div className="flex flex-col gap-1">
          <label className="text-xs font-semibold uppercase tracking-widest text-[#6b6b80]">
            Email Address
          </label>
          <input
            {...register('attendeeEmail')}
            placeholder="john@example.com"
            className="rounded-lg border border-[#2a2a35] bg-[#0c0c0f] px-4 py-2.5 text-sm text-white placeholder-[#6b6b80] outline-none focus:border-violet-500 transition-colors"
          />
          {errors.attendeeEmail && (
            <span className="text-xs text-red-400">{errors.attendeeEmail.message}</span>
          )}
        </div>

        <button
          type="submit"
          disabled={isSubmitting || mutation.isPending}
          className="mt-2 rounded-xl bg-violet-600 px-4 py-2.5 text-sm font-bold text-white transition-all hover:bg-violet-500 disabled:cursor-not-allowed disabled:opacity-50"
        >
          {isSubmitting || mutation.isPending ? 'Registering…' : 'Register Now'}
        </button>
      </form>
    </div>
  )
}
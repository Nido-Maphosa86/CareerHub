import { fetchTalkById } from '@/lib/mock-data'
import { notFound } from 'next/navigation'
import { RegisterForm } from '@/components/RegisterForm'
import { TopicBadge } from '@/components/TopicBadge'

export default async function TalkPage({
  params,
}: {
  params: Promise<{ id: string }>
}) {
  const { id } = await params
  const talk = await fetchTalkById(Number(id))

  if (!talk) notFound()

  const date = new Date(talk.scheduledAt).toLocaleDateString('en-ZA', {
    weekday: 'long',
    year: 'numeric',
    month: 'long',
    day: 'numeric',
  })

  const time = new Date(talk.scheduledAt).toLocaleTimeString('en-ZA', {
    hour: '2-digit',
    minute: '2-digit',
  })

  const percentage = Math.round((talk.registrationCount / talk.capacity) * 100)

  return (
    <main className="min-h-screen bg-[#0c0c0f] px-4 py-12">
      <div className="mx-auto max-w-3xl flex flex-col gap-6">

        <div className="relative rounded-2xl border border-[#2a2a35] bg-[#131318] p-6">
          <div className="absolute top-0 left-0 h-[2px] w-full rounded-t-2xl bg-gradient-to-r from-violet-600 via-violet-400 to-transparent" />

          <div className="mb-4 flex items-start justify-between gap-4">
            <h1 className="text-2xl font-black text-white leading-snug">
              {talk.title}
            </h1>
            <TopicBadge topic={talk.topic} />
          </div>

          <p className="mb-6 text-sm font-semibold text-violet-400">
            {talk.speaker}
          </p>

          <p className="mb-6 text-sm leading-relaxed text-[#9090a8]">
            {talk.description}
          </p>

          <div className="grid grid-cols-2 gap-3 text-sm text-[#6b6b80] mb-6">
            <span className="flex items-center gap-2">
              <span className="text-violet-500">📍</span> {talk.location}
            </span>
            <span className="flex items-center gap-2">
              <span className="text-violet-500">⏱</span> {talk.duration} min
            </span>
            <span className="col-span-2 flex items-center gap-2">
              <span className="text-violet-500">🗓</span> {date} at {time}
            </span>
          </div>

          <div className="flex flex-col gap-1">
            <div className="flex justify-between text-xs text-[#6b6b80]">
              <span>Capacity</span>
              <span className={percentage >= 90 ? 'text-red-400' : 'text-violet-400'}>
                {talk.registrationCount} / {talk.capacity}
              </span>
            </div>
            <div className="h-1.5 w-full overflow-hidden rounded-full bg-[#2a2a35]">
              <div
                className={`h-full rounded-full transition-all ${
                  percentage >= 90
                    ? 'bg-red-500'
                    : 'bg-gradient-to-r from-violet-600 to-violet-400'
                }`}
                style={{ width: `${percentage}%` }}
              />
            </div>
          </div>
        </div>

        <RegisterForm talkId={talk.id} />

      </div>
    </main>
  )
}
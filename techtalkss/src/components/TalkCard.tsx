import { Talk } from '@/types'
import { TopicBadge } from '@/components/TopicBadge'

interface Props {
  talk: Talk
}

export function TalkCard({ talk }: Props) {
  const date = new Date(talk.scheduledAt).toLocaleDateString('en-ZA', {
    weekday: 'short',
    year: 'numeric',
    month: 'short',
    day: 'numeric',
  })

  const time = new Date(talk.scheduledAt).toLocaleTimeString('en-ZA', {
    hour: '2-digit',
    minute: '2-digit',
  })

  const percentage = Math.round((talk.registrationCount / talk.capacity) * 100)

  return (
    <div className="relative flex flex-col gap-4 rounded-2xl border border-[#2a2a35] bg-[#131318] p-5 transition-all duration-300 hover:border-violet-500 hover:shadow-lg hover:shadow-violet-500/10">
      
      <div className="absolute top-0 left-0 h-[2px] w-full rounded-t-2xl bg-gradient-to-r from-violet-600 via-violet-400 to-transparent" />

      <div className="flex items-start justify-between gap-2">
        <h3 className="text-base font-bold leading-snug text-white">
          {talk.title}
        </h3>
        <TopicBadge topic={talk.topic} />
      </div>

      <p className="text-sm font-medium text-violet-400">{talk.speaker}</p>

      <p className="text-sm leading-relaxed text-[#9090a8]">{talk.description}</p>

      <div className="grid grid-cols-2 gap-2 text-xs text-[#6b6b80]">
        <span className="flex items-center gap-1">
          <span className="text-violet-500">📍</span> {talk.location}
        </span>
        <span className="flex items-center gap-1">
          <span className="text-violet-500">⏱</span> {talk.duration} min
        </span>
        <span className="col-span-2 flex items-center gap-1">
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
  )
}
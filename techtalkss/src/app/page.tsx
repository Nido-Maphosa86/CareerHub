"use client"

import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { TalkCard } from '@/components/TalkCard'
import { fetchTalks } from '@/lib/mock-data'
import { TalkTopic } from '@/types'
import { Skeleton } from '@/components/ui/skeleton'

const topics: TalkTopic[] = ['Frontend', 'Backend', 'DevOps', 'AI/ML', 'Mobile']

export default function Home() {
  const [selected, setSelected] = useState<TalkTopic | 'All'>('All')

  const { data, isPending, isError, error } = useQuery({
    queryKey: ['talks'],
    queryFn: fetchTalks,
  })

  const filtered = selected === 'All'
    ? (data ?? [])
    : (data ?? []).filter(t => t.topic === selected)

  return (
    <main className="min-h-screen bg-[#0c0c0f] px-4 py-12">
      <div className="mx-auto max-w-6xl">

        <div className="mb-10 text-center">
          <h1 className="text-4xl font-black tracking-tight text-white sm:text-5xl">
            Tech<span className="text-violet-400">Talks</span>
          </h1>
          <p className="mt-2 text-sm text-[#6b6b80]">
            Browse and register for upcoming tech sessions
          </p>
        </div>

        <div className="mb-8 flex flex-wrap justify-center gap-2">
          <button
            onClick={() => setSelected('All')}
            className={`rounded-full px-4 py-1.5 text-sm font-semibold transition-all ${
              selected === 'All'
                ? 'bg-violet-600 text-white'
                : 'border border-[#2a2a35] text-[#6b6b80] hover:border-violet-500 hover:text-violet-400'
            }`}
          >
            All
          </button>
          {topics.map(topic => (
            <button
              key={topic}
              onClick={() => setSelected(topic)}
              className={`rounded-full px-4 py-1.5 text-sm font-semibold transition-all ${
                selected === topic
                  ? 'bg-violet-600 text-white'
                  : 'border border-[#2a2a35] text-[#6b6b80] hover:border-violet-500 hover:text-violet-400'
              }`}
            >
              {topic}
            </button>
          ))}
        </div>

        {isError && (
          <div className="mb-6 rounded-xl border border-red-500/30 bg-red-500/10 px-4 py-3 text-sm text-red-400">
            {error.message}
          </div>
        )}

        {isPending ? (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {Array.from({ length: 3 }).map((_, i) => (
              <Skeleton key={i} className="h-64 w-full rounded-2xl bg-[#131318]" />
            ))}
          </div>
        ) : (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {filtered.map(talk => (
              <TalkCard key={talk.id} talk={talk} />
            ))}
          </div>
        )}

      </div>
    </main>
  )
}
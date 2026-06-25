import { fetchTalks } from '@/lib/mock-data'
import { TalkCard } from '@/components/TalkCard'
import Link from 'next/link'

export default async function TalksPage() {
  const talks = await fetchTalks()

  return (
    <main className="min-h-screen bg-[#0c0c0f] px-4 py-12">
      <div className="mx-auto max-w-6xl">

        <div className="mb-10">
          <h1 className="text-4xl font-black tracking-tight text-white">
            All <span className="text-violet-400">Talks</span>
          </h1>
          <p className="mt-2 text-sm text-[#6b6b80]">
            {talks.length} sessions scheduled — click a card to register
          </p>
        </div>

        <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
          {talks.map(talk => (
            <Link key={talk.id} href={`/talks/${talk.id}`}>
              <TalkCard talk={talk} />
            </Link>
          ))}
        </div>

      </div>
    </main>
  )
}
import Link from 'next/link'

export default function NotFound() {
  return (
    <main className="min-h-screen bg-[#0c0c0f] flex items-center justify-center px-4">
      <div className="text-center flex flex-col items-center gap-6">

        <div className="text-8xl font-black text-violet-500 opacity-20">404</div>

        <div>
          <h1 className="text-2xl font-black text-white">Talk Not Found</h1>
          <p className="mt-2 text-sm text-[#6b6b80]">
            That session does not exist or may have been removed.
          </p>
        </div>

        <Link
          href="/talks"
          className="rounded-xl bg-violet-600 px-6 py-2.5 text-sm font-bold text-white transition-all hover:bg-violet-500"
        >
          Back to Talks
        </Link>

      </div>
    </main>
  )
}
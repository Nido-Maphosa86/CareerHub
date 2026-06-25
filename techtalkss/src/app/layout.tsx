import type { Metadata } from "next"
import "./globals.css"
import { Providers } from "./providers"
import Link from "next/link"

export const metadata: Metadata = {
  title: "TechTalks",
  description: "Where the sharpest minds in tech share their knowledge.",
}

export default function RootLayout({
  children,
}: {
  children: React.ReactNode
}) {
  return (
    <html lang="en" className="dark">
      <body>
        <nav className="sticky top-0 z-50 border-b border-[#2a2a35] bg-[#0c0c0f]/80 backdrop-blur-md">
          <div className="mx-auto flex max-w-6xl items-center justify-between px-4 py-3">
            <Link href="/" className="text-lg font-black text-white">
              Tech<span className="text-violet-400">Talks</span>
            </Link>
            <div className="flex items-center gap-6">
              <Link
                href="/"
                className="text-sm font-medium text-[#6b6b80] transition-colors hover:text-violet-400"
              >
                Home
              </Link>
              <Link
                href="/talks"
                className="text-sm font-medium text-[#6b6b80] transition-colors hover:text-violet-400"
              >
                Talks
              </Link>
            </div>
          </div>
        </nav>
        <Providers>{children}</Providers>
      </body>
    </html>
  )
}
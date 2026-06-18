// src/app/layout.tsx

import type { Metadata } from "next";
import { Geist } from "next/font/google";
import "./globals.css";
import { ThemeToggle } from "@/src/components/ThemeToggle";


const geist = Geist({ subsets: ["latin"] });

export const metadata: Metadata = {
  title: "CareerHub",
  description: "Find your next opportunity",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className={geist.className}>
        {/* Header — light and dark mode classes both applied */}
        <header
          className="sticky top-0 z-10 border-b px-8 py-4 flex items-center justify-between
            bg-white border-gray-200
            dark:bg-gray-900 dark:border-gray-700"
        >
          <div>
            <span className="text-lg font-bold text-gray-900 dark:text-gray-50">
              CareerHub
            </span>
            <span className="ml-2 text-sm text-gray-400 dark:text-gray-500">
              Find your next opportunity
            </span>
          </div>

          {/* ThemeToggle is a Client Component — reads localStorage on mount */}
          <ThemeToggle />
        </header>

        {children}
      </body>
    </html>
  );
}

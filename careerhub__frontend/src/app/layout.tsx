// src/app/layout.tsx
// Root layout. Stays a Server Component — only the <Providers> wrapper
// inside it is a Client Component.

import type { Metadata } from "next";
import "./globals.css";
import { Providers } from "./providers";

export const metadata: Metadata = {
  title: "CareerHub",
  description: "Find your next role.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  return (
    <html lang="en" suppressHydrationWarning>
      <body className="min-h-screen antialiased">
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}
//Wraps all pages with providers
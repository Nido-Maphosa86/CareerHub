// src/app/layout.tsx
// Root layout. Stays a Server Component — only <Providers> is a Client Component.

import type { Metadata } from "next";
import "./globals.css";
import { Providers } from "./providers";

export const metadata: Metadata = {
  title: "CareerHub — Find your next role",
  description: "Browse open positions on CareerHub.",
};

export default function RootLayout({
  children,
}: {
  children: React.ReactNode;
}) {
  // className="dark" makes dark the default before JS runs, avoiding a flash
  // of light theme. ThemeToggle can switch it off and persist the choice.
  return (
    <html lang="en" className="dark" suppressHydrationWarning>
      <body className="min-h-screen antialiased">
        <Providers>{children}</Providers>
      </body>
    </html>
  );
}

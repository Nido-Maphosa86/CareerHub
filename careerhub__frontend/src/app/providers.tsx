// src/app/providers.tsx
// Client-side providers: Auth.js session + nuqs URL-state adapter + TanStack
// Query cache + the existing backend auth context.

"use client";

import { useState, ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools";
import { SessionProvider } from "next-auth/react";
import { NuqsAdapter } from "nuqs/adapters/next/app";
import { AuthProvider } from "@/lib/auth";

export function Providers({ children }: { children: ReactNode }) {
  const [client] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 60 * 1000,
            refetchOnWindowFocus: false,
          },
        },
      })
  );

  return (
    // SessionProvider makes useSession() available to Client Components.
    <SessionProvider>
      {/* NuqsAdapter lets useQueryStates read/write the URL on the App Router. */}
      <NuqsAdapter>
        <QueryClientProvider client={client}>
          <AuthProvider>{children}</AuthProvider>
          <ReactQueryDevtools initialIsOpen={false} />
        </QueryClientProvider>
      </NuqsAdapter>
    </SessionProvider>
  );
}

// src/app/providers.tsx
// Client-side wrapper that sets up TanStack Query.
// Created via useState so each browser session gets its own QueryClient.
// This avoids one user's cache leaking into another user's session.

"use client"; 
// Tells Next.js this file runs in the browser (client-side).

import { useState, ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { ReactQueryDevtools } from "@tanstack/react-query-devtools"; 


export function Providers({ children }: { children: ReactNode }) { 
// Define a component called Providers.
// It accepts children (any React elements) and wraps them with QueryClientProvider.

  // useState with an initializer runs only once per component instance.
  // This is the documented pattern from TanStack Query for the App Router.
  // We create a QueryClient instance here so it’s shared across the app, but not shared between different users in the same browser.
  //builds a data manager that knows how long to keep data fresh and when to refetch.
  const [client] = useState(
    () =>
      new QueryClient({
        defaultOptions: {
          queries: {
            staleTime: 60 * 1000, // 1 minute — avoid refetch on every render
            refetchOnWindowFocus: false,
          },
        },
      })
  );
  // Here we create a QueryClient instance inside useState.
  // useState ensures each browser session gets its own client (no shared cache).
  // defaultOptions:
  // - staleTime: data is considered "fresh" for 1 minute, so it won’t refetch too often.
  // - refetchOnWindowFocus: false means data won’t reload just because you clicked back into the tab.

  return (
    <QueryClientProvider client={client}>
      {children}
      <ReactQueryDevtools initialIsOpen={false} />
    </QueryClientProvider>
  );
  // Wrap the whole app (children) with QueryClientProvider so queries work everywhere.
  // Also include ReactQueryDevtools (debugging panel), but keep it closed by default.
}

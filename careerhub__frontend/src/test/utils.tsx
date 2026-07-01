// src/test/utils.tsx
// Assignment 3.2 — Part 2: shared test helpers.
//
// renderWithProviders wraps a component in everything it needs to run in a test:
// the React Query cache (with retries off so failures surface immediately) and a
// faked Auth.js session. useSession is mocked at the module level so ANY
// component that calls it receives the session we pass in; the default is an
// authenticated Candidate. useAuth (the app's backend-token context) is also
// mocked so the wizard has a token to submit with and does not need a real
// AuthProvider/localStorage token in tests.

import { vi } from "vitest";
import { ReactElement, ReactNode } from "react";
import { render } from "@testing-library/react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { useSession } from "next-auth/react";

// Mock next-auth/react so useSession returns whatever we set per render.
vi.mock("next-auth/react", () => ({
  useSession: vi.fn(),
  SessionProvider: ({ children }: { children: ReactNode }) => children,
}));

// Mock the app's own auth context so useAuth() returns a token without needing a
// real AuthProvider. The token value is irrelevant — MSW does not validate it.
vi.mock("@/lib/auth", () => ({
  useAuth: () => ({
    token: "test-token",
    user: { username: "alice", role: "candidate" },
    isAuthenticated: true,
    isApplicant: true,
    login: vi.fn(),
    logout: vi.fn(),
  }),
  AuthProvider: ({ children }: { children: ReactNode }) => children,
}));

// A default signed-in Candidate session.
const candidateSession = {
  user: { name: "Alice Candidate", role: "candidate" },
  expires: "2099-01-01T00:00:00.000Z",
};

interface Options {
  // null = signed out. Omit = default Candidate session.
  session?: typeof candidateSession | null;
}

export function renderWithProviders(
  ui: ReactElement,
  { session = candidateSession }: Options = {}
) {
  // Point the mocked useSession at the requested session for this render.
  vi.mocked(useSession).mockReturnValue({
    data: session,
    status: session ? "authenticated" : "unauthenticated",
    update: vi.fn(),
    // eslint-disable-next-line @typescript-eslint/no-explicit-any
  } as any);

  const queryClient = new QueryClient({
    defaultOptions: {
      queries: { retry: false },
      mutations: { retry: false },
    },
  });

  function Wrapper({ children }: { children: ReactNode }) {
    return <QueryClientProvider client={queryClient}>{children}</QueryClientProvider>;
  }

  return render(ui, { wrapper: Wrapper });
}

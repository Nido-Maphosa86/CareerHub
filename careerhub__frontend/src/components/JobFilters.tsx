// src/components/JobFilters.tsx
// Assignment 2.3 — Part 6: the job filters (a Client Component).
//
// All three filters live in the URL via nuqs useQueryStates, so a filtered view
// is shareable, survives refresh, and works with browser back/forward. The two
// text inputs are debounced: we keep a local useState for what the user is
// typing and only push it into the URL 300ms after they stop, so we don't
// trigger a navigation on every keystroke. The status toggle updates instantly.

"use client";

import { useState, useEffect, useRef } from "react";
import { useQueryStates } from "nuqs";
import { parseAsString, parseAsStringEnum } from "nuqs";
import { Search, MapPin } from "lucide-react";

export function JobFilters() {
  // The single source of truth for filters: the URL.
  const [filters, setFilters] = useQueryStates(
    {
      q: parseAsString.withDefault(""),
      location: parseAsString.withDefault(""),
      status: parseAsStringEnum(["open", "all"]).withDefault("all"),
    },
    // Replace history entries so typing doesn't stack dozens of back steps.
    { history: "replace", shallow: false }
  );

  // Local state for the debounced text inputs, seeded from the URL.
  const [q, setQ] = useState(filters.q);
  const [location, setLocation] = useState(filters.location);

  // Debounce: push q -> URL 300ms after the last keystroke.
  const qTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  useEffect(() => {
    if (qTimer.current) clearTimeout(qTimer.current);
    qTimer.current = setTimeout(() => {
      setFilters({ q: q || null });
    }, 300);
    return () => {
      if (qTimer.current) clearTimeout(qTimer.current);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [q]);

  // Debounce: push location -> URL 300ms after the last keystroke.
  const locTimer = useRef<ReturnType<typeof setTimeout> | null>(null);
  useEffect(() => {
    if (locTimer.current) clearTimeout(locTimer.current);
    locTimer.current = setTimeout(() => {
      setFilters({ location: location || null });
    }, 300);
    return () => {
      if (locTimer.current) clearTimeout(locTimer.current);
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location]);

  return (
    <div className="mb-6 flex flex-col gap-3 rounded-xl border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-950 sm:flex-row sm:items-center">
      {/* Keyword search (debounced). */}
      <div className="relative flex-1">
        <Search className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-400" />
        <input
          value={q}
          onChange={(e) => setQ(e.target.value)}
          placeholder="Search by title or company…"
          className="w-full rounded-lg border border-zinc-300 bg-white py-2 pl-9 pr-3 text-sm text-zinc-900 outline-none transition-colors focus:border-lime-500 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-100"
        />
      </div>

      {/* Location (debounced). A free-text input is used rather than a select
          because the mock backend has an open-ended set of locations; a select
          would need a hardcoded list that could drift from the real data. */}
      <div className="relative flex-1">
        <MapPin className="absolute left-3 top-1/2 h-4 w-4 -translate-y-1/2 text-zinc-400" />
        <input
          value={location}
          onChange={(e) => setLocation(e.target.value)}
          placeholder="Location…"
          className="w-full rounded-lg border border-zinc-300 bg-white py-2 pl-9 pr-3 text-sm text-zinc-900 outline-none transition-colors focus:border-lime-500 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-100"
        />
      </div>

      {/* Status toggle (instant — no debounce). */}
      <div className="flex shrink-0 rounded-lg border border-zinc-300 p-0.5 dark:border-zinc-700">
        {(["all", "open"] as const).map((value) => (
          <button
            key={value}
            type="button"
            onClick={() => setFilters({ status: value })}
            className={`rounded-md px-3 py-1.5 text-sm font-semibold capitalize transition-colors ${
              filters.status === value
                ? "bg-lime-400 text-black"
                : "text-zinc-500 hover:text-zinc-800 dark:text-zinc-400 dark:hover:text-zinc-100"
            }`}
          >
            {value}
          </button>
        ))}
      </div>
    </div>
  );
}

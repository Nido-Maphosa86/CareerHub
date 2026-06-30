// src/components/ClearFiltersButton.tsx
// Assignment 3.1 — Part 5: resets every URL filter param.
//
// A Client Component because it writes to the URL via nuqs. Setting each param
// to null removes it from the query string, which returns /jobs to its full,
// unfiltered list. Used by the "filters returned nothing" empty state.

"use client";

import { useQueryStates, parseAsString, parseAsStringEnum } from "nuqs";

export function ClearFiltersButton() {
  const [, setFilters] = useQueryStates({
    q: parseAsString.withDefault(""),
    location: parseAsString.withDefault(""),
    status: parseAsStringEnum(["open", "all"]).withDefault("all"),
  });

  return (
    <button
      type="button"
      onClick={() => setFilters({ q: null, location: null, status: null })}
      className="rounded-lg bg-lime-400 px-4 py-2 text-sm font-bold text-black transition-colors hover:bg-lime-300"
    >
      Clear all filters
    </button>
  );
}

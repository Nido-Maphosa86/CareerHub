// src/components/ThemeToggle.tsx
// Toggles between light and dark mode by adding/removing the "dark" class
// on <html>. Reads localStorage first, then falls back to OS preference.

"use client";

import { useEffect, useState } from "react";
import { Sun, Moon } from "lucide-react";

type Theme = "light" | "dark";

export function ThemeToggle() {
  // Start as null so we can tell "not loaded yet" apart from a real value.
  // This avoids a flash of the wrong icon on first render.
  const [theme, setTheme] = useState<Theme | null>(null);

  // On mount, decide what theme to use.
  useEffect(() => {
    // 1. Check localStorage for a saved choice.
    const saved = localStorage.getItem("theme") as Theme | null;

    // 2. Fall back to the OS preference.
    const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;

    const initial: Theme = saved ?? (prefersDark ? "dark" : "light");
    setTheme(initial);
    applyTheme(initial);
  }, []);

  // Add or remove the "dark" class on the <html> element.
  // Tailwind reads this class to switch dark mode styles on/off.
  function applyTheme(next: Theme) {
    const root = document.documentElement;
    if (next === "dark") root.classList.add("dark");
    else root.classList.remove("dark");
  }

  function toggle() {
    const next: Theme = theme === "dark" ? "light" : "dark";
    setTheme(next);
    applyTheme(next);
    localStorage.setItem("theme", next);
  }

  // Don't render the icon until we know the real theme.
  if (theme === null) {
    return <div className="h-9 w-9" aria-hidden />;
  }

  return (
    <button
      type="button"
      onClick={toggle}
      aria-label="Toggle dark mode"
      className="inline-flex h-9 w-9 items-center justify-center rounded-md border border-slate-200 bg-white text-slate-700 transition-colors hover:bg-slate-100 dark:border-slate-700 dark:bg-slate-900 dark:text-slate-200 dark:hover:bg-slate-800"
    >
      {theme === "dark" ? <Sun className="h-4 w-4" /> : <Moon className="h-4 w-4" />}
    </button>
  );
}

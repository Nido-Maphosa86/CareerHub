// src/components/ThemeToggle.tsx
//
// Manages dark mode by toggling the "dark" class on <html>.
// The real source of truth is the CSS class on <html>.
// React state (isDark) is just a mirror to show the correct button label.
// Even if the component unmounts, the <html> class stays, so dark mode persists.

"use client"; // Must be here so React hooks work in Next.js

// Import React hooks
import { useEffect, useState } from "react";

// ThemeToggle component
export function ThemeToggle() {
  // State: isDark → only used for button label
  const [isDark, setIsDark] = useState(false);

  // On first mount: check localStorage or OS preference
  // Runs once because dependency array is []
  useEffect(() => {
    const stored = localStorage.getItem("careerhub-theme");

    if (stored === "dark") {
      // If user saved "dark", add dark class to <html>
      document.documentElement.classList.add("dark");
      setIsDark(true);
    } else if (stored === "light") {
      // If user saved "light", remove dark class
      document.documentElement.classList.remove("dark");
      setIsDark(false);
    } else {
      // No saved preference → check OS setting
      const prefersDark = window.matchMedia("(prefers-color-scheme: dark)").matches;
      if (prefersDark) {
        document.documentElement.classList.add("dark");
        setIsDark(true);
      }
    }
  }, []);

  // Toggle function → flips dark mode on/off
  function toggle() {
    const next = !isDark; // opposite of current state
    setIsDark(next);

    if (next) {
      // Turn dark mode on
      document.documentElement.classList.add("dark");
      localStorage.setItem("careerhub-theme", "dark");
    } else {
      // Turn dark mode off
      document.documentElement.classList.remove("dark");
      localStorage.setItem("careerhub-theme", "light");
    }
  }

  return (
    // Button that toggles dark mode
    <button
      onClick={toggle}
      // aria-label describes the action (what will happen when clicked)
      aria-label={isDark ? "Switch to light mode" : "Switch to dark mode"}
      // Button styling → cn() not needed here, but dark mode variants added
      className="text-sm px-3 py-1.5 rounded-lg border transition-colors
        border-gray-300 text-gray-600 hover:bg-gray-100
        dark:border-gray-600 dark:text-gray-300 dark:hover:bg-gray-700"
    >
      {/* Button text changes depending on state */}
      {isDark ? "☀ Light" : "☾ Dark"}
    </button>
  );
}

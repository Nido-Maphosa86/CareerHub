// vitest.config.ts
// Assignment 3.2 — Part 2: Vitest configuration.
//
// - @vitejs/plugin-react: lets Vitest compile JSX/TSX the way the app does.
// - environment "jsdom": gives tests a fake browser DOM (document, localStorage).
// - globals: true: makes describe/it/expect available without importing them.
// - setupFiles: runs before every test file (jest-dom matchers + MSW lifecycle).
// - env: sets the API base URL the CareerHub API client reads, so test requests
//   and MSW handlers line up on the same URL.
// - resolve.alias: maps "@/..." to "src/..." exactly like tsconfig/next.

import { defineConfig } from "vitest/config";
import react from "@vitejs/plugin-react";
import path from "path";

export default defineConfig({
  plugins: [react()],
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/test/setup.ts"],
    env: {
      NEXT_PUBLIC_API_URL: "http://localhost:5000/api/v1",
      NEXT_PUBLIC_SITE_URL: "http://localhost:3000",
    },
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
});

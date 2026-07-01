// src/test/setup.ts
// Assignment 3.2 — Part 4: global test setup, run before every test file.
//
// - jest-dom adds matchers like toBeVisible()/toBeInTheDocument().
// - The MSW server starts before all tests, resets handlers after each test (so
//   a per-test override like a forced 500 never leaks into the next test), and
//   closes after all tests.
// - localStorage is cleared after each test so drafts from one test never bleed
//   into another (tests must be independent).

import "@testing-library/jest-dom";
import { server } from "./msw/server";
import { beforeAll, afterEach, afterAll } from "vitest";

beforeAll(() => server.listen({ onUnhandledRequest: "warn" }));

afterEach(() => {
  server.resetHandlers();
  localStorage.clear();
});

afterAll(() => server.close());

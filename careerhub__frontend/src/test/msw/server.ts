// src/test/msw/server.ts
// Assignment 3.2 — Part 4: the MSW server instance for Node (Vitest runs in Node).
// setupServer wires the handlers into a server the setup file starts/stops.

import { setupServer } from "msw/node";
import { handlers } from "./handlers";

export const server = setupServer(...handlers);

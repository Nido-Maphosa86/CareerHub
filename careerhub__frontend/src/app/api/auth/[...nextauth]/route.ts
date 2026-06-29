// src/app/api/auth/[...nextauth]/route.ts
// Assignment 2.3 — Part 2: the Auth.js catch-all route handler.
//
// Auth.js needs a set of endpoints under /api/auth/* (signin, signout, session,
// callback, csrf, etc). This one file wires all of them up by re-exporting the
// GET and POST handlers that src/auth.ts produced. The [...nextauth] folder name
// is a catch-all segment, so every /api/auth/... request lands here.


//Auth.js needs several endpoints to work — things like /api/auth/signin, /api/auth/signout, /api/auth/session, /api/auth/csrf, and /api/auth/callback. 
// Instead of creating a separate file for each one, this single file handles all of them.
//The [...nextauth] folder name is what makes it a catch-all — the three dots mean "match anything after /api/auth/". So every request to any /api/auth/* URL lands in this one file.

import { handlers } from "@/auth";

export const { GET, POST } = handlers;

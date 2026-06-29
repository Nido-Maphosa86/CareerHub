// src/types/next-auth.d.ts
// Assignment 2.3 — Part 2: module augmentation.
//
// Auth.js ships with default types for Session, User, and JWT that do not know
// about our custom `role` field. This file extends those built-in types so that
// session.user.role, user.role, and token.role are all properly typed across
// the app. Without this, TypeScript would error on every reference to role.

import { DefaultSession } from "next-auth";

declare module "next-auth" {
  interface Session {
    user: {
      role: string;
    } & DefaultSession["user"];
  }

  interface User {
    role: string;
  }
}

declare module "next-auth/jwt" {
  interface JWT {
    role: string;
  }
}

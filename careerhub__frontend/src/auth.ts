// src/auth.ts
// Assignment 2.3 — Part 2: Auth.js v5 configuration.
//
// This is the page-level identity system: it answers "who is using the app and
// what role are they" so we can protect routes and gate UI. It is separate from
// the real backend JWT login used by the apply flow — that one proves identity
// to CareerHub.Api; this one proves identity to the Next.js app itself.
//
// There is no backend auth endpoint for this, so authorize() validates against
// a hardcoded array of mock users defined right here. Do not move this array.

//mock users accounts live here, in the auth.ts file. This is a mock implementation for testing purposes only.
import NextAuth from "next-auth";
import Credentials from "next-auth/providers/credentials";

//mock users3
669
// The only place the mock users live.
//defines who can log in and what their roles are
const users = [
  { id: "1", username: "employer1", password: "password123", role: "employer", name: "Employer One" },
  { id: "2", username: "employer2", password: "password123", role: "employer", name: "Employer Two" },
  { id: "3", username: "alice", password: "password123", role: "candidate", name: "Alice" },
  { id: "4", username: "bob", password: "password123", role: "candidate", name: "Bob" },
];

export const { handlers, auth, signIn, signOut } = NextAuth({
  // JWT strategy: the session is stored in a signed cookie, not a database.
  session: { strategy: "jwt" },

  // Our own login page instead of the default Auth.js one.
  pages: { signIn: "/login" },

  providers: [
    Credentials({
      // The fields the authorize function receives. Username, not email.
      credentials: {
        username: { label: "Username" },
        password: { label: "Password", type: "password" },
      },

      // Validates the submitted credentials. Returns the user on success or
      // null on any mismatch — it must NOT throw.
      authorize: (credentials) => {
        const username = credentials?.username as string | undefined;
        const password = credentials?.password as string | undefined;

        const user = users.find((u) => u.username === username);

        // Strict equality — this is a mock, no bcrypt.
        if (!user || user.password !== password) {
          return null;
        }

        // Only the fields we actually use flow onward. No password, no email.
        return { id: user.id, name: user.name, role: user.role };
      },
    }),
  ],

  callbacks: {
    // STEP 2 of the relay: authorize's return value arrives here as `user` on
    // first sign-in. Copy role onto the token so it survives in the cookie.
    //copies use role on to the token
    jwt: ({ token, user }) => {
      if (user) {
        token.role = user.role;
      }
      return token;
    },

    // STEP 3 of the relay: copy role from the token onto the session so any
    // component calling auth() can read session.user.role. Without this step
    // the role would live on the JWT but never reach the components.
    session: ({ session, token }) => {
      if (session.user) {
        session.user.role = token.role as string;
      }
      return session;
    },
  },
});

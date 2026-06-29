// src/lib/loginRedirect.ts
// Helper for the login page's role-based redirect.
//
// signIn() decides the destination before the session exists, so we map a
// username to its role (and a role to its landing page) from the same mock data
// authorize() trusts. This only chooses WHERE to send the user; signIn still
// validates the password, so a wrong password never actually redirects anywhere
// except back to the login page with an error.


//iguring out where to send the user after they sign in.
//which user name belongs to which role
const userRoles: Record<string, "employer" | "candidate"> = {
  employer1: "employer",
  employer2: "employer",
  alice: "candidate",
  bob: "candidate",
};

export function roleForUsername(username: string): "employer" | "candidate" | null {
  return userRoles[username] ?? null;
}

//redirect toa specific page based on the role
export function redirectForRole(role: "employer" | "candidate" | null): string {
  if (role === "employer") return "/dashboard/listings";
  if (role === "candidate") return "/jobs";
  // Unknown username — let signIn fail and bounce back to login.
  return "/jobs";
}

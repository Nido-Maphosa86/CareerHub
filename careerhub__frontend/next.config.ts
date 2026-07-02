// next.config.ts
// Assignment 3.3 — Performance & SEO configuration.
//
// Three concerns live here:
//  1. Bundle analyzer — run `npm run analyze` to open a visual treemap showing
//     which packages end up in which JS chunks. Used to verify the dynamic
//     import of ApplicationWizard actually created a split chunk.
//  2. Remote image patterns — next/image refuses to serve images from unknown
//     domains for security. Any remote URL used in a next/image src must match
//     one of these patterns or the build fails with a runtime error.
//  3. Existing app config is preserved unchanged below both additions.

import type { NextConfig } from "next";
import withBundleAnalyzer from "@next/bundle-analyzer";

const nextConfig: NextConfig = {
  images: {
    // Allow next/image to optimise images served from these remote origins.
    // Add an entry here for every external domain used in an <Image src="..."> tag.
    // The deprecated `domains` array is intentionally NOT used — remotePatterns
    // is the current API and supports wildcard subdomains.
    remotePatterns: [
      {
        // The CareerHub.Api backend — used for any company logo URLs the API
        // might return in the future. Set to localhost for local dev; change to
        // your Vercel / Railway API domain before deploying.
        protocol: "http",
        hostname: "localhost",
        port: "5000",
        pathname: "/**",
      },
    ],
  },
};

// Wrapping with withBundleAnalyzer means the treemap only opens when the
// ANALYZE env var is "true" — normal builds are unaffected.
export default withBundleAnalyzer({
  enabled: process.env.ANALYZE === "true",
})(nextConfig);

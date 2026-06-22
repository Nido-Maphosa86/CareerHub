// src/lib/utils.ts
// Merges Tailwind class names safely.
// clsx joins them, tailwind-merge resolves conflicts (e.g. "p-2 p-4" -> "p-4").

import { clsx, type ClassValue } from "clsx";
import { twMerge } from "tailwind-merge";

export function cn(...inputs: ClassValue[]) {
  return twMerge(clsx(inputs));
}

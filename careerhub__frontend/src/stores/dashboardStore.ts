// src/stores/dashboardStore.ts
// Assignment 2.3 — Part 7: the employer dashboard preference store (Zustand v5).
//
// This holds session-level UI preferences: how listings are displayed (table or
// grid) and whether closed jobs are shown. These are UI state, not user data —
// they should survive navigating around the app but it is fine for them to reset
// on a hard refresh. So there is NO persist middleware: the store lives in
// memory only. (If we wanted persistence the right approach would be the persist
// middleware writing to localStorage under a key like "careerhub-dashboard-prefs"
// with a { view, showClosedJobs } shape — see the README for why we don't.)

import { create } from "zustand";

interface DashboardState {
  view: "table" | "grid";
  setView: (view: "table" | "grid") => void;
  showClosedJobs: boolean;
  toggleShowClosedJobs: () => void;
}

export const useDashboardStore = create<DashboardState>((set) => ({
  view: "table",
  setView: (view) => set({ view }),
  showClosedJobs: true,
  toggleShowClosedJobs: () =>
    set((state) => ({ showClosedJobs: !state.showClosedJobs })),
}));

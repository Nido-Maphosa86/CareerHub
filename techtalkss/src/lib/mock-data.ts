import { Talk, Registration } from '@/types'

const talks: Talk[] = [
  {
    id: 1,
    title: 'React 19 Deep Dive: The Compiler & Auto-Memoisation',
    speaker: 'Alice Nkosi',
    topic: 'Frontend',
    duration: 45,
    capacity: 60,
    registrationCount: 47,
    scheduledAt: '2026-08-12T09:00:00',
    location: 'Main Stage',
    description: 'A hands-on look at what the React 19 compiler actually does, why it removes the need for useMemo/useCallback in most cases, and how to verify it is working.',
  },
  {
    id: 2,
    title: 'TypeScript Strict Mode Survival Guide',
    speaker: 'Brendan Osei',
    topic: 'Frontend',
    duration: 30,
    capacity: 40,
    registrationCount: 38,
    scheduledAt: '2026-08-12T11:00:00',
    location: 'Room A',
    description: 'Real patterns for taming noImplicitAny, strictNullChecks, and discriminated unions in a production codebase.',
  },
  {
    id: 3,
    title: 'Next.js App Router: Server vs Client — Drawing the Line',
    speaker: 'Chloé Dupont',
    topic: 'Frontend',
    duration: 45,
    capacity: 60,
    registrationCount: 52,
    scheduledAt: '2026-08-12T14:00:00',
    location: 'Main Stage',
    description: 'When to reach for "use client", why Server Components cannot read cookies or call hooks, and how Suspense boundaries let you stream partial UI.',
  },
  {
    id: 4,
    title: 'Zod v4: One Schema, Zero Duplication',
    speaker: 'Daniel Ferreira',
    topic: 'Backend',
    duration: 30,
    capacity: 35,
    registrationCount: 20,
    scheduledAt: '2026-08-13T10:00:00',
    location: 'Room B',
    description: 'Why z.infer replaces hand-written TypeScript types, how to use .refine for cross-field validation, and a tour of what changed in v4.',
  },
  {
    id: 5,
    title: 'Deploying .NET APIs to Azure Container Apps',
    speaker: 'Fatima Al-Rashid',
    topic: 'DevOps',
    duration: 60,
    capacity: 30,
    registrationCount: 28,
    scheduledAt: '2026-08-13T13:00:00',
    location: 'Room C',
    description: 'From dockerfile to health checks: packaging a .NET 10 minimal API and shipping it to Azure Container Apps with a managed identity.',
  },
  {
    id: 6,
    title: 'LLM-Powered Search with .NET + Semantic Kernel',
    speaker: 'Grace Mensah',
    topic: 'AI/ML',
    duration: 45,
    capacity: 50,
    registrationCount: 15,
    scheduledAt: '2026-08-13T15:30:00',
    location: 'Main Stage',
    description: 'Building a hybrid semantic + keyword search pipeline using Semantic Kernel, pgvector, and a .NET 10 background service.',
  },
]

const registrations: Registration[] = []

const delay = (ms: number) => new Promise(res => setTimeout(res, ms))

async function fetchTalks(): Promise<Talk[]> {
  await delay(700)
  return talks
}

async function fetchTalkById(id: number): Promise<Talk | null> {
  await delay(400)
  return talks.find(t => t.id === id) ?? null
}

async function createRegistration(data: {
  talkId: number
  attendeeName: string
  attendeeEmail: string
}): Promise<Registration> {
  await delay(900)
  const duplicate = registrations.find(
    r => r.talkId === data.talkId && r.attendeeEmail === data.attendeeEmail
  )
  if (duplicate) {
    throw new Error('You are already registered for this talk.')
  }
  const registration: Registration = {
    id: registrations.length + 1,
    talkId: data.talkId,
    attendeeName: data.attendeeName,
    attendeeEmail: data.attendeeEmail,
    registeredAt: new Date().toISOString(),
  }
  registrations.push(registration)
  const talk = talks.find(t => t.id === data.talkId)
  if (talk) talk.registrationCount += 1
  return registration
}

export { talks, fetchTalks, fetchTalkById, createRegistration }
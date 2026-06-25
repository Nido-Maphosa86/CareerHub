export type TalkTopic = 'Frontend' | 'Backend' | 'DevOps' | 'AI/ML' | 'Mobile'

export interface Talk {
  id: number
  title: string
  speaker: string
  topic: TalkTopic
  duration: number
  capacity: number
  registrationCount: number
  scheduledAt: string
  location: string
  description: string
}

export interface Registration {
  id: number
  talkId: number
  attendeeName: string
  attendeeEmail: string
  registeredAt: string
}
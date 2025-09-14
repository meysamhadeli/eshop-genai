export type TrackUserActivityDto = {
  userId: string
  itemId: string
  activityType?: string
  metadata?: Record<string, any>
  context?: {
    sessionId?: string
    deviceType?: string
    location?: string
    referrer?: string
    duration?: number
    searchQuery?: string
    category?: string
    tags?: string[]
  }
}
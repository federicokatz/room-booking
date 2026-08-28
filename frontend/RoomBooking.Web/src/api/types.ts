export type CurrentUser = {
  userName: string
}

export type Room = {
  id: string
  code: string
  capacity: number
}

export type Booking = {
  id: string
  roomCode: string
  title: string
  attendees: number
  startUtc: string
  endUtc: string
  status: number
  cancelledAtUtc: string | null
}

export type ChatSession = {
  sessionId: string
}

export type ChatResponse = {
  assistantMessage: string
  effects: string[]
}

export type ProblemDetails = {
  detail?: string
  title?: string
}

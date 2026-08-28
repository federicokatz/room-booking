import type { Booking, ChatResponse, ChatSession, CurrentUser, ProblemDetails, Room } from './types'

const csrfHeaderName = 'X-CSRF-TOKEN'

export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
  ) {
    super(message)
    this.name = 'ApiError'
  }
}

let csrfToken: string | null = null

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const response = await fetch(path, {
    ...init,
    credentials: 'include',
    headers: {
      Accept: 'application/json',
      ...init.headers,
    },
  })

  if (!response.ok) {
    let problem: ProblemDetails | undefined
    try {
      problem = (await response.json()) as ProblemDetails
    } catch {
      // Some deliberate API errors have no response body.
    }

    throw new ApiError(response.status, problem?.detail ?? problem?.title ?? 'The request could not be completed.')
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

async function getCsrfToken(): Promise<string> {
  if (csrfToken) {
    return csrfToken
  }

  const response = await request<{ token: string }>('/api/auth/csrf')
  csrfToken = response.token
  return csrfToken
}

async function mutation<T>(path: string, init: RequestInit = {}): Promise<T> {
  const token = await getCsrfToken()
  return request<T>(path, {
    ...init,
    headers: {
      'Content-Type': 'application/json',
      [csrfHeaderName]: token,
      ...init.headers,
    },
  })
}

export const api = {
  async login(userName: string, password: string): Promise<CurrentUser> {
    const user = await request<CurrentUser>('/api/auth/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ userName, password }),
    })
    csrfToken = null
    return user
  },

  getCurrentUser: () => request<CurrentUser>('/api/auth/me'),

  async logout(): Promise<void> {
    await mutation<void>('/api/auth/logout', { method: 'POST' })
    csrfToken = null
  },

  getRooms: () => request<Room[]>('/api/rooms'),

  getMyBookings: () => request<Booking[]>('/api/bookings/mine'),

  createChatSession: () => mutation<ChatSession>('/api/chat/sessions', { method: 'POST' }),

  sendChatMessage: (sessionId: string, message: string) =>
    mutation<ChatResponse>(`/api/chat/sessions/${sessionId}/messages`, {
      method: 'POST',
      body: JSON.stringify({ message }),
    }),

  deleteChatSession: (sessionId: string) =>
    mutation<void>(`/api/chat/sessions/${sessionId}`, { method: 'DELETE' }),
}

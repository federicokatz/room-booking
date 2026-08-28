import { fireEvent, render, screen, waitFor } from '@testing-library/react'
import { beforeEach, describe, expect, it, vi } from 'vitest'
import App from './App'

const api = vi.hoisted(() => ({
  login: vi.fn(),
  getCurrentUser: vi.fn(),
  logout: vi.fn(),
  getRooms: vi.fn(),
  getMyBookings: vi.fn(),
  createChatSession: vi.fn(),
  sendChatMessage: vi.fn(),
  deleteChatSession: vi.fn(),
}))

vi.mock('./api/client', async () => {
  class ApiError extends Error { constructor(public readonly status: number, message: string) { super(message) } }
  return { api, ApiError }
})

describe('App', () => {
  beforeEach(() => {
    vi.clearAllMocks()
    api.getCurrentUser.mockRejectedValue(new Error('No session'))
    api.getRooms.mockResolvedValue([{ id: 'A', code: 'A', capacity: 4 }])
    api.getMyBookings.mockResolvedValue([])
  })

  it('shows the login screen when no authenticated session exists', async () => {
    render(<App />)
    expect(await screen.findByRole('heading', { name: 'Enter your workspace' })).toBeInTheDocument()
  })

  it('shows the workspace after a successful login', async () => {
    api.login.mockResolvedValue({ userName: 'User1' })
    render(<App />)

    await screen.findByRole('heading', { name: 'Enter your workspace' })
    fireEvent.change(screen.getByLabelText('Password'), { target: { value: 'TechnicalChallengePromtior' } })
    fireEvent.click(screen.getByRole('button', { name: /enter workspace/i }))

    expect(await screen.findByRole('heading', { name: 'How can I help?' })).toBeInTheDocument()
    expect(await screen.findByText('Room A')).toBeInTheDocument()
  })

  it('sends a chat message and refreshes bookings after a booking effect', async () => {
    api.getCurrentUser.mockResolvedValue({ userName: 'User1' })
    api.createChatSession.mockResolvedValue({ sessionId: 'session-1' })
    api.sendChatMessage.mockResolvedValue({ assistantMessage: '### Agenda\n\n| Time | Status |\n| --- | --- |\n| 10:00 | **Occupied** |\n\n- **Room:** A', effects: ['booking_created'] })
    render(<App />)

    await screen.findByRole('heading', { name: 'How can I help?' })
    fireEvent.change(screen.getByLabelText('Message the booking assistant'), { target: { value: 'Book room A' } })
    fireEvent.click(screen.getByRole('button', { name: 'Send message' }))

    expect(await screen.findByText('Occupied')).toBeInTheDocument()
    expect(screen.getByRole('table')).toBeInTheDocument()
    expect(screen.getByRole('list')).toBeInTheDocument()
    expect(screen.getByText('Room:')).toBeInTheDocument()
    await waitFor(() => expect(api.getMyBookings).toHaveBeenCalledTimes(2))
  })

  it('sends a chat message with Enter while Shift+Enter keeps the composer open', async () => {
    api.getCurrentUser.mockResolvedValue({ userName: 'User1' })
    api.createChatSession.mockResolvedValue({ sessionId: 'session-1' })
    api.sendChatMessage.mockResolvedValue({ assistantMessage: 'Done.', effects: [] })
    render(<App />)

    const composer = await screen.findByLabelText('Message the booking assistant')
    fireEvent.change(composer, { target: { value: 'Book room A' } })
    fireEvent.keyDown(composer, { key: 'Enter', shiftKey: true })
    expect(api.sendChatMessage).not.toHaveBeenCalled()

    fireEvent.keyDown(composer, { key: 'Enter' })
    await waitFor(() => expect(api.sendChatMessage).toHaveBeenCalledWith('session-1', 'Book room A'))
  })
})

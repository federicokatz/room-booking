import { useCallback, useEffect, useState } from 'react'
import { api, ApiError } from './api/client'
import type { Booking, CurrentUser, Room } from './api/types'
import { Dashboard } from './components/Dashboard'
import { LoginScreen } from './components/LoginScreen'
import type { ConversationMessage } from './components/ChatPanel'

const welcomeMessage: ConversationMessage = {
  id: 'welcome',
  role: 'assistant',
  content: 'I can help you find a room, check a schedule, create a booking, or cancel one of your bookings. What do you need?',
}

function toErrorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    if (error.status === 401) return 'Your session has ended. Sign in again to continue.'
    if (error.status === 503) return 'The booking assistant is temporarily unavailable. Try again shortly.'
    return error.message
  }

  return 'Something interrupted this request. Please try again.'
}

export default function App() {
  const [user, setUser] = useState<CurrentUser | null>(null)
  const [isCheckingSession, setIsCheckingSession] = useState(true)
  const [rooms, setRooms] = useState<Room[]>([])
  const [bookings, setBookings] = useState<Booking[]>([])
  const [isLoadingData, setIsLoadingData] = useState(false)
  const [sessionId, setSessionId] = useState<string | null>(null)
  const [messages, setMessages] = useState<ConversationMessage[]>([welcomeMessage])
  const [isSending, setIsSending] = useState(false)
  const [chatError, setChatError] = useState<string | null>(null)

  const resetWorkspace = useCallback(() => {
    setUser(null)
    setRooms([])
    setBookings([])
    setSessionId(null)
    setMessages([welcomeMessage])
    setChatError(null)
  }, [])

  const loadWorkspaceData = useCallback(async () => {
    setIsLoadingData(true)
    try {
      const [loadedRooms, loadedBookings] = await Promise.all([api.getRooms(), api.getMyBookings()])
      setRooms(loadedRooms)
      setBookings(loadedBookings)
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) resetWorkspace()
      else setChatError('The workspace data could not be refreshed. Try again in a moment.')
    } finally {
      setIsLoadingData(false)
    }
  }, [resetWorkspace])

  useEffect(() => {
    let isMounted = true
    api.getCurrentUser()
      .then((currentUser) => { if (isMounted) setUser(currentUser) })
      .catch(() => { if (isMounted) resetWorkspace() })
      .finally(() => { if (isMounted) setIsCheckingSession(false) })

    return () => { isMounted = false }
  }, [resetWorkspace])

  useEffect(() => {
    if (user) void loadWorkspaceData()
  }, [user, loadWorkspaceData])

  async function handleLogin(userName: string, password: string) {
    const authenticatedUser = await api.login(userName, password)
    setUser(authenticatedUser)
  }

  async function ensureChatSession() {
    if (sessionId) return sessionId
    const session = await api.createChatSession()
    setSessionId(session.sessionId)
    return session.sessionId
  }

  async function handleSendMessage(content: string) {
    setChatError(null)
    setMessages((current) => [...current, { id: crypto.randomUUID(), role: 'user', content }])
    setIsSending(true)

    try {
      const activeSessionId = await ensureChatSession()
      const response = await api.sendChatMessage(activeSessionId, content)
      setMessages((current) => [...current, { id: crypto.randomUUID(), role: 'assistant', content: response.assistantMessage }])
      if (response.effects.includes('booking_created') || response.effects.includes('booking_cancelled')) {
        await loadWorkspaceData()
      }
    } catch (error) {
      if (error instanceof ApiError && error.status === 401) resetWorkspace()
      else setChatError(toErrorMessage(error))
    } finally {
      setIsSending(false)
    }
  }

  async function handleLogout() {
    try {
      if (sessionId) {
        try {
          await api.deleteChatSession(sessionId)
        } catch {
          // The server session may have already expired; logging out still matters.
        }
      }
      await api.logout()
    } catch {
      // Clearing the local workspace remains safe if the session already expired.
    } finally {
      resetWorkspace()
    }
  }

  if (isCheckingSession) return <main className="app-loading"><div className="loading-mark" /><p>Opening workspace…</p></main>
  if (!user) return <LoginScreen onLogin={handleLogin} />

  return <Dashboard
    user={user}
    rooms={rooms}
    bookings={bookings}
    isLoadingData={isLoadingData}
    messages={messages}
    isSending={isSending}
    chatError={chatError}
    onSendMessage={handleSendMessage}
    onLogout={handleLogout}
  />
}

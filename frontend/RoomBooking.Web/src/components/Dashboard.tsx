import type { Booking, CurrentUser, Room } from '../api/types'
import { BrandMark } from './BrandMark'
import { BookingsPanel } from './BookingsPanel'
import { ChatPanel, type ConversationMessage } from './ChatPanel'
import { Icon } from './Icon'
import { RoomsPanel } from './RoomsPanel'

type DashboardProps = {
  user: CurrentUser
  rooms: Room[]
  bookings: Booking[]
  isLoadingData: boolean
  messages: ConversationMessage[]
  isSending: boolean
  chatError: string | null
  onSendMessage: (message: string) => Promise<void>
  onLogout: () => Promise<void>
}

export function Dashboard(props: DashboardProps) {
  return (
    <main className="workspace-page">
      <header className="workspace-header">
        <div className="workspace-context">
          <a className="workspace-brand" href="/" aria-label="Room Booking home"><BrandMark /><span>ROOM BOOKING</span></a>
          <span className="location-label">CUBO ITAÚ · MONTEVIDEO</span>
        </div>
        <div className="workspace-meta"><div className="user-menu"><span className="avatar">{props.user.userName.slice(0, 1)}</span><span>{props.user.userName}</span><button onClick={() => void props.onLogout()} title="Log out" aria-label="Log out"><Icon name="log-out" size={17} /></button></div></div>
      </header>
      <div className="workspace-grid">
        <RoomsPanel rooms={props.rooms} isLoading={props.isLoadingData} />
        <ChatPanel messages={props.messages} isSending={props.isSending} error={props.chatError} onSend={props.onSendMessage} />
        <BookingsPanel bookings={props.bookings} isLoading={props.isLoadingData} />
      </div>
    </main>
  )
}

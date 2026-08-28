import type { Booking } from '../api/types'
import { Icon } from './Icon'

export function BookingsPanel({ bookings, isLoading }: { bookings: Booking[]; isLoading: boolean }) {
  return (
    <section className="panel bookings-panel" aria-labelledby="bookings-heading">
      <div className="panel-heading">
        <div><p className="eyebrow">YOUR AGENDA</p><h2 id="bookings-heading">My bookings</h2></div>
        {!isLoading && <span className="panel-count">{bookings.length}</span>}
      </div>
      {isLoading ? <BookingSkeletons /> : bookings.length > 0 ? <div className="booking-list">{bookings.map((booking) => <BookingCard key={booking.id} booking={booking} />)}</div> : <EmptyBookings />}
    </section>
  )
}

function BookingCard({ booking }: { booking: Booking }) {
  const start = new Date(booking.startUtc)
  const end = new Date(booking.endUtc)
  const date = new Intl.DateTimeFormat('en', { weekday: 'short', day: 'numeric', month: 'short', timeZone: 'America/Montevideo' }).format(start)
  const time = new Intl.DateTimeFormat('en', { hour: '2-digit', minute: '2-digit', hour12: false, timeZone: 'America/Montevideo' })

  return (
    <article className="booking-card">
      <div className="booking-room">{booking.roomCode}</div>
      <div className="booking-copy"><h3>{booking.title}</h3><p><Icon name="calendar" size={14} /> {date} <span>·</span> <Icon name="clock" size={14} /> {time.format(start)}–{time.format(end)}</p></div>
      <span className="booking-attendees" aria-label={`${booking.attendees} attendees`}><Icon name="users" size={14} /> {booking.attendees}</span>
    </article>
  )
}

function EmptyBookings() {
  return <div className="empty-bookings"><div className="empty-mark">—</div><p>No bookings on your agenda.</p><span>Ask the assistant to reserve a room.</span></div>
}

function BookingSkeletons() {
  return <div className="booking-list">{[1, 2].map((item) => <div className="booking-card booking-skeleton" key={item}><div className="skeleton-box" /><div className="skeleton-lines"><span /><span /></div></div>)}</div>
}

import type { Room } from '../api/types'
import { Icon } from './Icon'

export function RoomsPanel({ rooms, isLoading }: { rooms: Room[]; isLoading: boolean }) {
  return (
    <section className="panel rooms-panel" aria-labelledby="rooms-heading">
      <div className="panel-heading">
        <div><p className="eyebrow">THE OFFICE</p><h2 id="rooms-heading">Your rooms</h2></div>
        <span className="panel-count">{isLoading ? '—' : rooms.length}</span>
      </div>
      <div className="room-list">
        {isLoading ? <RoomSkeletons /> : rooms.map((room) => <RoomCard key={room.id} room={room} />)}
      </div>
      <p className="panel-note">Ask the assistant to check availability for any time and group size.</p>
    </section>
  )
}

function RoomCard({ room }: { room: Room }) {
  return (
    <article className="room-card">
      <div className="room-code">{room.code}</div>
      <div><h3>Room {room.code}</h3><p><Icon name="users" size={14} /> Up to {room.capacity}</p></div>
    </article>
  )
}

function RoomSkeletons() {
  return <>{['A', 'B', 'C', 'D', 'E'].map((code) => <div className="room-card room-skeleton" key={code}><div className="skeleton-box" /><div className="skeleton-lines"><span /><span /></div></div>)}</>
}

import { FormEvent, useState } from 'react'
import { ApiError } from '../api/client'
import { BrandMark } from './BrandMark'
import { Icon } from './Icon'

type LoginScreenProps = {
  onLogin: (userName: string, password: string) => Promise<void>
}

export function LoginScreen({ onLogin }: LoginScreenProps) {
  const [userName, setUserName] = useState('User1')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [isSubmitting, setIsSubmitting] = useState(false)

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    setError(null)
    setIsSubmitting(true)

    try {
      await onLogin(userName, password)
    } catch (caught) {
      setError(caught instanceof ApiError && caught.status === 401
        ? 'Check the username and password, then try again.'
        : 'Sign-in is unavailable right now. Try again in a moment.')
    } finally {
      setIsSubmitting(false)
    }
  }

  return (
    <main className="login-page">
      <section className="login-intro" aria-label="Room Booking introduction">
        <div className="intro-orbit orbit-one" />
        <div className="intro-orbit orbit-two" />
        <div className="login-brand"><BrandMark /><span>ROOM BOOKING</span></div>
        <div className="intro-copy">
          <p className="eyebrow"><Icon name="sparkle" size={15} /> CUBO ITAÚ · MONTEVIDEO</p>
          <h1>Make space for <em>better</em> work.</h1>
          <p>One conversation is all it takes to find, reserve, or adjust the room your team needs.</p>
        </div>
        <div className="intro-footnote"><span className="status-dot" /> Five rooms, one calm workspace.</div>
      </section>

      <section className="login-panel">
        <div className="login-card">
          <p className="eyebrow">WELCOME BACK</p>
          <h2>Enter your workspace</h2>
          <p className="login-description">Use the challenge account assigned to you.</p>
          <form onSubmit={handleSubmit}>
            <label>
              Username
              <input value={userName} onChange={(event) => setUserName(event.target.value)} autoComplete="username" required />
            </label>
            <label>
              Password
              <input type="password" value={password} onChange={(event) => setPassword(event.target.value)} autoComplete="current-password" required />
            </label>
            {error && <p className="form-error" role="alert">{error}</p>}
            <button className="primary-button login-button" type="submit" disabled={isSubmitting}>
              {isSubmitting ? 'Entering workspace…' : <>Enter workspace <Icon name="arrow-up" /></>}
            </button>
          </form>
          <p className="login-hint">This assistant only handles meeting-room bookings.</p>
        </div>
      </section>
    </main>
  )
}

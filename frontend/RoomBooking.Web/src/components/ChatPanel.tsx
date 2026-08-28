import { FormEvent, useEffect, useRef, useState } from 'react'
import ReactMarkdown from 'react-markdown'
import remarkGfm from 'remark-gfm'
import { Icon } from './Icon'

export type ConversationMessage = {
  id: string
  role: 'assistant' | 'user'
  content: string
}

type ChatPanelProps = {
  messages: ConversationMessage[]
  isSending: boolean
  error: string | null
  onSend: (message: string) => Promise<void>
}

export function ChatPanel({ messages, isSending, error, onSend }: ChatPanelProps) {
  const [message, setMessage] = useState('')
  const endRef = useRef<HTMLDivElement>(null)

  useEffect(() => {
    endRef.current?.scrollIntoView({ behavior: 'smooth' })
  }, [messages, isSending])

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault()
    const trimmed = message.trim()
    if (!trimmed || isSending) return
    setMessage('')
    await onSend(trimmed)
  }

  return (
    <section className="chat-panel" aria-labelledby="chat-heading">
      <header className="chat-header">
        <div><p className="eyebrow"><span className="status-dot" /> ASSISTANT ONLINE</p><h2 id="chat-heading">What are we making room for?</h2></div>
        <div className="chat-emblem"><Icon name="sparkle" /></div>
      </header>
      <div className="chat-thread" aria-live="polite">
        {messages.map((item) => <article className={`message message-${item.role}`} key={item.id}><span>{item.role === 'assistant' ? 'ROOM BOOKING' : 'YOU'}</span><div className="message-content"><ReactMarkdown remarkPlugins={[remarkGfm]} skipHtml>{item.content}</ReactMarkdown></div></article>)}
        {isSending && <article className="message message-assistant typing"><span>ROOM BOOKING</span><div className="message-content"><i /><i /><i /></div></article>}
        {error && <p className="chat-error" role="alert">{error}</p>}
        <div ref={endRef} />
      </div>
      <form className="chat-composer" onSubmit={handleSubmit}>
        <label className="sr-only" htmlFor="chat-message">Message the booking assistant</label>
        <textarea id="chat-message" rows={1} placeholder="Ask about a room, a time, or a booking…" value={message} onChange={(event) => setMessage(event.target.value)} disabled={isSending} />
        <button className="send-button" type="submit" disabled={!message.trim() || isSending} aria-label="Send message"><Icon name="arrow-up" size={20} /></button>
      </form>
      <p className="chat-disclaimer">The assistant will ask for missing details before making any changes.</p>
    </section>
  )
}

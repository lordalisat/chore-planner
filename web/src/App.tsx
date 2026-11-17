import { useEffect, useState, type FormEvent } from 'react'

type Session = 'checking' | 'anonymous' | 'authenticated' | 'unreachable'

function App() {
  const [session, setSession] = useState<Session>('checking')

  useEffect(() => {
    // Any 401 from the API means the household password is needed
    fetch('/api/session')
      .then((response) => setSession(response.ok ? 'authenticated' : response.status === 401 ? 'anonymous' : 'unreachable'))
      .catch(() => setSession('unreachable'))
  }, [])

  async function logout() {
    await fetch('/api/logout', { method: 'POST' })
    setSession('anonymous')
  }

  if (session === 'checking') return null
  if (session === 'unreachable') {
    return (
      <main>
        <h1>Chores</h1>
        <p role="alert">The server can't be reached right now. Try again later.</p>
      </main>
    )
  }
  if (session === 'anonymous') return <Login onLogin={() => setSession('authenticated')} />

  return (
    <main>
      <h1>Chores</h1>
      <p>What needs doing around the house will show up here.</p>
      <button type="button" onClick={logout}>
        Log out
      </button>
    </main>
  )
}

function Login({ onLogin }: { onLogin: () => void }) {
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string>()

  async function submit(event: FormEvent) {
    event.preventDefault()
    const response = await fetch('/api/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ password }),
    }).catch(() => undefined)
    if (response?.ok) onLogin()
    else if (response?.status === 401) setError('That password is not right.')
    else setError("The server can't be reached right now. Try again later.")
  }

  return (
    <main>
      <h1>Chores</h1>
      <form onSubmit={submit}>
        <label>
          Household password{' '}
          <input
            type="password"
            autoComplete="current-password"
            autoFocus
            value={password}
            onChange={(event) => setPassword(event.target.value)}
          />
        </label>{' '}
        <button type="submit">Log in</button>
        {error && <p role="alert">{error}</p>}
      </form>
    </main>
  )
}

export default App

import { useEffect, useState, type FormEvent } from 'react'

type Session = 'checking' | 'anonymous' | 'authenticated'

function App() {
  const [session, setSession] = useState<Session>('checking')

  useEffect(() => {
    // Any 401 from the API means the household password is needed
    fetch('/api/session').then((response) => setSession(response.status === 401 ? 'anonymous' : 'authenticated'))
  }, [])

  async function logout() {
    await fetch('/api/logout', { method: 'POST' })
    setSession('anonymous')
  }

  if (session === 'checking') return null
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
  const [wrong, setWrong] = useState(false)

  async function submit(event: FormEvent) {
    event.preventDefault()
    const response = await fetch('/api/login', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ password }),
    })
    if (response.ok) onLogin()
    else setWrong(true)
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
        {wrong && <p role="alert">That password is not right.</p>}
      </form>
    </main>
  )
}

export default App

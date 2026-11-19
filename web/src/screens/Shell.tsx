import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, Outlet, useNavigate, useRouter } from '@tanstack/react-router'
import { useEffect, useState } from 'react'
import { api } from '../api/client.ts'
import { sessionQuery } from '../session.ts'
import { Dialog } from '../ui/Dialog.tsx'
import { Menu } from '../ui/Menu.tsx'
import { toast } from '../ui/toast.ts'
import { ChoreForm } from './ChoreForm.tsx'
import { unreachableMessage } from './Unreachable.tsx'

// Links to the screens besides the Overview; All Chores (09) joins here
const navLinks: { to: '/areas'; label: string }[] = [{ to: '/areas', label: 'Areas' }]

/** Header with navigation around every screen but the login: a menu on a phone, a top bar from md up. */
export function Shell() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const router = useRouter()
  const [creating, setCreating] = useState(false)
  // Refetched on focus, so a session ended elsewhere sends this device to the login. The route guard does the
  // redirect, once; navigating from here would rerun on every location change until the Shell unmounts.
  const { data: session } = useQuery(sessionQuery)
  useEffect(() => {
    if (session === 'anonymous') void router.invalidate()
  }, [session, router])

  const logout = useMutation({
    mutationFn: () => api('/api/logout', 'post'),
    onSuccess: () => queryClient.setQueryData(sessionQuery.queryKey, 'anonymous'),
    onError: () => toast(unreachableMessage),
  })

  return (
    <div className="min-h-dvh">
      <header className="bg-brand text-white">
        <div className="mx-auto flex max-w-3xl items-center gap-2 px-4 py-2">
          <Link to="/" className="py-2 text-lg font-semibold">
            Chores
          </Link>
          <nav className="ml-auto hidden items-center gap-2 md:flex">
            {navLinks.map((link) => (
              <Link key={link.label} to={link.to} className="rounded-lg px-3 py-2 hover:bg-white/10">
                {link.label}
              </Link>
            ))}
            <button type="button" onClick={() => setCreating(true)} className="rounded-lg bg-cream px-3 py-2 font-semibold text-brand hover:bg-white">
              New Chore
            </button>
            <button type="button" onClick={() => logout.mutate()} className="rounded-lg px-3 py-2 hover:bg-white/10">
              Log out
            </button>
          </nav>
          <div className="ml-auto md:hidden">
            <Menu
              label="Menu"
              trigger={<MenuIcon />}
              entries={[
                ...navLinks.map((link) => ({ label: link.label, onSelect: () => void navigate({ to: link.to }) })),
                { label: 'Log out', onSelect: () => logout.mutate() },
              ]}
            />
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-3xl px-4 py-6">
        <Outlet />
      </main>
      <button
        type="button"
        aria-label="New Chore"
        onClick={() => setCreating(true)}
        className="fixed right-4 bottom-6 flex size-14 items-center justify-center rounded-full bg-brand text-3xl text-white shadow-lg md:hidden"
      >
        +
      </button>
      <Dialog open={creating} onOpenChange={setCreating} title="New Chore">
        {/* Mounted only while open, so each new Chore starts from an empty form */}
        {creating && <ChoreForm onDone={() => setCreating(false)} />}
      </Dialog>
    </div>
  )
}

function MenuIcon() {
  return (
    <svg viewBox="0 0 24 24" className="size-6" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <path d="M4 6h16M4 12h16M4 18h16" strokeLinecap="round" />
    </svg>
  )
}

import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, Outlet, useLocation, useNavigate } from '@tanstack/react-router'
import { useEffect } from 'react'
import { api } from '../api/client.ts'
import { sessionQuery, unreachableMessage } from '../session.ts'
import { Menu } from '../ui/Menu.tsx'
import { toast } from '../ui/toast.ts'

// The screens besides the Overview; Areas (08) and All Chores (09) join here
const screens: { to: '/'; label: string }[] = []

/** Header with navigation around every screen but the login: a menu on a phone, a top bar from md up. */
export function Shell() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const location = useLocation()
  // Refetched on focus, so a session ended elsewhere sends this device to the login
  const { data: session } = useQuery(sessionQuery)
  useEffect(() => {
    if (session === 'anonymous') void navigate({ to: '/login', search: { redirect: location.href } })
  }, [session, navigate, location.href])

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
            {screens.map((screen) => (
              <Link key={screen.label} to={screen.to} className="rounded-lg px-3 py-2 hover:bg-white/10">
                {screen.label}
              </Link>
            ))}
            {/* Opens the new-Chore form from 02 on */}
            <button type="button" className="rounded-lg bg-cream px-3 py-2 font-semibold text-brand hover:bg-white">
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
                ...screens.map((screen) => ({ label: screen.label, onSelect: () => void navigate({ to: screen.to }) })),
                { label: 'Log out', onSelect: () => logout.mutate() },
              ]}
            />
          </div>
        </div>
      </header>
      <main className="mx-auto max-w-3xl px-4 py-6">
        <Outlet />
      </main>
      {/* Opens the new-Chore form from 02 on */}
      <button
        type="button"
        aria-label="New Chore"
        className="fixed right-4 bottom-6 flex size-14 items-center justify-center rounded-full bg-brand text-3xl text-white shadow-lg md:hidden"
      >
        +
      </button>
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

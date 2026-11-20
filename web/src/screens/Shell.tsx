import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, Outlet, useNavigate, useRouter, useRouterState } from '@tanstack/react-router'
import { useEffect, useState } from 'react'
import { api } from '../api/client.ts'
import { sessionQuery } from '../session.ts'
import { Dialog } from '../ui/Dialog.tsx'
import { Menu } from '../ui/Menu.tsx'
import { toast } from '../ui/toast.ts'
import { fromLg, useMediaQuery } from '../ui/useMediaQuery.ts'
import { ChoreForm } from './ChoreForm.tsx'
import { unreachableMessage } from './Unreachable.tsx'

// Every screen in the navigation; the Overview only counts as current on itself, not on the screens below it
const navLinks: { to: '/' | '/chores' | '/areas'; label: string; exact?: boolean }[] = [
  { to: '/', label: 'Overview', exact: true },
  { to: '/chores', label: 'All Chores' },
  { to: '/areas', label: 'Areas' },
]

/** Header with navigation around every screen but the login: a menu on a phone, a top bar from md up. */
export function Shell() {
  const queryClient = useQueryClient()
  const navigate = useNavigate()
  const router = useRouter()
  const [creating, setCreating] = useState(false)
  const wide = useRouterState({ select: (state) => state.matches.some((match) => match.staticData.wide) })
  const beside = useRouterState({ select: (state) => state.matches.some((match) => match.staticData.newChoreBeside) })
  const form = useRouterState({ select: (state) => state.matches.some((match) => match.staticData.form) })
  const pathname = useRouterState({ select: (state) => state.location.pathname })
  const fromLgUp = useMediaQuery(fromLg)
  const newChore = () => (beside && fromLgUp ? void navigate({ to: '/chores', search: { chore: 'new' } }) : setCreating(true))
  // Refetched on focus, so a session ended elsewhere sends this device to the login. The route guard does the
  // redirect, once; navigating from here would rerun on every location change until the Shell unmounts.
  const { data: session } = useQuery(sessionQuery)
  useEffect(() => {
    if (session === 'anonymous') void router.invalidate()
  }, [session, router])

  const logout = useMutation({
    mutationFn: () => api('/api/logout', 'post'),
    onSuccess: () => queryClient.setQueryData(sessionQuery.queryKey, 'anonymous'),
    onError: () => toast.error(unreachableMessage),
  })

  return (
    <div className="min-h-dvh">
      <header className="bg-brand text-white">
        {/* One width on every screen, so the navigation stays put; only the content below narrows */}
        <div className="mx-auto flex max-w-7xl items-center gap-2 px-4 py-2">
          <Link to="/" className="py-2 text-lg font-semibold">
            Chores
          </Link>
          <nav className="ml-auto hidden items-center gap-2 md:flex">
            {navLinks.map((link) => (
              <Link
                key={link.label}
                to={link.to}
                activeOptions={{ exact: link.exact ?? false }}
                className="rounded-lg px-3 py-2 hover:bg-white/10 data-[status=active]:bg-white/20"
              >
                {link.label}
              </Link>
            ))}
            <button type="button" onClick={newChore} className="rounded-lg bg-cream px-3 py-2 font-semibold text-brand hover:bg-white">
              New Chore
            </button>
            <button type="button" onClick={() => logout.mutate()} className="ml-2 px-2 py-2 text-sm text-white/70 hover:text-white hover:underline">
              Log out
            </button>
          </nav>
          <div className="ml-auto md:hidden">
            <Menu
              label="Menu"
              trigger={<MenuIcon />}
              entries={[
                ...navLinks.map((link) => ({
                  label: link.label,
                  onSelect: () => void navigate({ to: link.to }),
                  current: link.exact ? pathname === link.to : pathname.startsWith(link.to),
                })),
                { label: 'Log out', onSelect: () => logout.mutate() },
              ]}
            />
          </div>
        </div>
      </header>
      {/* Below md, room at the bottom so the floating New Chore button never covers the last row */}
      <main className={`mx-auto px-4 pt-6 pb-24 md:pb-6 ${wide ? 'max-w-7xl' : 'max-w-3xl'}`}>
        <Outlet />
      </main>
      {!form && (
        <button
          type="button"
          aria-label="New Chore"
          onClick={newChore}
          className="fixed right-4 bottom-6 flex size-14 items-center justify-center rounded-full bg-brand text-3xl text-white shadow-lg md:hidden"
        >
          +
        </button>
      )}
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

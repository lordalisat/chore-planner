import type { QueryClient } from '@tanstack/react-query'
import {
  createRootRouteWithContext,
  createRoute,
  createRouter,
  ErrorComponent,
  notFound,
  Outlet,
  redirect,
  type RouterHistory,
} from '@tanstack/react-router'
import { ApiError } from './api/client.ts'
import { areasQuery } from './areas.ts'
import { archivedChoresQuery, choreQuery, choresQuery } from './chores.ts'
import { AllChores } from './screens/AllChores.tsx'
import { Areas } from './screens/Areas.tsx'
import { ChoreNotFound, EditChore } from './screens/EditChore.tsx'
import { Login } from './screens/Login.tsx'
import { Overview } from './screens/Overview.tsx'
import { Shell } from './screens/Shell.tsx'
import { Unreachable } from './screens/Unreachable.tsx'
import { sessionQuery } from './session.ts'
import { Toasts } from './ui/Toasts.tsx'

const rootRoute = createRootRouteWithContext<{ queryClient: QueryClient }>()({
  component: () => (
    <>
      <Outlet />
      <Toasts />
    </>
  ),
  // fetch rejects with a TypeError when the server can't be reached; a 5xx is the proxy in front failing to reach it
  errorComponent: ({ error }) =>
    error instanceof TypeError || (error instanceof ApiError && error.status >= 500) ? (
      <Unreachable />
    ) : (
      <ErrorComponent error={error} />
    ),
})

const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/login',
  // Where to go once logged in; only a path in this app, never another site
  validateSearch: (search): { redirect?: string } => ({
    redirect: typeof search.redirect === 'string' && /^\/(?![/\\])/.test(search.redirect) ? search.redirect : undefined,
  }),
  beforeLoad: async ({ context }) => {
    if ((await context.queryClient.ensureQueryData(sessionQuery)) === 'authenticated') throw redirect({ to: '/' })
  },
  component: Login,
})

// Every screen but the login, inside the app shell
const authenticatedRoute = createRoute({
  getParentRoute: () => rootRoute,
  id: 'authenticated',
  beforeLoad: async ({ context, location }) => {
    if ((await context.queryClient.ensureQueryData(sessionQuery)) === 'anonymous') {
      throw redirect({ to: '/login', search: { redirect: location.href } })
    }
  },
  component: Shell,
})

const overviewRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/',
  loader: ({ context }) => context.queryClient.ensureQueryData(choresQuery),
  component: Overview,
})

const allChoresRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/chores',
  // A desktop setup screen: a wide table, with the Chore form beside it
  staticData: { wide: true, newChoreBeside: true },
  // Active Chores unless asked for the Archived ones, with the Chore open beside them, or a new one
  validateSearch: (search): { archived?: true; chore?: number | 'new' } => ({
    ...(search.archived === true && { archived: true }),
    ...((search.chore === 'new' || Number.isInteger(search.chore)) && { chore: search.chore as number | 'new' }),
  }),
  loaderDeps: ({ search: { archived } }) => ({ archived }),
  loader: ({ context, deps: { archived } }) =>
    context.queryClient.ensureQueryData(archived ? archivedChoresQuery : choresQuery),
  component: AllChores,
})

const choreRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/chores/$id',
  staticData: { form: true },
  params: { parse: ({ id }) => ({ id: Number(id) }), stringify: ({ id }) => ({ id: String(id) }) },
  loader: async ({ context, params: { id } }) => {
    if (!Number.isInteger(id)) throw notFound()
    try {
      await context.queryClient.ensureQueryData(choreQuery(id))
    } catch (error) {
      throw error instanceof ApiError && error.status === 404 ? notFound() : error
    }
  },
  component: EditChore,
  notFoundComponent: ChoreNotFound,
})

const areasRoute = createRoute({
  getParentRoute: () => authenticatedRoute,
  path: '/areas',
  loader: ({ context }) => context.queryClient.ensureQueryData(areasQuery),
  component: Areas,
})

const routeTree = rootRoute.addChildren([
  loginRoute,
  authenticatedRoute.addChildren([overviewRoute, allChoresRoute, choreRoute, areasRoute]),
])

/** The app's screens; `history` defaults to the browser's. */
export function createAppRouter(queryClient: QueryClient, history?: RouterHistory) {
  return createRouter({ routeTree, context: { queryClient }, history })
}

export type AppRouter = ReturnType<typeof createAppRouter>

declare module '@tanstack/react-router' {
  interface Register {
    router: AppRouter
  }
  interface StaticDataRouteOption {
    /** The Shell lets the screen use the full width of a desktop browser, not a centered column. */
    wide?: boolean
    /** From lg up, the Shell's New Chore opens the form beside this screen's list (`?chore=new`), not in a dialog. */
    newChoreBeside?: boolean
    /** A form screen: the phone's floating New Chore button would sit on its own buttons. */
    form?: boolean
  }
}

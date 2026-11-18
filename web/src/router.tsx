import type { QueryClient } from '@tanstack/react-query'
import {
  createRootRouteWithContext,
  createRoute,
  createRouter,
  ErrorComponent,
  Outlet,
  redirect,
  type RouterHistory,
} from '@tanstack/react-router'
import { ApiError } from './api/client.ts'
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
  component: Overview,
})

const routeTree = rootRoute.addChildren([loginRoute, authenticatedRoute.addChildren([overviewRoute])])

/** The app's screens; `history` defaults to the browser's. */
export function createAppRouter(queryClient: QueryClient, history?: RouterHistory) {
  return createRouter({ routeTree, context: { queryClient }, history })
}

export type AppRouter = ReturnType<typeof createAppRouter>

declare module '@tanstack/react-router' {
  interface Register {
    router: AppRouter
  }
}

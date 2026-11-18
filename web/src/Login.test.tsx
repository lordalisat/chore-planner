import { createMemoryHistory } from '@tanstack/react-router'
import { QueryClient } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, test, vi } from 'vitest'
import App from './App.tsx'
import { createAppRouter } from './router.tsx'

// The API as the browser sees it: each path answers with a status, or the server can't be reached
function stubApi(answers: Record<string, number | 'unreachable'>) {
  vi.stubGlobal(
    'fetch',
    vi.fn(async (input: RequestInfo | URL) => {
      const answer = answers[String(input)]
      if (answer === undefined || answer === 'unreachable') throw new TypeError('Failed to fetch')
      return new Response(null, { status: answer })
    }),
  )
}

function renderAt(url: string) {
  const queryClient = new QueryClient()
  const router = createAppRouter(queryClient, createMemoryHistory({ initialEntries: [url] }))
  render(<App queryClient={queryClient} router={router} />)
  return router
}

async function logIn(password: string) {
  await userEvent.type(await screen.findByLabelText('Household password'), password)
  await userEvent.click(screen.getByRole('button', { name: 'Log in' }))
}

test('a wrong password says so', async () => {
  stubApi({ '/api/session': 401, '/api/login': 401 })
  renderAt('/login')

  await logIn('wrong')

  expect(await screen.findByText('That password is not right.')).toHaveRole('alert')
})

test('logging in when the server is gone says it can’t be reached', async () => {
  stubApi({ '/api/session': 401, '/api/login': 'unreachable' })
  renderAt('/login')

  await logIn('chores')

  expect(await screen.findByText(/The server can't be reached right now/)).toHaveRole('alert')
})

test('logging out goes to the login, which returns to where you were', async () => {
  stubApi({ '/api/session': 204, '/api/logout': 204 })
  const router = renderAt('/')

  await userEvent.click(await screen.findByRole('button', { name: 'Log out' }))

  await screen.findByLabelText('Household password')
  expect(router.state.location.href).toBe('/login?redirect=%2F')
})

import { createMemoryHistory } from '@tanstack/react-router'
import { QueryClient } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, test } from 'vitest'
import App from '../App.tsx'
import type { Chore } from '../chores.ts'
import { createAppRouter } from '../router.tsx'
import { stubApi } from '../test-api.ts'

const laundry: Chore = {
  id: 7,
  kind: 'Recurring',
  name: 'Laundry',
  importance: 'High',
  effort: 30,
  area: null,
  interval: { count: 1, unit: 'Week' },
  preferredDay: 'Tuesday',
  season: { fromMonth: 4, toMonth: 10 },
  dueDate: '2027-03-16',
  urgency: 0,
  overdue: false,
  dormant: false,
  finished: false,
  lastDone: '2027-03-09',
  completionCount: 1,
  archivedOn: null,
}

const tap: Chore = {
  ...laundry,
  id: 9,
  kind: 'OneOff',
  name: 'Fix the tap',
  importance: 'Normal',
  effort: null,
  interval: null,
  preferredDay: null,
  season: null,
  dueDate: '2027-03-20',
  lastDone: null,
  completionCount: 0,
}

function renderAt(url: string) {
  const queryClient = new QueryClient()
  const router = createAppRouter(queryClient, createMemoryHistory({ initialEntries: [url] }))
  render(<App queryClient={queryClient} router={router} />)
  return router
}

test('tapping a Chore on the Overview opens it filled in', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [laundry], '/api/chores/7': laundry })
  const router = renderAt('/')

  await userEvent.click(await screen.findByRole('link', { name: /Laundry/ }))

  expect(await screen.findByRole('textbox', { name: 'Name' })).toHaveValue('Laundry')
  expect(router.state.location.pathname).toBe('/chores/7')
  expect(screen.getByRole('textbox', { name: 'Every' })).toHaveValue('1')
  expect(screen.getByRole('combobox', { name: 'Unit' })).toHaveTextContent('weeks')
  expect(screen.getByRole('combobox', { name: 'Preferred day' })).toHaveTextContent('Tuesday')
  expect(screen.getByRole('switch', { name: 'Seasonal' })).toBeChecked()
  expect(screen.getByRole('combobox', { name: 'Season from' })).toHaveTextContent('April')
  expect(screen.getByRole('combobox', { name: 'Season to' })).toHaveTextContent('October')
  expect(screen.getByRole('textbox', { name: 'Effort in minutes' })).toHaveValue('30')
  // Completions change Last done, not the form
  expect(screen.queryByRole('group', { name: 'Last done' })).not.toBeInTheDocument()
  expect(screen.queryByText('Last done')).not.toBeInTheDocument()
  expect(screen.queryByRole('button', { name: 'Save and add another' })).not.toBeInTheDocument()
})

test('saving an edited Chore sends its fields and returns to the Overview', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': [laundry],
    '/api/chores/7': (init) => Response.json(init?.method === 'PUT' ? { ...laundry, preferredDay: 'Saturday' } : laundry),
  })
  const router = renderAt('/chores/7')
  await userEvent.click(await screen.findByRole('combobox', { name: 'Preferred day' }))
  await userEvent.click(await screen.findByRole('option', { name: 'Saturday' }))

  await userEvent.click(screen.getByRole('button', { name: 'Save' }))

  await expect.poll(() => router.state.location.pathname).toBe('/')
  const put = fetch.mock.calls.find(([, init]) => init?.method === 'PUT')
  expect(JSON.parse(String(put?.[1]?.body))).toEqual({
    name: 'Laundry',
    interval: { count: 1, unit: 'Week' },
    preferredDay: 'Saturday',
    season: { fromMonth: 4, toMonth: 10 },
    importance: 'High',
    effort: 30,
  })
})

test('a One-off Chore stays one and keeps its Due Date', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': [tap],
    '/api/chores/9': (init) => Response.json(init?.method === 'PUT' ? { ...tap, name: 'Fix the kitchen tap' } : tap),
  })
  const router = renderAt('/chores/9')
  const name = await screen.findByRole('textbox', { name: 'Name' })

  expect(screen.queryByRole('radiogroup', { name: 'Kind' })).not.toBeInTheDocument()
  expect(screen.queryByText('Kind')).not.toBeInTheDocument()
  expect(screen.queryByRole('textbox', { name: 'Every' })).not.toBeInTheDocument()
  await userEvent.clear(name)
  await userEvent.type(name, 'Fix the kitchen tap{Enter}')

  await expect.poll(() => router.state.location.pathname).toBe('/')
  const put = fetch.mock.calls.find(([, init]) => init?.method === 'PUT')
  expect(JSON.parse(String(put?.[1]?.body))).toEqual({
    name: 'Fix the kitchen tap',
    dueDate: '2027-03-20',
    importance: 'Normal',
    effort: null,
  })
})

test('deleting a Chore from its screen returns to the Overview', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': [laundry],
    '/api/chores/7': (init) => Response.json(init?.method === 'DELETE' ? { archived: true } : laundry),
  })
  const router = renderAt('/chores/7')

  await userEvent.click(await screen.findByRole('button', { name: 'Delete' }))

  await expect.poll(() => router.state.location.pathname).toBe('/')
  expect(fetch.mock.calls.some(([, init]) => init?.method === 'DELETE')).toBe(true)
})

test('an unknown Chore is not found', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [], '/api/chores/8': 404 })
  renderAt('/chores/8')

  expect(await screen.findByText('That Chore is gone.')).toBeVisible()
})

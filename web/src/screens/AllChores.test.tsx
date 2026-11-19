import { QueryClient } from '@tanstack/react-query'
import { createMemoryHistory } from '@tanstack/react-router'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, test } from 'vitest'
import App from '../App.tsx'
import type { Chore } from '../chores.ts'
import { createAppRouter } from '../router.tsx'
import { stubApi } from '../test-api.ts'

function chore(id: number, name: string, details: Partial<Chore> = {}): Chore {
  return {
    id,
    kind: 'OneOff',
    name,
    importance: 'Normal',
    effort: null,
    area: null,
    interval: null,
    preferredDay: null,
    season: null,
    dueDate: null,
    urgency: null,
    overdue: false,
    dormant: false,
    finished: false,
    lastDone: null,
    completionCount: 0,
    archivedOn: null,
    ...details,
  }
}

const laundry = chore(1, 'Laundry', {
  kind: 'Recurring',
  importance: 'High',
  effort: 30,
  interval: { count: 1, unit: 'Week' },
  preferredDay: 'Tuesday',
  dueDate: '2027-03-16',
})
const lawn = chore(2, 'Mow the lawn', {
  kind: 'Recurring',
  importance: 'Low',
  effort: 45,
  interval: { count: 2, unit: 'Week' },
  season: { fromMonth: 4, toMonth: 10 },
  dueDate: '2027-04-06',
  dormant: true,
})
const mirror = chore(3, 'Hang the mirror', { effort: 15, dueDate: '2027-03-08', finished: true })
const gutters = chore(4, 'Clean the gutters', {
  kind: 'Recurring',
  effort: 120,
  interval: { count: 6, unit: 'Month' },
  preferredDay: 'FirstOfMonth',
  dueDate: '2027-09-01',
})

function renderAt(url: string) {
  const queryClient = new QueryClient()
  const router = createAppRouter(queryClient, createMemoryHistory({ initialEntries: [url] }))
  render(<App queryClient={queryClient} router={router} />)
  return router
}

const table = async () => screen.findByRole('table', { name: 'All Chores' })
const names = (t: HTMLElement) =>
  within(t)
    .getAllByRole('row')
    .slice(1)
    .map((row) => within(row).getAllByRole('cell')[0]!.querySelector('a')?.textContent)

test('All Chores is in the top navigation', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [laundry] })
  const router = renderAt('/')

  await userEvent.click(await screen.findByRole('link', { name: 'All Chores' }))

  expect(await table()).toBeVisible()
  expect(router.state.location.pathname).toBe('/chores')
})

test('every Chore that is not Archived is listed, Dormant and finished ones marked', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [laundry, lawn, mirror] })
  renderAt('/chores')

  const t = await table()
  expect(names(t)).toEqual(['Hang the mirror', 'Laundry', 'Mow the lawn'])
  const lawnRow = within(t).getByRole('row', { name: /Mow the lawn/ })
  expect(lawnRow).toHaveTextContent('Dormant')
  expect(lawnRow).toHaveTextContent('every 2 weeks')
  expect(lawnRow).toHaveTextContent('Apr – Oct')
  expect(lawnRow).toHaveTextContent('Low')
  expect(lawnRow).toHaveTextContent('45 min')
  expect(within(t).getByRole('row', { name: /Laundry/ })).toHaveTextContent('every week on Tuesday')
  expect(within(t).getByRole('row', { name: /Hang the mirror/ })).toHaveTextContent('Done')
})

// Ties go by Name, and blanks (a One-off Chore's Interval, a Season all year) stay at the bottom either way
test.each([
  ['Name', ['Clean the gutters', 'Hang the mirror', 'Laundry', 'Mow the lawn'], ['Mow the lawn', 'Laundry', 'Hang the mirror', 'Clean the gutters']],
  ['Kind', ['Hang the mirror', 'Clean the gutters', 'Laundry', 'Mow the lawn'], ['Clean the gutters', 'Laundry', 'Mow the lawn', 'Hang the mirror']],
  ['Interval', ['Laundry', 'Mow the lawn', 'Clean the gutters', 'Hang the mirror'], ['Clean the gutters', 'Mow the lawn', 'Laundry', 'Hang the mirror']],
  ['Season', ['Mow the lawn', 'Clean the gutters', 'Hang the mirror', 'Laundry'], ['Mow the lawn', 'Clean the gutters', 'Hang the mirror', 'Laundry']],
  ['Importance', ['Mow the lawn', 'Clean the gutters', 'Hang the mirror', 'Laundry'], ['Laundry', 'Clean the gutters', 'Hang the mirror', 'Mow the lawn']],
  ['Effort', ['Hang the mirror', 'Laundry', 'Mow the lawn', 'Clean the gutters'], ['Clean the gutters', 'Mow the lawn', 'Laundry', 'Hang the mirror']],
  ['Due', ['Hang the mirror', 'Laundry', 'Mow the lawn', 'Clean the gutters'], ['Clean the gutters', 'Mow the lawn', 'Laundry', 'Hang the mirror']],
])('sorting by %s, then the other way round', async (column, ascending, descending) => {
  stubApi({ '/api/session': 204, '/api/chores': [laundry, lawn, mirror, gutters] })
  renderAt('/chores')
  const t = await table()
  const header = within(t).getByRole('button', { name: column })

  // The list starts sorted by Name
  if (column !== 'Name') await userEvent.click(header)
  expect(names(t)).toEqual(ascending)
  expect(within(t).getByRole('columnheader', { name: column })).toHaveAttribute('aria-sort', 'ascending')

  await userEvent.click(header)
  expect(names(t)).toEqual(descending)
  expect(within(t).getByRole('columnheader', { name: column })).toHaveAttribute('aria-sort', 'descending')
})

test('Archived Chores show their archive date and Restore, and cannot be edited', async () => {
  const bins = chore(5, 'Take out the bins', { kind: 'Recurring', archivedOn: '2027-03-08', completionCount: 3 })
  let archived = [bins]
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': () => Response.json(archived.length ? [laundry] : [laundry, { ...bins, archivedOn: null }]),
    '/api/chores?archived=true': () => Response.json(archived),
    '/api/chores/5/restore': () => {
      archived = []
      return Response.json({ ...bins, archivedOn: null })
    },
  })
  renderAt('/chores')
  await table()

  await userEvent.click(screen.getByRole('link', { name: 'Archived' }))

  const t = await screen.findByRole('table', { name: 'Archived Chores' })
  const row = within(t).getByRole('row', { name: /Take out the bins/ })
  expect(row).toHaveTextContent('8 Mar 2027')
  expect(within(t).queryByRole('link')).not.toBeInTheDocument()
  expect(screen.queryByText('Laundry')).not.toBeInTheDocument()

  await userEvent.click(within(row).getByRole('button', { name: 'Restore' }))

  expect(await screen.findByText('Nothing is Archived.')).toBeVisible()
  expect(fetch.mock.calls.some(([url, init]) => url === '/api/chores/5/restore' && init?.method === 'POST')).toBe(true)
  await userEvent.click(screen.getByRole('link', { name: 'Active' }))
  expect(within(await table()).getByRole('row', { name: /Take out the bins/ })).toBeVisible()
})

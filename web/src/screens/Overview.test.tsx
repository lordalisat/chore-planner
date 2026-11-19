import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { Suspense } from 'react'
import { expect, test } from 'vitest'
import type { Chore } from '../chores.ts'
import { stubApi } from '../test-api.ts'
import { Toasts } from '../ui/Toasts.tsx'
import { Overview } from './Overview.tsx'

function chore(name: string, details: Partial<Chore>): Chore {
  return {
    id: name.length,
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
    ...details,
  }
}

const laundry = chore('Laundry', {
  id: 7,
  kind: 'Recurring',
  interval: { count: 1, unit: 'Week' },
  dueDate: '2027-03-09',
  urgency: 1.25,
  overdue: true,
})

function renderOverview() {
  render(
    <QueryClientProvider client={new QueryClient()}>
      <Suspense>
        <Overview />
      </Suspense>
      <Toasts />
    </QueryClientProvider>,
  )
}

// What the Overview asked the API to do, leaving out the reads
const commands = (fetch: ReturnType<typeof stubApi>) =>
  fetch.mock.calls
    .filter(([, init]) => init?.method !== 'GET')
    .map(([path, init]) => ({ path: String(path), method: init?.method, body: init?.body && JSON.parse(String(init.body)) }))

test('One-off Chores without a Due Date wait under Anytime; finished ones are gone', async () => {
  stubApi({
    '/api/chores': [
      chore('Fix leaking tap', { urgency: 0.5 }),
      chore('Laundry', { kind: 'Recurring', dueDate: '2027-03-16', urgency: 0 }),
      chore('Hang the mirror', { dueDate: '2027-03-08', finished: true }),
      chore('Book the plumber', { dueDate: '2027-03-12', urgency: 0.5 }),
      chore('Take out the bins', { kind: 'Recurring', dueDate: '2027-03-09', urgency: 1.25, overdue: true }),
    ],
  })

  renderOverview()

  await screen.findByText('Laundry')
  // Leaving out the toasts' list
  const [main, anytime] = screen.getAllByRole('list').filter((list) => list.tagName === 'UL')
  expect(within(main!).getAllByRole('listitem').map((row) => row.firstChild?.textContent)).toEqual([
    'Take out the bins',
    'Book the plumber',
    'Laundry',
  ])
  expect(screen.getByRole('heading', { name: 'Anytime' })).toBeVisible()
  expect(within(anytime!).getAllByRole('listitem').map((row) => row.firstChild?.textContent)).toEqual(['Fix leaking tap'])
  expect(screen.queryByText('Hang the mirror')).not.toBeInTheDocument()
})

test('Done records a Completion today for the Due Date shown, and Undo takes it back', async () => {
  const fetch = stubApi({
    '/api/chores': [laundry],
    '/api/chores/7/complete': { completionId: 31, chore: { ...laundry, dueDate: '2027-03-16', lastDone: '2027-03-10' } },
    '/api/chores/7/completions/31': laundry,
  })
  renderOverview()

  await userEvent.click(await screen.findByRole('button', { name: 'Done' }))
  await userEvent.click(await screen.findByRole('button', { name: 'Undo' }))

  await expect.poll(() => commands(fetch)).toEqual([
    { path: '/api/chores/7/complete', method: 'POST', body: { dueDate: '2027-03-09' } },
    { path: '/api/chores/7/completions/31', method: 'DELETE', body: undefined },
  ])
})

test('when someone else did it first, the list is fetched again and says so', async () => {
  const fetch = stubApi({ '/api/chores': [laundry], '/api/chores/7/complete': 409 })
  renderOverview()

  await userEvent.click(await screen.findByRole('button', { name: 'Done' }))

  expect(await screen.findByText('Already done by someone else')).toBeInTheDocument()
  await expect.poll(() => fetch.mock.calls.filter(([path]) => path === '/api/chores')).toHaveLength(2)
})

test('Done on… records the Completion on the day picked', async () => {
  const fetch = stubApi({
    '/api/chores': [laundry],
    '/api/chores/7/complete': { completionId: 31, chore: laundry },
  })
  renderOverview()

  await userEvent.click(await screen.findByRole('button', { name: 'More for Laundry' }))
  await userEvent.click(await screen.findByRole('menuitem', { name: 'Done on…' }))
  const date = await screen.findByRole('textbox', { name: 'Done on' })
  await userEvent.clear(date)
  await userEvent.type(date, '08/03/2027')
  await userEvent.click(screen.getByRole('button', { name: 'Save' }))

  await expect.poll(() => commands(fetch)).toEqual([
    { path: '/api/chores/7/complete', method: 'POST', body: { dueDate: '2027-03-09', date: '2027-03-08' } },
  ])
})

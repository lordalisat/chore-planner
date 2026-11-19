import { QueryClient } from '@tanstack/react-query'
import { createMemoryHistory } from '@tanstack/react-router'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, test } from 'vitest'
import App from '../App.tsx'
import type { Chore } from '../chores.ts'
import { createAppRouter } from '../router.tsx'
import { stubApi } from '../test-api.ts'

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
    canSkip: details.kind === 'Recurring',
    lastDone: null,
    completionCount: 0,
    archivedOn: null,
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
  const queryClient = new QueryClient()
  const router = createAppRouter(queryClient, createMemoryHistory())
  render(<App queryClient={queryClient} router={router} />)
  return router
}

// What the Overview asked the API to do, leaving out the reads
const commands = (fetch: ReturnType<typeof stubApi>) =>
  fetch.mock.calls
    .filter(([, init]) => init?.method !== 'GET')
    .map(([path, init]) => ({ path: String(path), method: init?.method, body: init?.body && JSON.parse(String(init.body)) }))

test('One-off Chores without a Due Date wait under Anytime; finished ones are gone', async () => {
  stubApi({
    '/api/session': 204,
    '/api/chores': [
      chore('Fix leaking tap', { urgency: 0.5 }),
      chore('Laundry', { kind: 'Recurring', dueDate: '2027-03-16', urgency: 0 }),
      chore('Hang the mirror', { dueDate: '2027-03-08', finished: true }),
      chore('Book the plumber', { dueDate: '2027-03-12', urgency: 0.5 }),
      chore('Take out the bins', { kind: 'Recurring', dueDate: '2027-03-09', urgency: 1.25, overdue: true }),
    ],
  })

  renderOverview()

  await screen.findByText('Take out the bins')
  // Leaving out the toasts' list
  const [main, anytime] = screen.getAllByRole('list').filter((list) => list.tagName === 'UL')
  expect(within(main!).getAllByRole('listitem').map((row) => row.querySelector('span')?.textContent)).toEqual([
    'Take out the bins',
    'Book the plumber',
  ])
  expect(screen.getByRole('heading', { name: 'Anytime' })).toBeVisible()
  expect(within(anytime!).getAllByRole('listitem').map((row) => row.querySelector('span')?.textContent)).toEqual(['Fix leaking tap'])
  expect(screen.queryByText('Hang the mirror')).not.toBeInTheDocument()
})

test('a row shows the Area its Chore belongs to', async () => {
  stubApi({
    '/api/session': 204,
    '/api/chores': [
      chore('Clean the fridge', { kind: 'Recurring', dueDate: '2027-03-16', urgency: 0.25, area: { id: 3, name: 'Kitchen' } }),
    ],
  })

  renderOverview()

  const row = (await screen.findByText('Clean the fridge')).closest('li')!
  expect(within(row).getByText('Kitchen')).toBeVisible()
})

test('Done records a Completion today for the Due Date shown, and Undo takes it back', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': [laundry],
    '/api/chores/7/complete': { completionId: 31, chore: { ...laundry, dueDate: '2027-03-16', lastDone: '2027-03-10' } },
    '/api/chores/7/completions/31': laundry,
  })
  const router = renderOverview()

  await userEvent.click(await screen.findByRole('button', { name: 'Mark Laundry done' }))
  await userEvent.click(await screen.findByRole('button', { name: 'Undo' }))

  await expect.poll(() => commands(fetch)).toEqual([
    { path: '/api/chores/7/complete', method: 'POST', body: { dueDate: '2027-03-09' } },
    { path: '/api/chores/7/completions/31', method: 'DELETE', body: undefined },
  ])
  // Done never leaves the Overview
  expect(router.state.location.pathname).toBe('/')
})

test('when someone else did it first, the list is fetched again and says so', async () => {
  const fetch = stubApi({ '/api/session': 204, '/api/chores': [laundry], '/api/chores/7/complete': 409 })
  renderOverview()

  await userEvent.click(await screen.findByRole('button', { name: 'Mark Laundry done' }))

  expect(await screen.findByText('Already done by someone else')).toBeInTheDocument()
  await expect.poll(() => fetch.mock.calls.filter(([path]) => path === '/api/chores')).toHaveLength(2)
})

test('Done on… records the Completion on the day picked', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': [laundry],
    '/api/chores/7/complete': { completionId: 31, chore: laundry },
  })
  const router = renderOverview()

  await userEvent.click(await screen.findByRole('button', { name: /^Laundry/, expanded: false }))
  await userEvent.click(await screen.findByRole('button', { name: 'Done on…' }))
  const date = await screen.findByRole('textbox', { name: 'Done on' })
  await userEvent.clear(date)
  await userEvent.type(date, '08/03/2027')
  await userEvent.click(screen.getByRole('button', { name: 'Save' }))

  await expect.poll(() => commands(fetch)).toEqual([
    { path: '/api/chores/7/complete', method: 'POST', body: { dueDate: '2027-03-09', date: '2027-03-08' } },
  ])
  expect(router.state.location.pathname).toBe('/')
})

test('Skip moves a Recurring Chore on for the Due Date shown, and Undo takes it back', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': [laundry],
    '/api/chores/7/skip': { completionId: 32, chore: { ...laundry, dueDate: '2027-03-16' } },
    '/api/chores/7/completions/32': laundry,
  })
  renderOverview()

  await userEvent.click(await screen.findByRole('button', { name: /^Laundry/, expanded: false }))
  await userEvent.click(await screen.findByRole('button', { name: 'Skip' }))
  expect(await screen.findByText('Skipped: Laundry')).toBeInTheDocument()
  await userEvent.click(await screen.findByRole('button', { name: 'Undo' }))

  await expect.poll(() => commands(fetch)).toEqual([
    { path: '/api/chores/7/skip', method: 'POST', body: { dueDate: '2027-03-09' } },
    { path: '/api/chores/7/completions/32', method: 'DELETE', body: undefined },
  ])
})

test('a One-off Chore cannot be skipped', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [chore('Book the plumber', { dueDate: '2027-03-12', urgency: 0.5 })] })
  renderOverview()

  await userEvent.click(await screen.findByRole('button', { name: /^Book the plumber/, expanded: false }))

  expect(await screen.findByRole('button', { name: 'Postpone…' })).toBeInTheDocument()
  expect(screen.queryByRole('button', { name: 'Skip' })).not.toBeInTheDocument()
})

test('Postpone… sets the Due Date to the day picked', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': [laundry],
    '/api/chores/7/postpone': { ...laundry, dueDate: '2027-03-13' },
  })
  renderOverview()

  await userEvent.click(await screen.findByRole('button', { name: /^Laundry/, expanded: false }))
  await userEvent.click(await screen.findByRole('button', { name: 'Postpone…' }))
  await userEvent.type(await screen.findByRole('textbox', { name: 'Postpone to' }), '13/03/2027')
  await userEvent.click(screen.getByRole('button', { name: 'Save' }))

  await expect.poll(() => commands(fetch)).toEqual([
    { path: '/api/chores/7/postpone', method: 'POST', body: { dueDate: '2027-03-09', to: '2027-03-13' } },
  ])
  await expect.poll(() => screen.queryByRole('textbox', { name: 'Postpone to' })).toBeNull()
})

test('Postpone… to a day that is not after today says so at the date', async () => {
  stubApi({
    '/api/session': 204,
    '/api/chores': [laundry],
    '/api/chores/7/postpone': Response.json(
      { errors: { to: ['A Chore can only be postponed to a day after today.'] } },
      { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
    ),
  })
  renderOverview()

  await userEvent.click(await screen.findByRole('button', { name: /^Laundry/, expanded: false }))
  await userEvent.click(await screen.findByRole('button', { name: 'Postpone…' }))
  await userEvent.type(await screen.findByRole('textbox', { name: 'Postpone to' }), '01/03/2027')
  await userEvent.click(screen.getByRole('button', { name: 'Save' }))

  expect(await screen.findByText('A Chore can only be postponed to a day after today.')).toBeInTheDocument()
})

test('tapping a row shows its actions and opens no screen; Edit opens the Chore', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [laundry], '/api/chores/7': laundry, '/api/areas': [] })
  const router = renderOverview()

  const row = (await screen.findByText('Laundry')).closest('li')!
  expect(within(row).queryByRole('link')).toBeNull()

  await userEvent.click(screen.getByRole('button', { name: /^Laundry/, expanded: false }))
  await userEvent.click(await screen.findByRole('button', { name: 'Edit' }))

  await expect.poll(() => router.state.location.pathname).toBe('/chores/7')
})

test('tapping an expanded row again folds its actions away', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [laundry] })
  const router = renderOverview()

  await userEvent.click(await screen.findByRole('button', { name: /^Laundry/, expanded: false }))
  expect(await screen.findByRole('button', { name: 'Postpone…' })).toBeVisible()
  expect(router.state.location.pathname).toBe('/')

  await userEvent.click(screen.getByRole('button', { name: /^Laundry/, expanded: true }))
  expect(screen.queryByRole('button', { name: 'Postpone…' })).not.toBeInTheDocument()
})

test('Chores equally urgent go by Due Date, earliest first', async () => {
  stubApi({
    '/api/session': 204,
    '/api/chores': [
      chore('Laundry', { kind: 'Recurring', dueDate: '2027-03-12', urgency: 0.5 }),
      chore('Clean vacuum water', { kind: 'Recurring', dueDate: '2027-03-11', urgency: 0.5 }),
    ],
  })
  renderOverview()

  await screen.findByText('Laundry')
  const [main] = screen.getAllByRole('list').filter((list) => list.tagName === 'UL')
  expect(within(main!).getAllByRole('listitem').map((row) => row.querySelector('span')?.textContent)).toEqual([
    'Clean vacuum water',
    'Laundry',
  ])
})

test('each row is coloured by how urgent its Chore is', async () => {
  stubApi({
    '/api/session': 204,
    '/api/chores': [
      chore('Not yet', { dueDate: '2027-03-30', urgency: 0 }),
      chore('Coming up', { dueDate: '2027-03-11', urgency: 0.5 }),
      chore('Due', { dueDate: '2027-03-09', urgency: 1.25 }),
      chore('Pressing', { dueDate: '2027-03-05', urgency: 2.25, overdue: true }),
    ],
  })
  renderOverview()

  const band = async (name: string) => (await screen.findByText(name)).closest('li')!.dataset.urgency
  await userEvent.click(await screen.findByRole('button', { name: /^Later/ }))
  expect(await band('Not yet')).toBe('none')
  expect(await band('Coming up')).toBe('soon')
  expect(await band('Due')).toBe('due')
  expect(await band('Pressing')).toBe('pressing')
})

test('Chores not urgent yet wait under Later, folded away, below Anytime', async () => {
  stubApi({
    '/api/session': 204,
    '/api/chores': [
      chore('Laundry', { kind: 'Recurring', dueDate: '2027-03-20', urgency: 0 }),
      chore('Fix leaking tap', { urgency: 0.5 }),
      chore('Clean vacuum water', { kind: 'Recurring', dueDate: '2027-03-15', urgency: 0 }),
      chore('Take out the bins', { kind: 'Recurring', dueDate: '2027-03-09', urgency: 1.25 }),
    ],
  })
  renderOverview()

  await screen.findByText('Take out the bins')
  const headings = screen.getAllByRole('heading', { level: 2 }).map((heading) => heading.textContent)
  expect(headings).toEqual(['Anytime', expect.stringMatching(/Later \(2\)/)])
  const later = screen.getByRole('button', { name: 'Later (2)' })
  expect(later).toHaveAttribute('aria-expanded', 'false')
  expect(screen.queryByText('Laundry')).not.toBeInTheDocument()

  await userEvent.click(later)

  const section = screen.getByRole('region', { name: /^Later/ })
  expect(within(section).getAllByRole('listitem').map((row) => row.querySelector('span')?.textContent)).toEqual([
    'Clean vacuum water',
    'Laundry',
  ])
})

test('with only Later Chores, the Overview says nothing needs doing yet', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [chore('Laundry', { kind: 'Recurring', dueDate: '2027-03-20', urgency: 0 })] })
  renderOverview()

  expect(await screen.findByText('Nothing needs doing yet.')).toBeVisible()
  expect(screen.getByRole('button', { name: 'Later (1)' })).toBeVisible()
})

import { QueryClient } from '@tanstack/react-query'
import { createMemoryHistory } from '@tanstack/react-router'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { beforeEach, expect, test, vi } from 'vitest'
import App from '../App.tsx'
import type { Chore } from '../chores.ts'
import { createAppRouter } from '../router.tsx'
import { choreFixture, stubApi } from '../test-api.ts'

const chore = (id: number, name: string, overrides: Partial<Chore> = {}): Chore => choreFixture({ ...overrides, id, name })

const laundry = chore(1, 'Laundry', {
  kind: 'Recurring',
  area: { id: 1, name: 'Utility room' },
  importance: 'High',
  effort: 30,
  interval: { count: 1, unit: 'Week' },
  preferredDay: 'Tuesday',
  dueDate: '2027-03-16',
  urgency: 1.5,
})
const lawn = chore(2, 'Mow the lawn', {
  kind: 'Recurring',
  area: { id: 2, name: 'Garden' },
  importance: 'Low',
  effort: 45,
  interval: { count: 2, unit: 'Week' },
  season: { fromMonth: 4, toMonth: 10 },
  dueDate: '2027-04-06',
  dormant: true,
})
const mirror = chore(3, 'Hang the mirror', { area: { id: 3, name: 'Hall' }, effort: 15, dueDate: '2027-03-08', finished: true })
const gutters = chore(4, 'Clean the gutters', {
  kind: 'Recurring',
  effort: 120,
  interval: { count: 6, unit: 'Month' },
  preferredDay: 'FirstOfMonth',
  dueDate: '2027-09-01',
  urgency: 0,
})

// jsdom has no layout, so the lg breakpoint is whatever a test says; from lg up unless it says otherwise
function screenFromLg(fromLg: boolean) {
  vi.stubGlobal('matchMedia', (query: string) => ({
    matches: fromLg && query === '(min-width: 64rem)',
    media: query,
    addEventListener: () => {},
    removeEventListener: () => {},
  }))
}
beforeEach(() => screenFromLg(true))

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
    .map((row) => within(row).getAllByRole('cell')[0]!.querySelector('button')?.textContent)

test('All Chores is in the top navigation', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [laundry] })
  const router = renderAt('/')

  await userEvent.click(await screen.findByRole('link', { name: 'All Chores' }))

  expect(await table()).toBeVisible()
  expect(router.state.location.pathname).toBe('/chores')
})

test('the Overview is in the top navigation too, and the screen you are on is marked', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [laundry] })
  const router = renderAt('/chores')

  expect(await screen.findByRole('link', { name: 'All Chores' })).toHaveAttribute('aria-current', 'page')
  await userEvent.click(screen.getByRole('link', { name: 'Overview' }))

  await expect.poll(() => router.state.location.pathname).toBe('/')
  expect(screen.getByRole('link', { name: 'Overview' })).toHaveAttribute('aria-current', 'page')
  expect(screen.getByRole('link', { name: 'All Chores' })).not.toHaveAttribute('aria-current')
})

test('the filter marks only the one shown', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [laundry], '/api/chores?archived=true': [] })
  const filter = async () => within(await screen.findByRole('navigation', { name: 'Filter' }))
  const router = renderAt('/chores?archived=true')

  expect((await filter()).getByRole('link', { name: 'Archived' })).toHaveAttribute('aria-current', 'page')
  expect((await filter()).getByRole('link', { name: 'Active' })).not.toHaveAttribute('aria-current')

  await router.navigate({ to: '/chores', search: { chore: 1 } })
  await expect.poll(async () => (await filter()).getByRole('link', { name: 'Active' })).toHaveAttribute('aria-current', 'page')
  expect((await filter()).getByRole('link', { name: 'Archived' })).not.toHaveAttribute('aria-current')
})

const columns = ['Name', 'Area', 'Kind', 'Interval', 'Season', 'Importance', 'Effort', 'Due', 'Urgency']

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
  expect(lawnRow).toHaveTextContent('Garden')
  expect(within(t).getByRole('row', { name: /Laundry/ })).toHaveTextContent('every week on Tuesday')
  expect(within(t).getByRole('columnheader', { name: /Urgency/ })).toBeVisible()
  expect(within(t).getByRole('row', { name: /Laundry/ })).toHaveTextContent('1.5')
  // Finished: no Due Date left to meet, so only the mark
  expect(within(t).getByRole('row', { name: /Hang the mirror/ })).toHaveTextContent('Done')
  const due = columns.indexOf('Due')
  expect(within(within(t).getByRole('row', { name: /Hang the mirror/ })).getAllByRole('cell')[due]).toBeEmptyDOMElement()
})

// Ties go by Name, and blanks (a One-off Chore's Interval, a Season all year) stay at the bottom either way. Importance
// and Urgency start highest first, the others lowest first.
test.each([
  ['Name', ['Clean the gutters', 'Hang the mirror', 'Laundry', 'Mow the lawn'], ['Mow the lawn', 'Laundry', 'Hang the mirror', 'Clean the gutters']],
  ['Kind', ['Hang the mirror', 'Clean the gutters', 'Laundry', 'Mow the lawn'], ['Clean the gutters', 'Laundry', 'Mow the lawn', 'Hang the mirror']],
  ['Interval', ['Laundry', 'Mow the lawn', 'Clean the gutters', 'Hang the mirror'], ['Clean the gutters', 'Mow the lawn', 'Laundry', 'Hang the mirror']],
  ['Season', ['Mow the lawn', 'Clean the gutters', 'Hang the mirror', 'Laundry'], ['Mow the lawn', 'Clean the gutters', 'Hang the mirror', 'Laundry']],
  ['Importance', ['Laundry', 'Clean the gutters', 'Hang the mirror', 'Mow the lawn'], ['Mow the lawn', 'Clean the gutters', 'Hang the mirror', 'Laundry']],
  ['Effort', ['Hang the mirror', 'Laundry', 'Mow the lawn', 'Clean the gutters'], ['Clean the gutters', 'Mow the lawn', 'Laundry', 'Hang the mirror']],
  ['Due', ['Hang the mirror', 'Laundry', 'Mow the lawn', 'Clean the gutters'], ['Clean the gutters', 'Mow the lawn', 'Laundry', 'Hang the mirror']],
  ['Area', ['Mow the lawn', 'Hang the mirror', 'Laundry', 'Clean the gutters'], ['Laundry', 'Hang the mirror', 'Mow the lawn', 'Clean the gutters']],
  ['Urgency', ['Laundry', 'Clean the gutters', 'Hang the mirror', 'Mow the lawn'], ['Clean the gutters', 'Laundry', 'Hang the mirror', 'Mow the lawn']],
])('sorting by %s, then the other way round', async (column, first, second) => {
  stubApi({ '/api/session': 204, '/api/chores': [laundry, lawn, mirror, gutters] })
  renderAt('/chores')
  const t = await table()
  const header = within(t).getByRole('button', { name: column })

  // The list starts sorted by Name
  const [firstSort, secondSort] = column === 'Importance' || column === 'Urgency' ? ['descending', 'ascending'] : ['ascending', 'descending']
  if (column !== 'Name') await userEvent.click(header)
  expect(names(t)).toEqual(first)
  expect(within(t).getByRole('columnheader', { name: column })).toHaveAttribute('aria-sort', firstSort)

  await userEvent.click(header)
  expect(names(t)).toEqual(second)
  expect(within(t).getByRole('columnheader', { name: column })).toHaveAttribute('aria-sort', secondSort)
})

test('Intervals sort by unit, then count, since a month has no fixed number of days', async () => {
  const plants = chore(5, 'Water the plants', { kind: 'Recurring', interval: { count: 40, unit: 'Day' } })
  const sheets = chore(6, 'Change the sheets', { kind: 'Recurring', interval: { count: 1, unit: 'Month' } })
  stubApi({ '/api/session': 204, '/api/chores': [sheets, laundry, plants] })
  renderAt('/chores')
  const t = await table()

  await userEvent.click(within(t).getByRole('button', { name: 'Interval' }))

  expect(names(t)).toEqual(['Water the plants', 'Laundry', 'Change the sheets'])
})

test('clicking through Chores opens each beside the list, and saving one stays on the list', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': [laundry, lawn],
    '/api/chores/2': (init) => Response.json(init?.method === 'PUT' ? { ...lawn, effort: 50 } : lawn),
  })
  const router = renderAt('/chores')
  const t = await table()

  await userEvent.click(within(t).getByRole('row', { name: /Laundry/ }))
  expect(await screen.findByRole('textbox', { name: 'Name' })).toHaveValue('Laundry')
  expect(router.state.location.search).toEqual({ chore: 1 })

  // A Dormant Chore can be edited too
  await userEvent.click(within(t).getByRole('row', { name: /Mow the lawn/ }))
  const effort = await screen.findByRole('textbox', { name: 'Effort in minutes' })
  expect(screen.getByRole('textbox', { name: 'Name' })).toHaveValue('Mow the lawn')
  await userEvent.clear(effort)
  await userEvent.type(effort, '50')
  await userEvent.click(screen.getByRole('button', { name: 'Save' }))

  await expect.poll(() => router.state.location.search).toEqual({})
  expect(router.state.location.pathname).toBe('/chores')
  expect(screen.queryByRole('textbox', { name: 'Name' })).not.toBeInTheDocument()
  const put = fetch.mock.calls.find(([, init]) => init?.method === 'PUT')
  expect(JSON.parse(String(put?.[1]?.body))).toMatchObject({ name: 'Mow the lawn', effort: 50 })
})

test('below lg a Chore opens on its own screen, and new ones come from the Shell', async () => {
  screenFromLg(false)
  stubApi({ '/api/session': 204, '/api/chores': [laundry, lawn], '/api/chores/1': laundry })
  const router = renderAt('/chores')

  await userEvent.click(within(await table()).getByRole('row', { name: /Laundry/ }))

  expect(await screen.findByRole('textbox', { name: 'Name' })).toHaveValue('Laundry')
  expect(router.state.location.pathname).toBe('/chores/1')
  expect(screen.queryByRole('table', { name: 'All Chores' })).not.toBeInTheDocument()
  await router.navigate({ to: '/chores' })
  await table()
  expect(screen.queryByRole('link', { name: 'New Chore' })).not.toBeInTheDocument()
})

test('clicking anywhere on a row opens its Chore, not only on its name', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [laundry, lawn] })
  const router = renderAt('/chores')

  const row = within(await table()).getByRole('row', { name: /Mow the lawn/ })
  await userEvent.click(within(row).getAllByRole('cell')[2]!)

  await expect.poll(() => router.state.location.search).toEqual({ chore: lawn.id })
  // The row is the one thing to click; its name is no separate link
  expect(within(row).queryByRole('link')).not.toBeInTheDocument()
})

test('a row opens its Chore from the keyboard too', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [laundry, lawn] })
  const router = renderAt('/chores')

  within(await table()).getByRole('button', { name: 'Mow the lawn' }).focus()
  await userEvent.keyboard('{Enter}')

  await expect.poll(() => router.state.location.search).toEqual({ chore: lawn.id })
})

test('the selected Chore is in the URL, so reloading keeps it open', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [laundry, lawn] })
  renderAt('/chores?chore=2')

  expect(await screen.findByRole('textbox', { name: 'Name' })).toHaveValue('Mow the lawn')
  expect(screen.getByRole('table', { name: 'All Chores' })).toBeInTheDocument()
})

test('new Chores are entered beside the list, one after another', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': [laundry],
    '/api/chores/one-off': (init) => Response.json({ ...mirror, name: JSON.parse(String(init?.body)).name }),
  })
  const router = renderAt('/chores')
  await table()

  // The header's New Chore is the only one; here it opens the form beside the list, not a dialog
  expect(screen.queryByRole('link', { name: 'New Chore' })).not.toBeInTheDocument()
  await userEvent.click(screen.getAllByRole('button', { name: 'New Chore' })[0]!)
  expect(router.state.location.search).toEqual({ chore: 'new' })
  expect(screen.queryByRole('dialog')).not.toBeInTheDocument()
  await userEvent.click(await screen.findByRole('radio', { name: 'One-off' }))
  for (const name of ['Fix the tap', 'Hang the mirror']) {
    await userEvent.type(screen.getByRole('textbox', { name: 'Name' }), name)
    await userEvent.click(screen.getByRole('button', { name: 'Save and add another' }))
    await expect.poll(() => screen.getByRole('textbox', { name: 'Name' })).toHaveValue('')
  }

  const posted = fetch.mock.calls.filter(([url]) => url === '/api/chores/one-off')
  expect(posted.map(([, init]) => JSON.parse(String(init?.body)).name)).toEqual(['Fix the tap', 'Hang the mirror'])
  expect(router.state.location.search).toEqual({ chore: 'new' })
})

test('Archived Chores show their archive date and Restore, and cannot be edited', async () => {
  const bins = chore(5, 'Take out the bins', {
    kind: 'Recurring',
    area: { id: 4, name: 'Driveway' },
    archivedOn: '2027-03-08',
    completionCount: 3,
  })
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
  expect(row).toHaveTextContent('Driveway')
  expect(within(t).queryByRole('link')).not.toBeInTheDocument()
  // A phone gets a plain list instead
  const item = within(screen.getByRole('list', { name: 'Archived Chores' })).getByRole('listitem')
  expect(item).toHaveTextContent('Take out the bins')
  expect(item).toHaveTextContent('8 Mar 2027')
  expect(within(item).getByRole('button', { name: 'Restore' })).toBeInTheDocument()
  expect(screen.queryByText('Laundry')).not.toBeInTheDocument()

  await userEvent.click(within(row).getByRole('button', { name: 'Restore' }))

  expect(await screen.findByText('Nothing is Archived.')).toBeVisible()
  expect(fetch.mock.calls.some(([url, init]) => url === '/api/chores/5/restore' && init?.method === 'POST')).toBe(true)
  await userEvent.click(screen.getByRole('link', { name: 'Active' }))
  expect(within(await table()).getByRole('row', { name: /Take out the bins/ })).toBeVisible()
})

test('Restore says so when an active Chore has taken the Name', async () => {
  const bins = chore(5, 'Take out the bins', { kind: 'Recurring', archivedOn: '2027-03-08', completionCount: 3 })
  stubApi({
    '/api/session': 204,
    '/api/chores?archived=true': [bins],
    '/api/chores/5/restore': Response.json(
      { errors: { name: ['There is already a Chore called take out the bins.'] } },
      { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
    ),
  })
  renderAt('/chores?archived=true')

  const t = await screen.findByRole('table', { name: 'Archived Chores' })
  await userEvent.click(within(t).getByRole('button', { name: 'Restore' }))

  expect(await screen.findByText('There is already a Chore called take out the bins.')).toBeVisible()
})

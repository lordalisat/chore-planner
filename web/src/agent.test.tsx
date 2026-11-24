import { createMemoryHistory } from '@tanstack/react-router'
import { QueryClient } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import { beforeEach, expect, test } from 'vitest'
import App from './App.tsx'
import { createAppRouter } from './router.tsx'
import { choreFixture, stubApi } from './test-api.ts'
import { createAgent } from './ui/agent.ts'

// One bridge for the file: it has no way to be destroyed, and remembers what earlier tests removed as off the page
let approve = false
const agent = createAgent({ allow: (step) => !step.confirm || approve })
beforeEach(() => {
  approve = false
})

function renderAt(url: string) {
  const queryClient = new QueryClient()
  const router = createAppRouter(queryClient, createMemoryHistory({ initialEntries: [url] }))
  render(<App queryClient={queryClient} router={router} />)
  return router
}

const onPage = () => agent.snapshot().filter((entry) => !entry.offPage && entry.component !== 'more')

/** Every control on the page has a name, and each of `labels` is one of them. */
function expectNamed(...labels: string[]) {
  const entries = onPage()
  for (const entry of entries) expect(entry.label, `${entry.component} ${entry.ref} has no name`).toBeTruthy()
  const names = entries.map((entry) => entry.label)
  for (const label of labels) expect(names, `${label} is missing from the snapshot`).toContain(label)
}

// Where a name is shared with a control jsdom can't hide, such as the phone layout's, a step names the control by ref
const refOf = (label: string) => onPage().find((entry) => entry.label === label)!.ref

async function agentAct(...steps: { ref: string; action: string; args?: unknown[] }[]) {
  const result = await agent.act({ steps })
  expect(result.ok, result.error?.message).toBe(true)
  return result
}

const sent = (fetch: ReturnType<typeof stubApi>, method: string) =>
  fetch.mock.calls
    .filter(([, init]) => init?.method === method)
    .map(([path, init]) => ({ path: String(path), body: init?.body && JSON.parse(String(init.body)) }))

const laundry = choreFixture({
  id: 7,
  name: 'Laundry',
  kind: 'Recurring',
  interval: { count: 1, unit: 'Week' },
  dueDate: '2027-03-09',
  urgency: 1.25,
  items: [{ id: 1, name: 'Sheets', description: 'Hot wash', ticked: false }],
})

test('an agent can log in', async () => {
  const fetch = stubApi({ '/api/session': 401, '/api/login': 204, '/api/chores': [] })
  renderAt('/login')
  await screen.findByLabelText('Household password')

  expectNamed('Household password', 'Log in')
  await agentAct({ ref: 'Household password', action: 'setValue', args: ['secret'] }, { ref: 'Log in', action: 'press' })

  await expect.poll(() => sent(fetch, 'POST')).toEqual([{ path: '/api/login', body: { password: 'secret' } }])
})

test('an agent can mark a Chore done, open it and tick its Items, and log out', async () => {
  const later = choreFixture({ id: 8, name: 'Clean the gutters', kind: 'Recurring', dueDate: '2027-04-01', urgency: 0 })
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': [laundry, later],
    '/api/chores/7/complete': { completionId: 31, chore: laundry },
    '/api/chores/7/items/1': laundry,
    '/api/logout': 204,
  })
  renderAt('/')
  await screen.findByText('Laundry')

  expectNamed('Mark Laundry done', 'Open Laundry Tue 9 Mar', 'Later (1)', 'Log out')
  await agentAct({ ref: 'Mark Laundry done', action: 'press' })
  await expect.poll(() => sent(fetch, 'POST').map((request) => request.path)).toEqual(['/api/chores/7/complete'])

  await agentAct(
    { ref: 'Open Laundry', action: 'setOpen', args: [true] },
    { ref: 'Sheets', action: 'setChecked', args: [true] },
    { ref: 'Description of Sheets', action: 'setOpen', args: [true] },
    { ref: 'Later', action: 'setOpen', args: [true] },
  )
  await expect.poll(() => sent(fetch, 'PUT')).toEqual([{ path: '/api/chores/7/items/1', body: { ticked: true } }])
  expect(screen.getByText('Hot wash')).toBeVisible()
  expect(screen.getByText('Clean the gutters')).toBeVisible()

  await agentAct({ ref: refOf('Log out'), action: 'press' })
  await expect.poll(() => sent(fetch, 'POST').map((request) => request.path)).toContain('/api/logout')
})

test('an agent can sort All Chores and open one', async () => {
  stubApi({ '/api/session': 204, '/api/chores': [laundry] })
  const router = renderAt('/chores')
  await screen.findByRole('table', { name: 'All Chores' })

  expectNamed('Active', 'Archived', 'Urgency', 'Laundry')
  // Each header's row is the other headers, so a name alone finds them all
  await agentAct({ ref: refOf('Urgency'), action: 'press' }, { ref: refOf('Laundry'), action: 'press' })

  // Narrower than lg, so the Chore opens on its own screen
  await expect.poll(() => router.state.location.pathname).toBe('/chores/7')
})

test('an agent can rename an Area, and deletes one only once the person agrees', async () => {
  const kitchen = { id: 3, name: 'Kitchen', parentId: null, choreCount: 2 }
  const fetch = stubApi({
    '/api/session': 204,
    '/api/areas': [kitchen],
    '/api/areas/3': () => Response.json({ ...kitchen, name: 'Scullery' }),
    '/api/chores': [],
  })
  renderAt('/areas')
  await screen.findByRole('table', { name: 'Areas' })

  expectNamed('New Area', 'Kitchen')
  await agentAct({ ref: 'Kitchen', action: 'press' })
  await screen.findByRole('dialog', { name: 'Edit Area' })
  expectNamed('Name', 'Inside', 'Save', 'Delete')
  await agentAct({ ref: 'Name', action: 'setValue', args: ['Scullery'] }, { ref: 'Save', action: 'press' })
  await expect.poll(() => sent(fetch, 'PUT')).toEqual([{ path: '/api/areas/3', body: { name: 'Scullery', parentId: null } }])

  await agentAct({ ref: 'Kitchen', action: 'press' })
  await agentAct({ ref: refOf('Delete'), action: 'press' })
  const confirm = { steps: [{ ref: 'Delete Kitchen?', action: 'press', args: ['Delete for good'] }] }
  const refused = await agent.act(confirm)
  expect(refused.ok).toBe(false)
  expect(sent(fetch, 'DELETE')).toEqual([])

  approve = true
  if (!screen.queryByRole('alertdialog')) await agentAct({ ref: refOf('Delete'), action: 'press' })
  expect((await agent.act(confirm)).ok).toBe(true)
  await expect.poll(() => sent(fetch, 'DELETE')).toEqual([{ path: '/api/areas/3', body: undefined }])
})

test('an agent can edit a Chore', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': [laundry],
    '/api/chores/7': (init) => Response.json(init?.method === 'PUT' ? { ...laundry, name: 'Washing' } : laundry),
    '/api/areas': [],
  })
  renderAt('/chores/7')
  await screen.findByRole('textbox', { name: 'Name' })

  expectNamed('Back', 'Name', 'Description', 'Item 1', 'Add item', 'Every', 'Unit', 'Preferred day', 'Seasonal', 'Area', 'Importance', 'Effort in minutes', 'Save', 'Delete')
  await agentAct(
    { ref: 'Name', action: 'setValue', args: ['Washing'] },
    { ref: 'Every', action: 'setValue', args: [2] },
    { ref: 'Unit', action: 'setValue', args: ['months'] },
    { ref: 'Importance', action: 'setValue', args: ['High'] },
    { ref: 'Save', action: 'press' },
  )

  await expect.poll(() => sent(fetch, 'PUT')[0]).toMatchObject({
    path: '/api/chores/7',
    body: { name: 'Washing', interval: { count: 2, unit: 'Month' }, importance: 'High' },
  })
})

test('an agent can add a Chore', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': [],
    '/api/chores/recurring': new Response('{}', { status: 201 }),
    '/api/areas': [],
  })
  renderAt('/')
  await screen.findByText(/Nothing to do yet/)

  await agentAct({ ref: refOf('New Chore'), action: 'press' })
  await screen.findByRole('dialog', { name: 'New Chore' })
  expectNamed('Kind', 'Name', 'Every', 'Unit', 'Save', 'Save and add another')
  await agentAct({ ref: 'Name', action: 'setValue', args: ['Water the plants'] }, { ref: refOf('Save'), action: 'press' })

  await expect.poll(() => sent(fetch, 'POST')[0]).toMatchObject({ path: '/api/chores/recurring', body: { name: 'Water the plants' } })
})

test('an agent can postpone a Chore', async () => {
  const fetch = stubApi({ '/api/session': 204, '/api/chores': [laundry], '/api/chores/7/postpone': laundry })
  renderAt('/')
  await screen.findByText('Laundry')

  await agentAct({ ref: 'Open Laundry', action: 'setOpen', args: [true] })
  // A row's actions each have the others as row text, so a name alone finds them all
  await agentAct({ ref: refOf('Postpone…'), action: 'press' })
  await screen.findByRole('dialog', { name: 'Postpone Laundry' })
  expectNamed('Postpone to', 'Save')
  await agentAct({ ref: 'Postpone to', action: 'setValue', args: ['2030-01-15'] }, { ref: 'Save', action: 'press' })

  await expect.poll(() => sent(fetch, 'POST')).toEqual([
    { path: '/api/chores/7/postpone', body: { dueDate: '2027-03-09', to: '2030-01-15' } },
  ])
})

test('an agent can restore an Archived Chore', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': [],
    '/api/chores?archived=true': [{ ...laundry, archivedOn: '2026-11-01' }],
    '/api/chores/7/restore': laundry,
  })
  renderAt('/chores?archived=true')
  await screen.findByRole('table', { name: 'Archived Chores' })

  expectNamed('Active', 'Archived', 'Restore')
  await agentAct({ ref: refOf('Restore'), action: 'press' })

  await expect.poll(() => sent(fetch, 'POST').map((request) => request.path)).toEqual(['/api/chores/7/restore'])
})

test('an agent deletes a Chore that was never done only once the person agrees', async () => {
  const fresh = { ...laundry, completionCount: 0 }
  const fetch = stubApi({
    '/api/session': 204,
    '/api/chores': [fresh],
    '/api/chores/7': (init) => (init?.method === 'DELETE' ? Response.json({ archived: false }) : Response.json(fresh)),
    '/api/areas': [],
  })
  renderAt('/chores/7')
  await screen.findByRole('textbox', { name: 'Name' })

  await agentAct({ ref: refOf('Delete'), action: 'press' })
  const confirm = { steps: [{ ref: 'Delete Laundry?', action: 'press', args: ['Delete for good'] }] }
  expect((await agent.act(confirm)).ok).toBe(false)
  expect(sent(fetch, 'DELETE')).toEqual([])

  approve = true
  if (!screen.queryByRole('alertdialog')) await agentAct({ ref: refOf('Delete'), action: 'press' })
  expect((await agent.act(confirm)).ok).toBe(true)
  await expect.poll(() => sent(fetch, 'DELETE').map((request) => request.path)).toEqual(['/api/chores/7'])
})

import { QueryClient } from '@tanstack/react-query'
import { createMemoryHistory } from '@tanstack/react-router'
import { render, screen, within } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, test } from 'vitest'
import App from '../App.tsx'
import type { Area } from '../areas.ts'
import { createAppRouter } from '../router.tsx'
import { stubApi } from '../test-api.ts'

const downstairs: Area = { id: 2, name: 'Downstairs', parentId: null, choreCount: 0 }
const kitchen: Area = { id: 3, name: 'Kitchen', parentId: 2, choreCount: 2 }

function renderAreas() {
  const queryClient = new QueryClient()
  render(<App queryClient={queryClient} router={createAppRouter(queryClient, createMemoryHistory({ initialEntries: ['/areas'] }))} />)
}

async function pick(field: string, option: string) {
  await userEvent.click(screen.getByRole('combobox', { name: field }))
  await userEvent.click(await screen.findByRole('option', { name: option }))
}

function sent(fetch: ReturnType<typeof stubApi>, method: string) {
  return fetch.mock.calls.filter(([, init]) => init?.method === method).map(([path, init]) => ({
    path: String(path),
    body: init?.body ? JSON.parse(String(init.body)) : undefined,
  }))
}

test('each Area shows the Area it sits inside', async () => {
  stubApi({ '/api/session': 204, '/api/areas': [downstairs, kitchen] })
  renderAreas()

  const rows = await screen.findAllByRole('listitem')
  expect(rows.map((row) => row.textContent)).toEqual([expect.stringContaining('Downstairs'), expect.stringContaining('Kitchen')])
  expect(within(rows[1]!).getByText('in Downstairs')).toBeVisible()
  expect(within(rows[0]!).queryByText(/^in /)).not.toBeInTheDocument()
})

test('a new Area is made inside the picked parent', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/areas': (init) => (init?.method === 'POST' ? Response.json(kitchen, { status: 201 }) : Response.json([downstairs])),
  })
  renderAreas()

  await userEvent.click(await screen.findByRole('button', { name: 'New Area' }))
  await userEvent.type(screen.getByRole('textbox', { name: 'Name' }), 'Kitchen')
  await pick('Inside', 'Downstairs')
  await userEvent.click(screen.getByRole('button', { name: 'Save' }))

  await expect.poll(() => sent(fetch, 'POST')).toEqual([{ path: '/api/areas', body: { name: 'Kitchen', parentId: 2 } }])
  await expect.poll(() => screen.queryByRole('dialog')).not.toBeInTheDocument()
})

test('an Area is renamed and taken out of its parent', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/areas': [downstairs, kitchen],
    '/api/areas/3': () => Response.json({ ...kitchen, name: 'Big kitchen', parentId: null }),
  })
  renderAreas()

  await userEvent.click(await screen.findByRole('button', { name: /Kitchen/ }))
  const name = screen.getByRole('textbox', { name: 'Name' })
  expect(screen.getByRole('combobox', { name: 'Inside' })).toHaveTextContent('Downstairs')
  await userEvent.clear(name)
  await userEvent.type(name, 'Big kitchen')
  await pick('Inside', 'Nothing, at the top')
  await userEvent.click(screen.getByRole('button', { name: 'Save' }))

  await expect.poll(() => sent(fetch, 'PUT')).toEqual([{ path: '/api/areas/3', body: { name: 'Big kitchen', parentId: null } }])
})

test('an Area the API rejects shows why', async () => {
  stubApi({
    '/api/session': 204,
    '/api/areas': [downstairs, kitchen],
    '/api/areas/2': Response.json(
      { status: 400, errors: { parentId: ['Downstairs cannot sit inside itself or an Area inside it.'] } },
      { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
    ),
  })
  renderAreas()

  await userEvent.click(await screen.findByRole('button', { name: 'Downstairs' }))
  await pick('Inside', 'Kitchen')
  await userEvent.click(screen.getByRole('button', { name: 'Save' }))

  expect(await screen.findByText('Downstairs cannot sit inside itself or an Area inside it.')).toBeVisible()
})

test('deleting an Area first says how many Chores use it', async () => {
  const fetch = stubApi({
    '/api/session': 204,
    '/api/areas': [downstairs, kitchen],
    '/api/areas/3': 204,
    '/api/chores': [],
  })
  renderAreas()

  await userEvent.click(await screen.findByRole('button', { name: /Kitchen/ }))
  await userEvent.click(screen.getByRole('button', { name: 'Delete' }))

  const confirm = await screen.findByRole('alertdialog')
  expect(confirm).toHaveTextContent('2 Chores use Kitchen')
  expect(sent(fetch, 'DELETE')).toEqual([])
  await userEvent.click(within(confirm).getByRole('button', { name: 'Delete' }))

  await expect.poll(() => sent(fetch, 'DELETE')).toEqual([{ path: '/api/areas/3', body: undefined }])
})

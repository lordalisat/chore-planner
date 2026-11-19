import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, test, vi } from 'vitest'
import type { Chore } from '../chores.ts'
import { stubApi } from '../test-api.ts'
import { Toasts } from '../ui/Toasts.tsx'
import { DeleteChore } from './DeleteChore.tsx'

const laundry: Chore = {
  id: 7,
  kind: 'Recurring',
  name: 'Laundry',
  importance: 'Normal',
  effort: 30,
  area: null,
  interval: { count: 1, unit: 'Week' },
  preferredDay: 'Tuesday',
  season: null,
  dueDate: '2027-03-16',
  urgency: 0,
  overdue: false,
  dormant: false,
  finished: false,
  canSkip: true,
  lastDone: '2027-03-09',
  completionCount: 3,
  archivedOn: null,
}
const fresh: Chore = { ...laundry, id: 8, name: 'Clean the oven', lastDone: null, completionCount: 0 }

function renderDelete(chore: Chore) {
  const onDeleted = vi.fn()
  render(
    <QueryClientProvider client={new QueryClient()}>
      <DeleteChore chore={chore} onDeleted={onDeleted} />
      <Toasts />
    </QueryClientProvider>,
  )
  return onDeleted
}

const requests = (fetch: ReturnType<typeof stubApi>) =>
  fetch.mock.calls.map(([path, init]) => `${init?.method ?? 'GET'} ${String(path)}`)

test('deleting a Chore that was done before archives it at once and offers Undo', async () => {
  const fetch = stubApi({
    '/api/chores/7': { archived: true },
    '/api/chores/7/restore': laundry,
    '/api/chores': [],
  })
  const onDeleted = renderDelete(laundry)

  await userEvent.click(screen.getByRole('button', { name: 'Delete' }))

  await expect.poll(() => onDeleted.mock.calls.length).toBe(1)
  expect(screen.queryByRole('alertdialog')).not.toBeInTheDocument()
  expect(requests(fetch)).toContain('DELETE /api/chores/7')
  expect(await screen.findByText('Archived Laundry')).toBeInTheDocument()
  await userEvent.click(await screen.findByRole('button', { name: 'Undo' }))
  await expect.poll(() => requests(fetch)).toContain('POST /api/chores/7/restore')
})

test('deleting a Chore never done asks first, since it is gone for good', async () => {
  const fetch = stubApi({ '/api/chores/8': { archived: false }, '/api/chores': [] })
  const onDeleted = renderDelete(fresh)

  await userEvent.click(screen.getByRole('button', { name: 'Delete' }))
  const question = await screen.findByRole('alertdialog')
  expect(question).toHaveTextContent('Delete Clean the oven?')
  expect(requests(fetch)).not.toContain('DELETE /api/chores/8')

  await userEvent.click(screen.getByRole('button', { name: 'Delete for good' }))

  await expect.poll(() => onDeleted.mock.calls.length).toBe(1)
  expect(requests(fetch)).toContain('DELETE /api/chores/8')
  expect(await screen.findByText('Deleted Clean the oven')).toBeInTheDocument()
  expect(screen.queryByRole('button', { name: 'Undo' })).not.toBeInTheDocument()
})

test('cancelling the question keeps the Chore', async () => {
  const fetch = stubApi({ '/api/chores/8': { archived: false } })
  const onDeleted = renderDelete(fresh)

  await userEvent.click(screen.getByRole('button', { name: 'Delete' }))
  await userEvent.click(await screen.findByRole('button', { name: 'Cancel' }))

  await expect.poll(() => screen.queryByRole('alertdialog')).not.toBeInTheDocument()
  expect(fetch).not.toHaveBeenCalled()
  expect(onDeleted).not.toHaveBeenCalled()
})

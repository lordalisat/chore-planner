import { QueryClient } from '@tanstack/react-query'
import { createMemoryHistory } from '@tanstack/react-router'
import { render, screen, within } from '@testing-library/react'
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
    lastDone: null,
    completionCount: 0,
    archivedOn: null,
    ...details,
  }
}

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

  const queryClient = new QueryClient()
  render(<App queryClient={queryClient} router={createAppRouter(queryClient, createMemoryHistory())} />)

  const [main, anytime] = await screen.findAllByRole('list')
  expect(within(main!).getAllByRole('listitem').map((row) => row.querySelector('span')?.textContent)).toEqual([
    'Take out the bins',
    'Book the plumber',
    'Laundry',
  ])
  expect(screen.getByRole('heading', { name: 'Anytime' })).toBeVisible()
  expect(within(anytime!).getAllByRole('listitem').map((row) => row.querySelector('span')?.textContent)).toEqual(['Fix leaking tap'])
  expect(screen.queryByText('Hang the mirror')).not.toBeInTheDocument()
})

import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen, within } from '@testing-library/react'
import { Suspense } from 'react'
import { expect, test } from 'vitest'
import type { Chore } from '../chores.ts'
import { stubApi } from '../test-api.ts'
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
    completionCount: 0,
    archivedOn: null,
    ...details,
  }
}

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

  render(
    <QueryClientProvider client={new QueryClient()}>
      <Suspense>
        <Overview />
      </Suspense>
    </QueryClientProvider>,
  )

  const [main, anytime] = await screen.findAllByRole('list')
  expect(within(main!).getAllByRole('listitem').map((row) => row.firstChild?.textContent)).toEqual([
    'Take out the bins',
    'Book the plumber',
    'Laundry',
  ])
  expect(screen.getByRole('heading', { name: 'Anytime' })).toBeVisible()
  expect(within(anytime!).getAllByRole('listitem').map((row) => row.firstChild?.textContent)).toEqual(['Fix leaking tap'])
  expect(screen.queryByText('Hang the mirror')).not.toBeInTheDocument()
})

import { QueryClient, QueryClientProvider } from '@tanstack/react-query'
import { render, screen } from '@testing-library/react'
import userEvent from '@testing-library/user-event'
import { expect, test } from 'vitest'
import { stubApi } from '../test-api.ts'
import { ChoreForm } from './ChoreForm.tsx'

function renderForm() {
  render(
    <QueryClientProvider client={new QueryClient()}>
      <ChoreForm onDone={() => {}} />
    </QueryClientProvider>,
  )
}

async function pick(field: string, option: string) {
  await userEvent.click(screen.getByRole('combobox', { name: field }))
  await userEvent.click(await screen.findByRole('option', { name: option }))
}

async function preferredDayOptions() {
  await userEvent.click(screen.getByRole('combobox', { name: 'Preferred day' }))
  const options = (await screen.findAllByRole('option')).map((option) => option.textContent)
  await userEvent.keyboard('{Escape}')
  return options
}

test('Save and add another keeps the Interval, Preferred Day and Season and starts on a new Name', async () => {
  const fetch = stubApi({ '/api/chores/recurring': new Response('{}', { status: 201 }), '/api/chores': [] })
  renderForm()

  await userEvent.type(screen.getByRole('textbox', { name: 'Name' }), 'Hoover upstairs')
  await userEvent.clear(screen.getByRole('textbox', { name: 'Every' }))
  await userEvent.type(screen.getByRole('textbox', { name: 'Every' }), '2')
  await pick('Preferred day', 'Tuesday')
  await userEvent.click(screen.getByRole('switch', { name: 'Seasonal' }))
  await pick('Season from', 'April')
  await pick('Season to', 'October')
  await userEvent.type(screen.getByRole('textbox', { name: 'Effort in minutes' }), '20')
  await userEvent.click(screen.getByRole('button', { name: 'Save and add another' }))

  const name = screen.getByRole('textbox', { name: 'Name' })
  await expect.poll(() => name).toHaveValue('')
  expect(JSON.parse(String(fetch.mock.calls[0]?.[1]?.body))).toMatchObject({
    name: 'Hoover upstairs',
    interval: { count: 2, unit: 'Week' },
    preferredDay: 'Tuesday',
    season: { fromMonth: 4, toMonth: 10 },
    effort: 20,
  })
  expect(name).toHaveFocus()
  expect(screen.getByRole('textbox', { name: 'Every' })).toHaveValue('2')
  expect(screen.getByRole('combobox', { name: 'Unit' })).toHaveTextContent('weeks')
  expect(screen.getByRole('combobox', { name: 'Preferred day' })).toHaveTextContent('Tuesday')
  expect(screen.getByRole('combobox', { name: 'Season from' })).toHaveTextContent('April')
  expect(screen.getByRole('combobox', { name: 'Season to' })).toHaveTextContent('October')
  expect(screen.getByRole('textbox', { name: 'Effort in minutes' })).toHaveValue('')
})

test('switching Seasonal off makes a Chore all year again', async () => {
  const fetch = stubApi({ '/api/chores/recurring': new Response('{}', { status: 201 }), '/api/chores': [] })
  renderForm()
  await userEvent.click(screen.getByRole('switch', { name: 'Seasonal' }))
  await pick('Season from', 'April')
  await pick('Season to', 'October')

  await userEvent.click(screen.getByRole('switch', { name: 'Seasonal' }))
  await userEvent.type(screen.getByRole('textbox', { name: 'Name' }), 'Dust{Enter}')

  expect(screen.queryByRole('combobox', { name: 'Season from' })).not.toBeInTheDocument()
  await expect.poll(() => fetch.mock.calls.length).toBe(1)
  expect(JSON.parse(String(fetch.mock.calls[0]?.[1]?.body))).toMatchObject({ name: 'Dust', season: null })
})

test('a field the API rejects shows why', async () => {
  stubApi({
    '/api/chores/recurring': Response.json(
      { status: 400, errors: { preferredDay: ['The first or last of the month needs an Interval in months or years.'] } },
      { status: 400, headers: { 'Content-Type': 'application/problem+json' } },
    ),
  })
  renderForm()

  await userEvent.type(screen.getByRole('textbox', { name: 'Name' }), 'Pay the rent{Enter}')

  expect(await screen.findByText('The first or last of the month needs an Interval in months or years.')).toBeVisible()
  expect(screen.getByRole('combobox', { name: 'Preferred day' })).toHaveAttribute('aria-invalid', 'true')
})

test('there is no Preferred Day for an Interval in days', async () => {
  renderForm()

  await pick('Unit', 'days')
  await userEvent.clear(screen.getByRole('textbox', { name: 'Every' }))
  await userEvent.type(screen.getByRole('textbox', { name: 'Every' }), '7')

  expect(screen.queryByRole('combobox', { name: 'Preferred day' })).not.toBeInTheDocument()
})

test('a Preferred Day the new Interval does not allow is cleared', async () => {
  renderForm()
  await pick('Preferred day', 'Monday')

  await pick('Unit', 'days')
  await pick('Unit', 'weeks')

  expect(screen.getByRole('combobox', { name: 'Preferred day' })).toHaveTextContent('Any day')
})

test('Any day clears a picked Preferred Day', async () => {
  const fetch = stubApi({ '/api/chores/recurring': new Response('{}', { status: 201 }), '/api/chores': [] })
  renderForm()
  await pick('Preferred day', 'Tuesday')

  await pick('Preferred day', 'Any day')
  await userEvent.type(screen.getByRole('textbox', { name: 'Name' }), 'Dust{Enter}')

  expect(screen.getByRole('combobox', { name: 'Preferred day' })).toHaveTextContent('Any day')
  await expect.poll(() => fetch.mock.calls.length).toBe(1)
  expect(JSON.parse(String(fetch.mock.calls[0]?.[1]?.body))).toMatchObject({ name: 'Dust', preferredDay: null })
})

test('the first and last of the month are only offered for an Interval in months or years', async () => {
  renderForm()

  await pick('Unit', 'weeks')
  expect(await preferredDayOptions()).not.toContain('Last of the month')
  await pick('Unit', 'months')
  expect(await preferredDayOptions()).toEqual(expect.arrayContaining(['Tuesday', 'First of the month', 'Last of the month']))
})

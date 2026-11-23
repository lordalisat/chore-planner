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

// The Chores the form saved, leaving out what it read, such as the Areas
function sent(fetch: ReturnType<typeof stubApi>) {
  return fetch.mock.calls.filter(([, init]) => init?.method === 'POST' || init?.method === 'PUT')
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
  expect(JSON.parse(String(sent(fetch)[0]?.[1]?.body))).toMatchObject({
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

test('a Chore goes in the picked Area, which Save and add another keeps', async () => {
  const fetch = stubApi({
    '/api/chores/one-off': new Response('{}', { status: 201 }),
    '/api/chores': [],
    '/api/areas': [
      { id: 2, name: 'Downstairs', parentId: null, choreCount: 0 },
      { id: 3, name: 'Kitchen', parentId: 2, choreCount: 0 },
    ],
  })
  renderForm()

  await userEvent.click(screen.getByRole('radio', { name: 'One-off' }))
  await userEvent.type(screen.getByRole('textbox', { name: 'Name' }), 'Clean the fridge')
  await pick('Area', 'Kitchen')
  await userEvent.click(screen.getByRole('button', { name: 'Save and add another' }))

  await expect.poll(() => screen.getByRole('textbox', { name: 'Name' })).toHaveValue('')
  expect(JSON.parse(String(sent(fetch)[0]?.[1]?.body))).toMatchObject({ name: 'Clean the fridge', areaId: 3 })
  expect(screen.getByRole('combobox', { name: 'Area' })).toHaveTextContent('Kitchen')
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
  await expect.poll(() => sent(fetch).length).toBe(1)
  expect(JSON.parse(String(sent(fetch)[0]?.[1]?.body))).toMatchObject({ name: 'Dust', season: null })
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

test('a Description is sent with its line breaks, and Save and add another clears it', async () => {
  const fetch = stubApi({ '/api/chores/recurring': new Response('{}', { status: 201 }), '/api/chores': [] })
  renderForm()

  await userEvent.type(screen.getByRole('textbox', { name: 'Name' }), 'Clean bathroom sink')
  await userEvent.type(screen.getByRole('textbox', { name: 'Description' }), 'Descaler under the sink;{Enter}rinse the plug hole too')
  await userEvent.click(screen.getByRole('button', { name: 'Save and add another' }))

  await expect.poll(() => sent(fetch).length).toBe(1)
  expect(JSON.parse(String(sent(fetch)[0]?.[1]?.body))).toMatchObject({
    name: 'Clean bathroom sink',
    description: 'Descaler under the sink;\nrinse the plug hole too',
  })
  await expect.poll(() => screen.getByRole('textbox', { name: 'Description' })).toHaveValue('')
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
  await expect.poll(() => sent(fetch).length).toBe(1)
  expect(JSON.parse(String(sent(fetch)[0]?.[1]?.body))).toMatchObject({ name: 'Dust', preferredDay: null })
})

test('a One-off Chore asks only for a Name, Due Date, Importance and Effort', async () => {
  const fetch = stubApi({ '/api/chores/one-off': new Response('{}', { status: 201 }), '/api/chores': [] })
  renderForm()

  await userEvent.click(screen.getByRole('radio', { name: 'One-off' }))
  await userEvent.type(screen.getByRole('textbox', { name: 'Effort in minutes' }), '15')
  await userEvent.click(screen.getByRole('radio', { name: 'High' }))
  await userEvent.type(screen.getByRole('textbox', { name: 'Name' }), 'Fix leaking tap{Enter}')

  expect(screen.queryByRole('textbox', { name: 'Every' })).not.toBeInTheDocument()
  expect(screen.queryByRole('switch', { name: 'Seasonal' })).not.toBeInTheDocument()
  expect(screen.queryByText('Last done')).not.toBeInTheDocument()
  expect(screen.getByText('Due date')).toBeVisible()
  await expect.poll(() => sent(fetch).length).toBe(1)
  expect(sent(fetch)[0]?.[0]).toBe('/api/chores/one-off')
  expect(JSON.parse(String(sent(fetch)[0]?.[1]?.body))).toEqual({
    name: 'Fix leaking tap',
    description: '',
    dueDate: null,
    importance: 'High',
    effort: 15,
    areaId: null,
  })
})

test('the first and last of the month are only offered for an Interval in months or years', async () => {
  renderForm()

  await pick('Unit', 'weeks')
  expect(await preferredDayOptions()).not.toContain('Last of the month')
  await pick('Unit', 'months')
  expect(await preferredDayOptions()).toEqual(expect.arrayContaining(['Tuesday', 'First of the month', 'Last of the month']))
})

test('a weekday in the month is picked as its week and day, starting on the first Saturday, and saved as one Preferred Day', async () => {
  const fetch = stubApi({ '/api/chores/recurring': new Response('{}', { status: 201 }), '/api/chores': [] })
  renderForm()
  await pick('Unit', 'months')

  await pick('Preferred day', 'A weekday in the month…')
  expect(screen.getByRole('combobox', { name: 'Which Saturday' })).toHaveTextContent('First')
  expect(screen.getByRole('combobox', { name: 'Weekday' })).toHaveTextContent('Saturday')
  await pick('Which Saturday', 'Last')
  await pick('Weekday', 'Friday')
  expect(screen.getByRole('combobox', { name: 'Which Friday' })).toHaveTextContent('Last')
  await userEvent.type(screen.getByRole('textbox', { name: 'Name' }), 'Check the smoke alarms{Enter}')

  expect(screen.getByRole('combobox', { name: 'Preferred day' })).toHaveTextContent('A weekday in the month…')
  await expect.poll(() => sent(fetch).length).toBe(1)
  expect(JSON.parse(String(sent(fetch)[0]?.[1]?.body))).toMatchObject({
    interval: { count: 1, unit: 'Month' },
    preferredDay: 'LastFriday',
  })
})

test('a weekday in the month stays for an Interval in years and is cleared for one in weeks', async () => {
  renderForm()
  await pick('Unit', 'months')
  await pick('Preferred day', 'A weekday in the month…')
  await pick('Weekday', 'Sunday')

  await pick('Unit', 'years')
  expect(screen.getByRole('combobox', { name: 'Weekday' })).toHaveTextContent('Sunday')

  await pick('Unit', 'weeks')
  expect(screen.queryByRole('combobox', { name: 'Weekday' })).not.toBeInTheDocument()
  expect(screen.getByRole('combobox', { name: 'Preferred day' })).toHaveTextContent('Any day')
})

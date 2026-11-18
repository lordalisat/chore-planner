import { expect, test } from 'vitest'
import { relativeDate } from './dates.ts'

// A Wednesday
const today = '2026-11-18'

test.each([
  ['2026-11-18', 'today'],
  ['2026-11-19', 'tomorrow'],
  ['2026-11-21', 'in 3 days'],
  ['2026-11-24', 'in 6 days'],
  ['2026-11-25', 'Wed 25 Nov'],
  ['2027-01-04', 'Mon 4 Jan'],
  ['2026-11-17', '1 day overdue'],
  ['2026-11-16', '2 days overdue'],
  ['2026-10-18', '31 days overdue'],
])('%s is %s', (date, expected) => {
  expect(relativeDate(date, today)).toBe(expected)
})

test('days are counted by the calendar, across the change to summer time', () => {
  expect(relativeDate('2027-03-30', '2027-03-27')).toBe('in 3 days')
})

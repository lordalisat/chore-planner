import { expect, test } from 'vitest'
import { preferredDayName, weekdayInMonth } from './chores.ts'

test.each([
  ['Tuesday', 'Tuesday'],
  ['FirstOfMonth', 'the first'],
  ['LastOfMonth', 'the last'],
  ['FirstSaturday', 'the first Saturday'],
  ['LastFriday', 'the last Friday'],
] as const)('%s reads as "every month on %s"', (day, name) => {
  expect(preferredDayName(day)).toBe(name)
})

test('only a weekday in the month splits into its ordinal and day', () => {
  expect(weekdayInMonth('SecondTuesday')).toEqual({ ordinal: 'Second', day: 'Tuesday' })
  expect(weekdayInMonth('Tuesday')).toBeNull()
  expect(weekdayInMonth('FirstOfMonth')).toBeNull()
  expect(weekdayInMonth(null)).toBeNull()
})

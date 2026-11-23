import { queryOptions } from '@tanstack/react-query'
import { api } from './api/client.ts'
import type { components } from './api/schema'

export type Chore = components['schemas']['ChoreDto']
export type Unit = NonNullable<Chore['interval']>['unit']

/** How each kind of Chore is named on screen. */
export const kindLabels: Record<Chore['kind'], string> = { Recurring: 'Recurring', OneOff: 'One-off' }

/** Interval units as counted, "2 weeks"; one of them drops the s. */
export const unitLabels: Record<Unit, string> = { Day: 'days', Week: 'weeks', Month: 'months', Year: 'years' }

export type PreferredDay = NonNullable<Chore['preferredDay']>
// Taken from the API's Preferred Days, so a change there fails the type check here
export type Weekday = Extract<PreferredDay, 'Monday' | 'Tuesday' | 'Wednesday' | 'Thursday' | 'Friday' | 'Saturday' | 'Sunday'>
/** Which of a weekday's days in a month. */
export type Ordinal = 'First' | 'Second' | 'Third' | 'Fourth' | 'Last'
export const weekdayNames: Weekday[] = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday']
export const ordinalNames: Ordinal[] = ['First', 'Second', 'Third', 'Fourth', 'Last']

/** The Preferred Day for a weekday in the month, such as "FirstSaturday"; the type check holds it to the API's list. */
export const toWeekdayInMonth = (ordinal: Ordinal, day: Weekday): PreferredDay => `${ordinal}${day}`

/** A weekday in the month split into its ordinal and day; null for any other Preferred Day. */
export function weekdayInMonth(preferred: PreferredDay | null): { ordinal: Ordinal; day: Weekday } | null {
  for (const ordinal of ordinalNames) {
    for (const day of weekdayNames) {
      if (preferred === toWeekdayInMonth(ordinal, day)) return { ordinal, day }
    }
  }
  return null
}

/** How a Preferred Day reads after "every month on": "Tuesday", "the first", "the first Saturday". */
export function preferredDayName(day: PreferredDay): string {
  if (day === 'FirstOfMonth') return 'the first'
  if (day === 'LastOfMonth') return 'the last'
  const inMonth = weekdayInMonth(day)
  return inMonth ? `the ${inMonth.ordinal.toLowerCase()} ${inMonth.day}` : day
}

/** Every Chore, with its scheduling as the server computed it. */
export const choresQuery = queryOptions({
  queryKey: ['chores'],
  queryFn: () => api('/api/chores', 'get'),
})

/** One Chore, under the key of every Chore so refetching those refetches it too. */
export const choreQuery = (id: number) =>
  queryOptions({
    queryKey: [...choresQuery.queryKey, id],
    queryFn: () => api('/api/chores/{id}', 'get', { id }),
  })

/** The Archived Chores, under the key of every Chore so refetching those refetches these too. */
export const archivedChoresQuery = queryOptions({
  queryKey: [...choresQuery.queryKey, 'archived'],
  queryFn: () => api('/api/chores', 'get', { archived: true }),
})

/** How a Chore's Urgency reads at a glance: nothing yet, coming up, about due, or pressing. */
export type UrgencyBand = 'none' | 'soon' | 'due' | 'pressing'

export function urgencyBand(urgency: number | null): UrgencyBand {
  if (!urgency) return 'none'
  return urgency < 1 ? 'soon' : urgency < 2 ? 'due' : 'pressing'
}

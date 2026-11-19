import { queryOptions } from '@tanstack/react-query'
import { api } from './api/client.ts'
import type { components } from './api/schema'

export type Chore = components['schemas']['ChoreDto']
export type Unit = NonNullable<Chore['interval']>['unit']

/** How each kind of Chore is named on screen. */
export const kindLabels: Record<Chore['kind'], string> = { Recurring: 'Recurring', OneOff: 'One-off' }

/** Interval units as counted, "2 weeks"; one of them drops the s. */
export const unitLabels: Record<Unit, string> = { Day: 'days', Week: 'weeks', Month: 'months', Year: 'years' }

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

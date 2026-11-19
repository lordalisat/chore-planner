import { queryOptions } from '@tanstack/react-query'
import { api } from './api/client.ts'
import type { components } from './api/schema'

export type Chore = components['schemas']['ChoreDto']

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

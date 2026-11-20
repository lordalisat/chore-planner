import { queryOptions } from '@tanstack/react-query'
import { api } from './api/client.ts'
import type { components } from './api/schema'

export type Area = components['schemas']['AreaDto']

/** Every Area, by Name, with how many Chores use each. */
export const areasQuery = queryOptions({
  queryKey: ['areas'],
  queryFn: () => api('/api/areas', 'get'),
})

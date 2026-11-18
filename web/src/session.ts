import { queryOptions } from '@tanstack/react-query'
import { api, isUnauthorized } from './api/client.ts'

export type Session = 'authenticated' | 'anonymous'

/** Whether this device is logged in; rejects when the server can't be reached. */
export const sessionQuery = queryOptions({
  queryKey: ['session'],
  queryFn: async (): Promise<Session> => {
    try {
      await api('/api/session', 'get')
      return 'authenticated'
    } catch (error) {
      if (isUnauthorized(error)) return 'anonymous'
      throw error
    }
  },
  // An unreachable server shows at once rather than after retries
  retry: false,
})

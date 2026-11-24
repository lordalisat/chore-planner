import { vi } from 'vitest'
import type { Chore } from './chores.ts'

/** How the stubbed API answers a path: a status, a JSON body with 200, a full Response, or no server at all. */
export type Answer = number | 'unreachable' | Response | ((init: RequestInit | undefined) => Response) | object

/** The API as the browser sees it; returns the fetch mock, so tests can see what was sent. */
export function stubApi(answers: Record<string, Answer>) {
  const fetch = vi.fn(async (input: RequestInfo | URL, init?: RequestInit) => {
    const answer = answers[String(input)]
    if (answer === undefined || answer === 'unreachable') throw new TypeError('Failed to fetch')
    if (typeof answer === 'number') return new Response(null, { status: answer })
    if (answer instanceof Response) return answer
    if (typeof answer === 'function') return answer(init)
    return Response.json(answer)
  })
  vi.stubGlobal('fetch', fetch)
  return fetch
}

/**
 * A Chore as the API sends it, with every field at its plain default: a One-off with no Due Date, nothing ticked, not
 * Archived. Tests give only what they are about; a new field on the API's Chore goes here, once.
 */
export function choreFixture(overrides: Partial<Chore> & Pick<Chore, 'id' | 'name'>): Chore {
  return {
    kind: 'OneOff',
    importance: 'Normal',
    effort: null,
    area: null,
    interval: null,
    preferredDay: null,
    season: null,
    dueDate: null,
    urgency: null,
    overdue: false,
    description: null,
    items: [],
    dormant: false,
    finished: false,
    canSkip: overrides.kind === 'Recurring',
    lastDone: null,
    completionCount: 0,
    archivedOn: null,
    ...overrides,
  }
}

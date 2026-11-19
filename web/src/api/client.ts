import type { paths } from './schema'

type Method = 'get' | 'post' | 'put' | 'delete'
// The methods the API has on a path
type MethodOf<P extends keyof paths> = { [M in Method]: paths[P][M] extends undefined ? never : M }[Method]
type Operation<P extends keyof paths, M extends MethodOf<P>> = NonNullable<paths[P][M]>
type RequestBody<O> = O extends { requestBody: { content: { 'application/json': infer B } } } ? B : undefined
type ResponseBody<O> = O extends { responses: { 200: { content: { 'application/json': infer R } } } } ? R : undefined
// The values for a path's `{name}` parameters
type PathParams<P> = P extends `${string}{${infer Name}}${infer Rest}` ? Record<Name, string | number> & PathParams<Rest> : unknown
// The body when the operation takes one, then the path's parameters when it has any
type Args<P, O> = [
  ...(RequestBody<O> extends undefined ? [] : [RequestBody<O>]),
  ...(P extends `${string}{${string}` ? [PathParams<P>] : []),
]

/** A validation problem's messages by field, such as `{ preferredDay: ['…'] }`. */
export type FieldErrors = Partial<Record<string, string[]>>

/** The API answered, but not with success; `status` says how, and `errors` what was wrong with which field. */
export class ApiError extends Error {
  readonly status: number
  readonly errors: FieldErrors

  constructor(status: number, errors: FieldErrors = {}) {
    super(`The API answered ${status}`)
    this.status = status
    this.errors = errors
  }
}

/** The API answered 401: the household password is needed. */
export const isUnauthorized = (error: unknown) => error instanceof ApiError && error.status === 401

/**
 * Calls the API with the types from `schema.d.ts` (`npm run api`). Rejects with an ApiError when the API answers
 * other than 2xx, and with a TypeError when the server can't be reached. A path such as `/api/chores/{id}` takes its
 * parameters last: `api('/api/chores/{id}', 'get', { id: 7 })`.
 */
export async function api<P extends keyof paths, M extends MethodOf<P>>(
  path: P,
  method: M,
  ...args: Args<P, Operation<P, M>>
): Promise<ResponseBody<Operation<P, M>>> {
  const params = path.includes('{') ? (args.at(-1) as Record<string, string | number>) : {}
  const body = path.includes('{') ? args.slice(0, -1)[0] : args[0]
  const url = path.replace(/\{(\w+)\}/g, (_, name: string) => encodeURIComponent(String(params[name])))
  const response = await fetch(url, {
    method: method.toUpperCase(),
    ...(body !== undefined && { headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }),
  })
  if (!response.ok) {
    const problem = response.headers.get('Content-Type')?.startsWith('application/problem+json')
      ? ((await response.json()) as { errors?: FieldErrors })
      : {}
    throw new ApiError(response.status, problem.errors)
  }
  return (response.status === 204 ? undefined : await response.json()) as ResponseBody<Operation<P, M>>
}

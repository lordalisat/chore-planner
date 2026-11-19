import type { paths } from './schema'

type Method = 'get' | 'post' | 'put' | 'delete'
// The methods the API has on a path
type MethodOf<P extends keyof paths> = { [M in Method]: paths[P][M] extends undefined ? never : M }[Method]
type Operation<P extends keyof paths, M extends MethodOf<P>> = NonNullable<paths[P][M]>
type RequestBody<O> = O extends { requestBody: { content: { 'application/json': infer B } } } ? B : undefined
// The values for a path's `{placeholders}`, such as `{ id: 7 }` for `/api/chores/{id}`
type PathParams<O> = O extends { parameters: { path: infer X } } ? X : undefined
type ResponseBody<O> = O extends { responses: { 200: { content: { 'application/json': infer R } } } } ? R : undefined

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
 * Calls the API with the types from `schema.d.ts` (`npm run api`): `api(path, method, body?, pathParams?)`, with
 * the body when the operation takes one and the path's values when it has `{placeholders}`. Rejects with an ApiError
 * when the API answers other than 2xx, and with a TypeError when the server can't be reached.
 */
export async function api<P extends keyof paths, M extends MethodOf<P>>(
  path: P,
  method: M,
  ...args: [
    ...(RequestBody<Operation<P, M>> extends undefined ? [] : [RequestBody<Operation<P, M>>]),
    ...(PathParams<Operation<P, M>> extends undefined ? [] : [PathParams<Operation<P, M>>]),
  ]
): Promise<ResponseBody<Operation<P, M>>> {
  const params = (path.includes('{') ? args.pop() : undefined) as Record<string, string | number> | undefined
  const [body] = args
  const url = path.replace(/\{(\w+)\}/g, (_, name: string) => encodeURIComponent(String(params?.[name])))
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

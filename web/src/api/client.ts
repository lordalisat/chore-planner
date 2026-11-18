import type { paths } from './schema'

type Method = 'get' | 'post' | 'put' | 'delete'
// The methods the API has on a path
type MethodOf<P extends keyof paths> = { [M in Method]: paths[P][M] extends undefined ? never : M }[Method]
type Operation<P extends keyof paths, M extends MethodOf<P>> = NonNullable<paths[P][M]>
type RequestBody<O> = O extends { requestBody: { content: { 'application/json': infer B } } } ? B : undefined
type ResponseBody<O> = O extends { responses: { 200: { content: { 'application/json': infer R } } } } ? R : undefined

/** The API answered, but not with success; `status` says how. */
export class ApiError extends Error {
  readonly status: number

  constructor(status: number) {
    super(`The API answered ${status}`)
    this.status = status
  }
}

/**
 * Calls the API with the types from `schema.d.ts` (`npm run api`). Rejects with an ApiError when the API answers
 * other than 2xx, and with a TypeError when the server can't be reached.
 */
export async function api<P extends keyof paths, M extends MethodOf<P>>(
  path: P,
  method: M,
  ...[body]: RequestBody<Operation<P, M>> extends undefined ? [] : [RequestBody<Operation<P, M>>]
): Promise<ResponseBody<Operation<P, M>>> {
  const response = await fetch(path, {
    method: method.toUpperCase(),
    ...(body !== undefined && { headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }),
  })
  if (!response.ok) throw new ApiError(response.status)
  return (response.status === 204 ? undefined : await response.json()) as ResponseBody<Operation<P, M>>
}

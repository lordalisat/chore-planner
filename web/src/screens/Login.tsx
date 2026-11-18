import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useRouter } from '@tanstack/react-router'
import { useState, type FormEvent } from 'react'
import { api, ApiError } from '../api/client.ts'
import { loginRoute } from '../router.tsx'
import { sessionQuery, unreachableMessage } from '../session.ts'
import { TextField } from '../ui/TextField.tsx'

export function Login() {
  const [password, setPassword] = useState('')
  const queryClient = useQueryClient()
  const router = useRouter()
  const { redirect } = loginRoute.useSearch()
  const login = useMutation({
    mutationFn: () => api('/api/login', 'post', { password }),
    onSuccess: () => {
      queryClient.setQueryData(sessionQuery.queryKey, 'authenticated')
      // Back to where the login interrupted
      router.history.push(redirect ?? '/')
    },
  })

  function submit(event: FormEvent) {
    event.preventDefault()
    login.mutate()
  }

  return (
    <main className="mx-auto max-w-sm px-4 py-12">
      <h1 className="text-2xl font-semibold text-brand">Chores</h1>
      <form onSubmit={submit} className="mt-6 flex flex-col gap-4">
        <TextField
          label="Household password"
          type="password"
          autoComplete="current-password"
          autoFocus
          value={password}
          onValueChange={setPassword}
        />
        <button
          type="submit"
          disabled={login.isPending}
          className="rounded-lg bg-brand px-4 py-2 font-semibold text-white hover:bg-brand/90 disabled:opacity-60"
        >
          Log in
        </button>
        {login.error && (
          <p role="alert" className="text-red-700">
            {login.error instanceof ApiError && login.error.status === 401 ? 'That password is not right.' : unreachableMessage}
          </p>
        )}
      </form>
    </main>
  )
}

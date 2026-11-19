import { useSuspenseQuery } from '@tanstack/react-query'
import { getRouteApi, Link, useNavigate } from '@tanstack/react-router'
import { choreQuery } from '../chores.ts'
import { ChoreForm } from './ChoreForm.tsx'

const route = getRouteApi('/authenticated/chores/$id')

/** The Chore form, filled in with a Chore; saving returns to the Overview. */
export function EditChore() {
  const { id } = route.useParams()
  const { data: chore } = useSuspenseQuery(choreQuery(id))
  const navigate = useNavigate()

  return (
    <section className="mx-auto max-w-lg">
      <h1 className="mb-4 text-xl font-semibold text-brand">Edit Chore</h1>
      {/* Keyed, so moving to another Chore starts from that one's values */}
      <ChoreForm key={chore.id} chore={chore} onDone={() => void navigate({ to: '/' })} />
    </section>
  )
}

export function ChoreNotFound() {
  return (
    <p className="py-12 text-center text-ink/70">
      That Chore is gone.{' '}
      <Link to="/" className="font-semibold text-brand underline">
        Back to the Overview
      </Link>
    </p>
  )
}

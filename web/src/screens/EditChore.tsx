import { useSuspenseQuery } from '@tanstack/react-query'
import { getRouteApi, Link, useNavigate } from '@tanstack/react-router'
import { choreQuery, type Chore } from '../chores.ts'
import { ChoreForm } from './ChoreForm.tsx'
import { DeleteChore } from './DeleteChore.tsx'

const route = getRouteApi('/authenticated/chores/$id')

/** The Chore form, filled in with a Chore; saving or deleting it returns to the Overview. */
export function EditChore() {
  const { id } = route.useParams()
  const { data: chore } = useSuspenseQuery(choreQuery(id))
  const navigate = useNavigate()

  return (
    <section className="mx-auto flex max-w-lg flex-col gap-4">
      <h1 className="text-xl font-semibold text-brand">Edit Chore</h1>
      <ChoreEditor chore={chore} onDone={() => void navigate({ to: '/' })} />
    </section>
  )
}

/** The Chore form filled in with a Chore, with Delete below it; `onDone` leaves once it is saved or deleted. */
export function ChoreEditor({ chore, onDone }: { chore: Chore; onDone: () => void }) {
  return (
    <>
      {/* Keyed, so moving to another Chore starts from that one's values */}
      <ChoreForm key={chore.id} chore={chore} onDone={onDone} />
      <div className="flex justify-center">
        <DeleteChore chore={chore} onDeleted={onDone} />
      </div>
    </>
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

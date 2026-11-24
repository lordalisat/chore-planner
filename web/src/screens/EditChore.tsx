import { useSuspenseQuery } from '@tanstack/react-query'
import { getRouteApi, Link, useNavigate, useRouter } from '@tanstack/react-router'
import { choreQuery, type Chore } from '../chores.ts'
import { ChoreForm } from './ChoreForm.tsx'
import { DeleteChore } from './DeleteChore.tsx'
import { Button } from '../ui/Button.tsx'

const route = getRouteApi('/authenticated/chores/$id')

/**
 * The Chore form, filled in with a Chore. Back, saving and deleting return to the screen it was opened from, or to
 * the Overview when it was opened directly.
 */
export function EditChore() {
  const { id } = route.useParams()
  const { data: chore } = useSuspenseQuery(choreQuery(id))
  const navigate = useNavigate()
  const router = useRouter()
  // Asked when leaving rather than through useCanGoBack, which would rerender this screen on its way out after a
  // delete and fetch the deleted Chore back into the cache
  const leave = () => (router.history.canGoBack() ? router.history.back() : void navigate({ to: '/' }))

  return (
    <section className="mx-auto flex max-w-lg flex-col gap-4">
      <div className="flex items-center gap-2">
        <Button onClick={leave} className="-ml-2 rounded-lg px-2 py-1 font-medium text-brand hover:bg-brand/10">
          <span aria-hidden>← </span>Back
        </Button>
        <h1 className="text-xl font-semibold text-brand">Edit Chore</h1>
      </div>
      <ChoreEditor chore={chore} onDone={leave} />
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
      <Button render={<Link to="/" className="font-semibold text-brand underline" />}>Back to the Overview</Button>
    </p>
  )
}

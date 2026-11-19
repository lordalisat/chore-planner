import { useMutation, useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { Link } from '@tanstack/react-router'
import { useState, type FormEvent } from 'react'
import { api, ApiError } from '../api/client.ts'
import { choresQuery, type Chore } from '../chores.ts'
import { relativeDate, today } from '../dates.ts'
import { DateField } from '../ui/DateField.tsx'
import { Dialog } from '../ui/Dialog.tsx'
import { Menu } from '../ui/Menu.tsx'
import { toast } from '../ui/toast.ts'
import { unreachableMessage } from './Unreachable.tsx'

/**
 * What needs doing, most urgent first, with One-off Chores that have no Due Date under Anytime at the bottom; Dormant
 * Chores wait out of sight until their Season, and finished ones are gone.
 */
export function Overview() {
  const { data: chores } = useSuspenseQuery(choresQuery)
  const complete = useComplete()
  const skip = useSkip()
  const postpone = usePostpone()
  const [doneOn, setDoneOn] = useState<Chore | null>(null)
  const [postponing, setPostponing] = useState<Chore | null>(null)
  const shown = chores
    .filter((chore) => !chore.dormant && !chore.finished)
    .sort((a, b) => (b.urgency ?? -1) - (a.urgency ?? -1))
  const scheduled = shown.filter((chore) => chore.dueDate)
  const anytime = shown.filter((chore) => !chore.dueDate)

  if (shown.length === 0) {
    return <p className="py-12 text-center text-ink/70">Nothing to do yet. Add a Chore to get started.</p>
  }
  const list = (chores: Chore[]) => (
    <ChoreList
      chores={chores}
      today={today()}
      onDone={(chore) => complete.mutate({ chore })}
      onDoneOn={setDoneOn}
      onSkip={(chore) => skip.mutate({ chore })}
      onPostpone={setPostponing}
    />
  )
  return (
    <div className="flex flex-col gap-6">
      {scheduled.length > 0 && list(scheduled)}
      {anytime.length > 0 && (
        <section className="flex flex-col gap-2">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-ink/60">Anytime</h2>
          {list(anytime)}
        </section>
      )}
      <Dialog open={doneOn !== null} onOpenChange={(open) => !open && setDoneOn(null)} title={`When was ${doneOn?.name ?? ''} done?`}>
        {/* Mounted only while open, so each starts on today */}
        {doneOn && (
          <DateForm
            label="Done on"
            initial={today()}
            missing="Pick the day it was done."
            field="date"
            save={(date, callbacks) => complete.mutate({ chore: doneOn, date }, callbacks)}
            onDone={() => setDoneOn(null)}
          />
        )}
      </Dialog>
      <Dialog open={postponing !== null} onOpenChange={(open) => !open && setPostponing(null)} title={`Postpone ${postponing?.name ?? ''}`}>
        {postponing && (
          <DateForm
            label="Postpone to"
            initial={null}
            missing="Pick the day to postpone to."
            field="to"
            save={(to, callbacks) => postpone.mutate({ chore: postponing, to }, callbacks)}
            onDone={() => setPostponing(null)}
          />
        )}
      </Dialog>
    </div>
  )
}

type RowActions = {
  onDone: (chore: Chore) => void
  onDoneOn: (chore: Chore) => void
  onSkip: (chore: Chore) => void
  onPostpone: (chore: Chore) => void
}

function ChoreList({ chores, today, ...actions }: { chores: Chore[]; today: string } & RowActions) {
  return (
    <ul className="flex flex-col gap-2">
      {chores.map((chore) => (
        <ChoreRow key={chore.id} chore={chore} today={today} {...actions} />
      ))}
    </ul>
  )
}

function ChoreRow({ chore, today, onDone, onDoneOn, onSkip, onPostpone }: { chore: Chore; today: string } & RowActions) {
  return (
    <li
      className={`flex items-center gap-3 rounded-lg border-l-4 bg-white py-2 pr-2 pl-4 shadow-sm hover:shadow-md ${
        chore.overdue ? 'border-red-700' : 'border-transparent'
      }`}
    >
      {/* Only the Chore itself opens its edit screen; Done and the menu sit beside the link, not in it */}
      <Link to="/chores/$id" params={{ id: chore.id }} className="flex flex-1 items-baseline gap-3 py-1">
        <span className="flex-1 font-medium">{chore.name}</span>
        {chore.area && <span className="text-sm text-ink/60">{chore.area.name}</span>}
        {chore.effort != null && <span className="text-sm text-ink/60">{chore.effort} min</span>}
        {chore.dueDate && (
          <span className={`text-sm ${chore.overdue ? 'font-semibold text-red-700' : 'text-ink/70'}`}>
            {relativeDate(chore.dueDate, today)}
          </span>
        )}
      </Link>
      <button type="button" onClick={() => onDone(chore)} className="rounded-lg bg-brand px-3 py-1.5 font-semibold text-white hover:bg-brand/90">
        Done
      </button>
      <Menu
        label={`More for ${chore.name}`}
        trigger="⋯"
        entries={[
          { label: 'Done on…', onSelect: () => onDoneOn(chore) },
          // Only a Recurring Chore has a next Due Date to move on to
          ...(chore.canSkip ? [{ label: 'Skip', onSelect: () => onSkip(chore) }] : []),
          { label: 'Postpone…', onSelect: () => onPostpone(chore) },
        ]}
      />
    </li>
  )
}

type DateFormProps = {
  label: string
  initial: string | null
  /** Shown when no day is picked. */
  missing: string
  /** The request field a validation problem names. */
  field: 'date' | 'to'
  save: (date: string, callbacks: { onSuccess: () => void; onError: (failure: Error) => void }) => void
  onDone: () => void
}

/** Asks for one day, for Done on… or Postpone…; a validation problem stays open at the date. */
function DateForm({ label, initial, missing, field, save, onDone }: DateFormProps) {
  const [date, setDate] = useState<string | null>(initial)
  const [error, setError] = useState<string>()

  function submit(event: FormEvent) {
    event.preventDefault()
    if (!date) return setError(missing)
    save(date, {
      onSuccess: onDone,
      // Anything but a validation problem was already toasted
      onError: (failure) => (failure instanceof ApiError && failure.status === 400 ? setError(failure.errors[field]?.[0]) : onDone()),
    })
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-4">
      <DateField label={label} value={date} onValueChange={setDate} error={error} />
      <button type="submit" className="rounded-lg bg-brand px-4 py-2 font-semibold text-white hover:bg-brand/90">
        Save
      </button>
    </form>
  )
}

/** Completes a Chore for the Due Date this device showed, today unless another date is given, then offers Undo. */
const useComplete = () =>
  useRecord(
    ({ chore, date }: { chore: Chore; date?: string }) =>
      api('/api/chores/{id}/complete', 'post', { dueDate: chore.dueDate, ...(date && { date }) }, { id: chore.id }),
    'Done',
    'Already done by someone else',
  )

/** Skips a Recurring Chore's Due Date this device showed, then offers Undo, like Complete. */
const useSkip = () =>
  useRecord(
    ({ chore }: { chore: Chore }) => api('/api/chores/{id}/skip', 'post', { dueDate: chore.dueDate }, { id: chore.id }),
    'Skipped',
    'Someone else got to it first',
  )

/**
 * Records a Completion, done or skipped, then offers Undo, toasting "`what`: Name". When someone else acted first the
 * API refuses, `conflict` says so, and the list is fetched again to show what they did.
 */
function useRecord<Variables extends { chore: Chore }>(
  record: (variables: Variables) => Promise<{ completionId: number }>,
  what: string,
  conflict: string,
) {
  const refetch = useRefetch()
  const undo = useUndo()

  return useMutation({
    mutationFn: record,
    onSuccess: ({ completionId }, { chore }) =>
      toast({
        title: `${what}: ${chore.name}`,
        action: { label: 'Undo', onAction: () => undo.mutateAsync({ choreId: chore.id, completionId }) },
      }),
    onError: failed(conflict),
    onSettled: refetch,
  })
}

/** Sets a Chore's Due Date by hand; no Undo, as postponing again puts a mistake right. */
function usePostpone() {
  const refetch = useRefetch()

  return useMutation({
    mutationFn: ({ chore, to }: { chore: Chore; to: string }) =>
      api('/api/chores/{id}/postpone', 'post', { dueDate: chore.dueDate, to }, { id: chore.id }),
    onError: failed('Someone else got to it first'),
    onSettled: refetch,
  })
}

/** Removes a Completion, done or skipped. */
function useUndo() {
  const refetch = useRefetch()
  return useMutation({
    mutationFn: ({ choreId, completionId }: { choreId: number; completionId: number }) =>
      api('/api/chores/{id}/completions/{completionId}', 'delete', { id: choreId, completionId }),
    onError: () => toast(unreachableMessage),
    onSettled: refetch,
  })
}

function useRefetch() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: choresQuery.queryKey })
}

/** Toasts why a command failed: `conflict` when someone else acted first (409), or that the server can't be reached. */
const failed = (conflict: string) => (error: Error) => {
  if (error instanceof ApiError && error.status === 409) toast(conflict)
  // A validation problem shows at its field, in the form that sent it
  else if (!(error instanceof ApiError && error.status === 400)) toast(unreachableMessage)
}

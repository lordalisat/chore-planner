import { useMutation, useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { api, ApiError, at } from '../api/client.ts'
import { choresQuery, type Chore } from '../chores.ts'
import { relativeDate, today } from '../dates.ts'
import { DateField } from '../ui/DateField.tsx'
import { Dialog } from '../ui/Dialog.tsx'
import { Menu } from '../ui/Menu.tsx'
import { toast } from '../ui/toast.ts'
import { unreachableMessage } from './Unreachable.tsx'

/** What needs doing, most urgent first; Dormant and finished Chores wait out of sight. */
export function Overview() {
  const { data: chores } = useSuspenseQuery(choresQuery)
  const complete = useComplete()
  const [doneOn, setDoneOn] = useState<Chore | null>(null)
  const shown = chores
    .filter((chore) => !chore.dormant && !chore.finished)
    .sort((a, b) => (b.urgency ?? -1) - (a.urgency ?? -1))

  if (shown.length === 0) {
    return <p className="py-12 text-center text-ink/70">Nothing to do yet. Add a Chore to get started.</p>
  }
  const now = today()
  return (
    <>
      <ul className="flex flex-col gap-2">
        {shown.map((chore) => (
          <ChoreRow
            key={chore.id}
            chore={chore}
            today={now}
            onDone={() => complete.mutate({ chore })}
            onDoneOn={() => setDoneOn(chore)}
          />
        ))}
      </ul>
      <Dialog open={doneOn !== null} onOpenChange={(open) => !open && setDoneOn(null)} title={`When was ${doneOn?.name ?? ''} done?`}>
        {/* Mounted only while open, so each starts on today */}
        {doneOn && <DoneOnForm chore={doneOn} onDone={() => setDoneOn(null)} complete={complete} />}
      </Dialog>
    </>
  )
}

function ChoreRow({ chore, today, onDone, onDoneOn }: { chore: Chore; today: string; onDone: () => void; onDoneOn: () => void }) {
  return (
    <li
      className={`flex items-center gap-3 rounded-lg border-l-4 bg-white py-2 pr-2 pl-4 shadow-sm ${
        chore.overdue ? 'border-red-700' : 'border-transparent'
      }`}
    >
      <span className="flex-1 font-medium">{chore.name}</span>
      {chore.effort != null && <span className="text-sm text-ink/60">{chore.effort} min</span>}
      {chore.dueDate && (
        <span className={`text-sm ${chore.overdue ? 'font-semibold text-red-700' : 'text-ink/70'}`}>
          {relativeDate(chore.dueDate, today)}
        </span>
      )}
      <button type="button" onClick={onDone} className="rounded-lg bg-brand px-3 py-1.5 font-semibold text-white hover:bg-brand/90">
        Done
      </button>
      <Menu label={`More for ${chore.name}`} trigger="⋯" entries={[{ label: 'Done on…', onSelect: onDoneOn }]} />
    </li>
  )
}

function DoneOnForm({ chore, onDone, complete }: { chore: Chore; onDone: () => void; complete: ReturnType<typeof useComplete> }) {
  const [date, setDate] = useState<string | null>(today())
  const [error, setError] = useState<string>()

  function submit(event: FormEvent) {
    event.preventDefault()
    if (!date) return setError('Pick the day it was done.')
    complete.mutate(
      { chore, date },
      {
        onSuccess: onDone,
        // A validation problem stays open at the date; anything else was already toasted
        onError: (failure) =>
          failure instanceof ApiError && failure.status === 400 ? setError(failure.errors.date?.[0]) : onDone(),
      },
    )
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-4">
      <DateField label="Done on" value={date} onValueChange={setDate} error={error} />
      <button type="submit" className="rounded-lg bg-brand px-4 py-2 font-semibold text-white hover:bg-brand/90">
        Save
      </button>
    </form>
  )
}

/**
 * Completes a Chore for the Due Date this device showed, today unless another date is given, then offers Undo. When
 * someone else acted first the API refuses, and the list is fetched again to show what they did.
 */
function useComplete() {
  const queryClient = useQueryClient()
  const refetch = () => queryClient.invalidateQueries({ queryKey: choresQuery.queryKey })

  const undo = useMutation({
    mutationFn: ({ choreId, completionId }: { choreId: number; completionId: number }) =>
      api(at('/api/chores/{id}/completions/{completionId}', { id: choreId, completionId }), 'delete'),
    onError: () => toast(unreachableMessage),
    onSettled: refetch,
  })

  return useMutation({
    mutationFn: ({ chore, date }: { chore: Chore; date?: string }) =>
      api(at('/api/chores/{id}/complete', { id: chore.id }), 'post', { dueDate: chore.dueDate, ...(date && { date }) }),
    onSuccess: ({ completionId }, { chore }) =>
      toast({
        title: `Done: ${chore.name}`,
        action: { label: 'Undo', onAction: () => undo.mutateAsync({ choreId: chore.id, completionId }) },
      }),
    onError: (error) => {
      if (error instanceof ApiError && error.status === 409) toast('Already done by someone else')
      // A validation problem shows at its field, in the form that sent it
      else if (!(error instanceof ApiError && error.status === 400)) toast(unreachableMessage)
    },
    onSettled: refetch,
  })
}

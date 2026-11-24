import { useMutation, useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { useNavigate } from '@tanstack/react-router'
import { useLayoutEffect, useRef, useState, type FormEvent } from 'react'
import { api, ApiError } from '../api/client.ts'
import { choresQuery, urgencyBand, type Chore, type UrgencyBand } from '../chores.ts'
import { relativeDate, shortDate, today } from '../dates.ts'
import { Button } from '../ui/Button.tsx'
import { Collapsible, CollapsiblePanel, CollapsibleTrigger } from '../ui/Collapsible.tsx'
import { DateField } from '../ui/DateField.tsx'
import { Dialog } from '../ui/Dialog.tsx'
import { TickBox } from '../ui/TickBox.tsx'
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
  const [expanded, setExpanded] = useState<number | null>(null)
  const shown = chores
    .filter((chore) => !chore.dormant && !chore.finished)
    // Equally urgent ones go by Due Date, earliest first
    .sort((a, b) => (b.urgency ?? -1) - (a.urgency ?? -1) || (a.dueDate ?? '').localeCompare(b.dueDate ?? ''))
  // Needing attention now; One-off Chores without a Due Date; and the rest, not urgent yet, folded away under Later
  const now = shown.filter((chore) => chore.dueDate && chore.urgency)
  const anytime = shown.filter((chore) => !chore.dueDate)
  const later = shown.filter((chore) => chore.dueDate && !chore.urgency)

  if (shown.length === 0) {
    return <p className="py-12 text-center text-ink/70">Nothing to do yet. Add a Chore to get started.</p>
  }
  const list = (chores: Chore[]) => (
    <ChoreList
      chores={chores}
      today={today()}
      expanded={expanded}
      onExpand={setExpanded}
      onDone={(chore) => complete.mutate({ chore })}
      onDoneOn={setDoneOn}
      onSkip={(chore) => skip.mutate({ chore })}
      onPostpone={setPostponing}
    />
  )
  return (
    <div className="flex flex-col gap-6">
      {now.length > 0 && list(now)}
      {now.length === 0 && anytime.length === 0 && <p className="py-6 text-center text-ink/70">Nothing needs doing yet.</p>}
      {anytime.length > 0 && (
        <section className="flex flex-col gap-2">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-ink/70">Anytime</h2>
          {list(anytime)}
        </section>
      )}
      {later.length > 0 && (
        <Collapsible render={<section />} aria-labelledby="later" className="flex flex-col gap-2">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-ink/70">
            <CollapsibleTrigger id="later" className="group flex items-center gap-1 uppercase hover:text-ink">
              <span aria-hidden className="inline-block transition-transform group-data-open:rotate-90">
                ›
              </span>
              Later ({later.length})
            </CollapsibleTrigger>
          </h2>
          <CollapsiblePanel>{list(later)}</CollapsiblePanel>
        </Collapsible>
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

type ListProps = { chores: Chore[]; today: string; expanded: number | null; onExpand: (id: number | null) => void } & RowActions

/** One row at a time is open to show its actions; any action folds it away again. */
function ChoreList({ chores, today, expanded, onExpand, ...actions }: ListProps) {
  return (
    <ul className="flex flex-col gap-2">
      {chores.map((chore) => (
        <ChoreRow
          key={chore.id}
          chore={chore}
          today={today}
          open={expanded === chore.id}
          onOpenChange={(open) => onExpand(open ? chore.id : null)}
          onClose={() => onExpand(null)}
          {...actions}
        />
      ))}
    </ul>
  )
}

// The row's left edge warms up as its Chore gets more urgent
const bandBorder: Record<UrgencyBand, string> = {
  none: 'border-transparent',
  soon: 'border-amber-400',
  due: 'border-orange-500',
  pressing: 'border-red-700',
}

type RowProps = { chore: Chore; today: string; open: boolean; onOpenChange: (open: boolean) => void; onClose: () => void } & RowActions

function ChoreRow({ chore, today, open, onOpenChange, onClose, onDone, onDoneOn, onSkip, onPostpone }: RowProps) {
  const navigate = useNavigate()
  const band = urgencyBand(chore.urgency)
  const details = [chore.area?.name, chore.effort != null ? `${chore.effort} min` : null].filter(Boolean).join(' · ')
  // Shown once something is ticked, so an untouched Chore's row stays as it was
  const ticked = chore.items.filter((item) => item.ticked).length
  const act = (action: (chore: Chore) => void) => () => {
    onClose()
    action(chore)
  }
  return (
    <Collapsible
      render={<li />}
      open={open}
      onOpenChange={(next) => onOpenChange(next)}
      data-urgency={band}
      className={`rounded-lg border-l-4 bg-white shadow-sm ${bandBorder[band]}`}
    >
      <div className="flex items-center gap-1 py-1 pr-3 pl-1">
        {/* A 44px target around the small round check, so a thumb finds it */}
        <Button aria-label={`Mark ${chore.name} done`} onClick={act(onDone)} className="group grid size-11 shrink-0 place-items-center">
          <span className="grid size-7 place-items-center rounded-full border-2 border-brand text-brand group-hover:bg-brand group-hover:text-white">
            <svg viewBox="0 0 16 16" aria-hidden className="size-4 opacity-0 group-hover:opacity-100">
              <path d="M3 8.5l3 3 7-7" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
          </span>
        </Button>
        <CollapsibleTrigger className="flex min-h-11 min-w-0 flex-1 flex-col justify-center text-left">
          <span className="flex w-full items-baseline gap-3">
            {/* A verb of its own, so naming the Chore alone can't also mean its Done button */}
            <span className="sr-only">Open</span>{' '}
            <span className="flex-1 truncate font-medium">{chore.name}</span>{' '}
            {details && <span className="text-sm text-ink/70">{details}</span>}
          </span>
          {/* Flex shows none of these spaces; they keep the words apart in the button's name */}{' '}
          {(chore.dueDate || ticked > 0) && (
            <span className="text-sm text-ink/70">
              {chore.dueDate && (
                <span className={chore.overdue ? 'font-semibold text-red-700' : undefined}>{relativeDate(chore.dueDate, today)}</span>
              )}
              {chore.dueDate && ticked > 0 && ' · '}
              {ticked > 0 && `${ticked} of ${chore.items.length} items`}
            </span>
          )}
        </CollapsibleTrigger>
      </div>
      <CollapsiblePanel>
        {chore.description && <Description text={chore.description} />}
        {chore.items.length > 0 && <Items chore={chore} />}
        <div className="flex flex-wrap gap-2 border-t border-black/5 px-3 py-2">
          <RowAction onClick={act(onDoneOn)}>Done on…</RowAction>
          {/* Only a Recurring Chore has a next Due Date to move on to */}
          {chore.canSkip && <RowAction onClick={act(onSkip)}>Skip</RowAction>}
          <RowAction onClick={act(onPostpone)}>Postpone…</RowAction>
          <RowAction onClick={() => void navigate({ to: '/chores/$id', params: { id: chore.id } })}>Edit</RowAction>
        </div>
      </CollapsiblePanel>
    </Collapsible>
  )
}

/** A Chore's Description, capped at three lines until Show more; the button shows only when the text is cut off. */
function Description({ text }: { text: string }) {
  const paragraph = useRef<HTMLParagraphElement>(null)
  const [cutOff, setCutOff] = useState(false)
  const [whole, setWhole] = useState(false)
  // A changed Description starts capped again
  const [shown, setShown] = useState(text)
  if (text !== shown) {
    setShown(text)
    setWhole(false)
  }
  // Measured while capped, and again whenever its size changes (a turned phone, a resized window): taller than its
  // three lines means some is hidden
  useLayoutEffect(() => {
    const element = paragraph.current
    if (!element || whole) return
    const measure = () => setCutOff(element.scrollHeight > element.clientHeight)
    measure()
    if (typeof ResizeObserver === 'undefined') return
    const observer = new ResizeObserver(measure)
    observer.observe(element)
    return () => observer.disconnect()
  }, [text, whole])

  return (
    <div className="flex flex-col items-start border-t border-black/5 px-3 py-2">
      <p ref={paragraph} className={`text-sm whitespace-pre-line text-ink/80 ${whole ? '' : 'line-clamp-3'}`}>
        {text}
      </p>
      {cutOff && (
        <Button
          aria-expanded={whole}
          onClick={() => setWhole(!whole)}
          // A 44px tap target that overlaps the padding around it, so it doesn't push the row apart
          className="-mx-2 -my-2 min-h-11 px-2 text-sm font-medium text-brand hover:underline"
        >
          {whole ? 'Show less' : 'Show more'}
        </Button>
      )}
    </div>
  )
}

/** A Chore's Items to tick while working on it; tapping one with a Description shows it. */
function Items({ chore }: { chore: Chore }) {
  const queryClient = useQueryClient()
  const [shown, setShown] = useState<number | null>(null)
  const tick = useMutation({
    mutationFn: ({ itemId, ticked }: { itemId: number; ticked: boolean }) =>
      api('/api/chores/{id}/items/{itemId}', 'put', { ticked }, { id: chore.id, itemId }),
    // Gone when someone removed the Item since; the list then shows what is there now
    onError: (error) =>
      error instanceof ApiError && error.status === 404
        ? toast.info(`${chore.name} changed since; here it is as it is now.`)
        : toast.error(unreachableMessage),
    onSettled: () => queryClient.invalidateQueries({ queryKey: choresQuery.queryKey }),
  })

  return (
    <ul className="flex flex-col border-t border-black/5 px-1 py-1">
      {chore.items.map((item) => {
        const box = <TickBox label={item.name} checked={item.ticked} onCheckedChange={(ticked) => tick.mutate({ itemId: item.id, ticked })} />
        if (!item.description) {
          return (
            <li key={item.id} className="flex items-center gap-1">
              {box}
              <span className={`flex-1 text-sm ${tickedName(item.ticked)}`}>{item.name}</span>
            </li>
          )
        }
        return (
          <Collapsible
            key={item.id}
            render={<li />}
            open={shown === item.id}
            onOpenChange={(open) => setShown(open ? item.id : null)}
            className="flex flex-col"
          >
            <div className="flex items-center gap-1">
              {box}
              <CollapsibleTrigger className="flex min-h-11 flex-1 items-center gap-2 text-left text-sm">
                {/* Named apart from the Item's tick box, which has the bare Name */}
                <span className="sr-only">Description of</span>{' '}
                <span className={tickedName(item.ticked)}>{item.name}</span>
                {/* Outside the struck-through Name, so a ticked Item still shows it has a Description */}
                <span aria-hidden className="rounded bg-black/5 px-1.5 text-xs text-ink/70">
                  {shown === item.id ? 'less' : 'more'}
                </span>
              </CollapsibleTrigger>
            </div>
            <CollapsiblePanel className="pb-2 pl-12 pr-3 text-sm whitespace-pre-line text-ink/80">{item.description}</CollapsiblePanel>
          </Collapsible>
        )
      })}
    </ul>
  )
}

// A ticked Item reads as done
const tickedName = (ticked: boolean) => (ticked ? 'text-ink/70 line-through' : '')

function RowAction({ onClick, children }: { onClick: () => void; children: string }) {
  return (
    <Button onClick={onClick} className="min-h-11 rounded-lg border border-brand/80 px-4 text-sm font-medium text-brand hover:bg-brand/10">
      {children}
    </Button>
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
      <Button type="submit" className="rounded-lg bg-brand px-4 py-2 font-semibold text-white hover:bg-brand/90">
        Save
      </Button>
    </form>
  )
}

/** Completes a Chore for the Due Date this device showed, today unless another date is given, then offers Undo. */
const useComplete = () =>
  useRecord(
    ({ chore, date }: { chore: Chore; date?: string }) =>
      api('/api/chores/{id}/complete', 'post', { dueDate: chore.dueDate, ...(date && { date }) }, { id: chore.id }),
    'Done',
    'success',
    'Already done by someone else',
  )

/** Skips a Recurring Chore's Due Date this device showed, then offers Undo, like Complete. */
const useSkip = () =>
  useRecord(
    ({ chore }: { chore: Chore }) => api('/api/chores/{id}/skip', 'post', { dueDate: chore.dueDate }, { id: chore.id }),
    'Skipped',
    'info',
    'Someone else got to it first',
  )

/**
 * Records a Completion, done or skipped, then offers Undo, toasting "`what`: Name". When someone else acted first the
 * API refuses, `conflict` says so, and the list is fetched again to show what they did.
 */
function useRecord<Variables extends { chore: Chore }>(
  record: (variables: Variables) => Promise<{ completionId: number }>,
  what: string,
  type: 'success' | 'info',
  conflict: string,
) {
  const refetch = useRefetch()
  const undo = useUndo()

  return useMutation({
    mutationFn: record,
    onSuccess: ({ completionId }, { chore }) =>
      toast({
        type,
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
    onSuccess: (_, { chore, to }) => toast.info(`Postponed: ${chore.name} to ${shortDate(to)}`),
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
    onError: () => toast.error(unreachableMessage),
    onSettled: refetch,
  })
}

function useRefetch() {
  const queryClient = useQueryClient()
  return () => queryClient.invalidateQueries({ queryKey: choresQuery.queryKey })
}

/** Toasts why a command failed: `conflict` when someone else acted first (409), or that the server can't be reached. */
const failed = (conflict: string) => (error: Error) => {
  if (error instanceof ApiError && error.status === 409) toast.error(conflict)
  // A validation problem shows at its field, in the form that sent it
  else if (!(error instanceof ApiError && error.status === 400)) toast.error(unreachableMessage)
}

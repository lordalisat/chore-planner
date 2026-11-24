import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useRef, useState, type FormEvent } from 'react'
import { api, ApiError, type FieldErrors } from '../api/client.ts'
import type { components } from '../api/schema'
import { areasQuery } from '../areas.ts'
import {
  choresQuery,
  kindLabels,
  limits,
  unitLabels,
  ordinalNames,
  toWeekdayInMonth,
  weekdayInMonth,
  weekdayNames,
  type Chore,
  type Ordinal,
  type PreferredDay,
  type Unit,
} from '../chores.ts'
import { DateField } from '../ui/DateField.tsx'
import { NumberField } from '../ui/NumberField.tsx'
import { scrollToTop } from '../ui/scrollToTop.ts'
import { SegmentedField } from '../ui/SegmentedField.tsx'
import { SelectField, type Option } from '../ui/SelectField.tsx'
import { SwitchField } from '../ui/SwitchField.tsx'
import { TextAreaField } from '../ui/TextAreaField.tsx'
import { TextField } from '../ui/TextField.tsx'
import { toast } from '../ui/toast.ts'
import { itemDraft, ItemsField, type ItemDraft } from './ItemsField.tsx'
import { unreachableMessage } from './Unreachable.tsx'

type RecurringRequest = components['schemas']['CreateRecurringChoreRequest']
type OneOffRequest = components['schemas']['CreateOneOffChoreRequest']
type EditRequest = components['schemas']['EditChoreRequest']
type Kind = components['schemas']['ChoreDto']['kind']
type Importance = NonNullable<RecurringRequest['importance']>
type Submission = { another: boolean } & (
  | { kind: 'Recurring'; request: RecurringRequest }
  | { kind: 'OneOff'; request: OneOffRequest }
)

const kinds: Option<Kind>[] = (['Recurring', 'OneOff'] as const).map((kind) => ({ value: kind, label: kindLabels[kind] }))
const units: Option<Unit>[] = (['Day', 'Week', 'Month', 'Year'] as const).map((unit) => ({ value: unit, label: unitLabels[unit] }))
// What the Preferred day list offers: a Preferred Day, or the way to a weekday in the month, picked in two more lists
type DayChoice = PreferredDay | 'WeekdayInMonth'
const weekdays = weekdayNames.map((day) => ({ value: day, label: day }))
const daysInMonth: Option<DayChoice>[] = [
  { value: 'FirstOfMonth', label: 'First of the month' },
  { value: 'LastOfMonth', label: 'Last of the month' },
  { value: 'WeekdayInMonth', label: 'A weekday in the month…' },
]
const ordinals: Option<Ordinal>[] = ordinalNames.map((ordinal) => ({ value: ordinal, label: ordinal }))
// A weekday in the month starts as the first Saturday
const firstWeekdayInMonth = toWeekdayInMonth('First', 'Saturday')

/** The Preferred day list's choice for a Preferred Day: a weekday in the month shows as the way to it. */
const dayChoice = (day: PreferredDay | null): DayChoice | null => (weekdayInMonth(day) ? 'WeekdayInMonth' : day)
const months: Option<string>[] = [
  'January', 'February', 'March', 'April', 'May', 'June',
  'July', 'August', 'September', 'October', 'November', 'December',
].map((month, index) => ({ value: String(index + 1), label: month }))
const importances: Option<Importance>[] = [
  { value: 'Low', label: 'Low' },
  { value: 'Normal', label: 'Normal' },
  { value: 'High', label: 'High' },
]

// The Preferred Days an Interval's unit allows, as the API's own rule: a weekday needs weeks, months or years, the
// first or last of the month, or a weekday in it, months or years
function preferredDaysFor(unit: Unit): Option<DayChoice>[] {
  return [...(unit === 'Day' ? [] : weekdays), ...(unit === 'Month' || unit === 'Year' ? daysInMonth : [])]
}

/** Creates a Recurring or One-off Chore, or edits the given `chore`, whose kind stays; `onDone` closes it. */
export function ChoreForm({ chore, onDone }: { chore?: Chore; onDone: () => void }) {
  const queryClient = useQueryClient()
  const [kind, setKind] = useState<Kind>(chore?.kind ?? 'Recurring')
  const [name, setName] = useState(chore?.name ?? '')
  const [description, setDescription] = useState(chore?.description ?? '')
  const [items, setItems] = useState<ItemDraft[]>(() => chore?.items.map(itemDraft) ?? [])
  const [count, setCount] = useState<number | null>(chore?.interval?.count ?? 1)
  const [unit, setUnit] = useState<Unit>(chore?.interval?.unit ?? 'Week')
  const [preferredDay, setPreferredDay] = useState<PreferredDay | null>(chore?.preferredDay ?? null)
  // Off is all year; the months are kept while off, so switching back on restores them
  const [seasonal, setSeasonal] = useState(!!chore?.season)
  const [seasonFrom, setSeasonFrom] = useState<string | null>(chore?.season ? String(chore.season.fromMonth) : null)
  const [seasonTo, setSeasonTo] = useState<string | null>(chore?.season ? String(chore.season.toMonth) : null)
  const [importance, setImportance] = useState<Importance>(chore?.importance ?? 'Normal')
  const [effort, setEffort] = useState<number | null>(chore?.effort ?? null)
  // The list only takes text, so the id travels as one
  const [areaId, setAreaId] = useState<string | null>(chore?.area ? String(chore.area.id) : null)
  // The Chore's own Area until the list has come, so its name shows from the start
  const { data: areas = chore?.area ? [chore.area] : [] } = useQuery(areasQuery)
  const areaOptions: Option<string>[] = areas.map((area) => ({ value: String(area.id), label: area.name }))
  const [lastDone, setLastDone] = useState<string | null>(null)
  const [dueDate, setDueDate] = useState<string | null>(chore?.kind === 'OneOff' ? (chore.dueDate ?? null) : null)
  const [errors, setErrors] = useState<FieldErrors>({})
  const nameInput = useRef<HTMLInputElement>(null)
  const form = useRef<HTMLFormElement>(null)

  const preferredDays = preferredDaysFor(unit)
  const inMonth = weekdayInMonth(preferredDay)

  function changeUnit(next: Unit) {
    setUnit(next)
    if (!preferredDaysFor(next).some((day) => day.value === dayChoice(preferredDay))) setPreferredDay(null)
  }

  const save = useMutation({
    mutationFn: (submission: Submission) => {
      if (chore) {
        // Once a Chore exists, its Completions are what change Last done
        const { lastDone: _, ...edit }: Partial<RecurringRequest> & OneOffRequest = submission.request
        return api('/api/chores/{id}', 'put', edit satisfies EditRequest, { id: chore.id })
      }
      return submission.kind === 'Recurring'
        ? api('/api/chores/recurring', 'post', submission.request)
        : api('/api/chores/one-off', 'post', submission.request)
    },
    onSuccess: (_, { request, another }) => {
      void queryClient.invalidateQueries({ queryKey: choresQuery.queryKey })
      if (!another) return onDone()
      // Setting up many Chores at once: the next one most likely shares the schedule
      toast.success(`Added ${request.name}`)
      setName('')
      setDescription('')
      setItems([])
      setImportance('Normal')
      setEffort(null)
      setLastDone(null)
      setDueDate(null)
      // Back to the top for the next one, where the title and Name are
      nameInput.current?.focus({ preventScroll: true })
      if (form.current) scrollToTop(form.current)
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 400) setErrors(error.errors)
      else toast.error(unreachableMessage)
    },
  })

  function submit(event: FormEvent, another = false) {
    event.preventDefault()
    const area = areaId === null ? null : Number(areaId)
    const itemRequests = items.map(({ id, name, description }) => ({ id, name, description }))
    if (kind === 'OneOff') {
      setErrors({})
      save.mutate({ another, kind, request: { name, description, items: itemRequests, dueDate, importance, effort, areaId: area } })
      return
    }
    // A Season needs both months; the API can only judge a whole one
    if (seasonal && (seasonFrom === null || seasonTo === null)) {
      setErrors({ season: ['Pick the months the Season runs from and to.'] })
      return
    }
    setErrors({})
    save.mutate({
      another,
      kind,
      request: {
        name,
        description,
        items: itemRequests,
        interval: { count: count ?? 0, unit },
        preferredDay,
        season: seasonal && seasonFrom && seasonTo ? { fromMonth: Number(seasonFrom), toMonth: Number(seasonTo) } : null,
        importance,
        effort,
        lastDone,
        areaId: area,
      },
    })
  }

  return (
    <form ref={form} onSubmit={submit} className="flex flex-col gap-4">
      {/* A Chore's kind can't change */}
      {!chore && <SegmentedField label="Kind" options={kinds} value={kind} onValueChange={setKind} />}
      <TextField ref={nameInput} label="Name" autoFocus value={name} onValueChange={setName} error={errors.name?.[0]} />
      <TextAreaField
        label="Description"
        value={description}
        onValueChange={setDescription}
        maxLength={limits.description}
        error={errors.description?.[0]}
      />
      <ItemsField items={items} onItemsChange={setItems} error={errors.items?.[0]} />
      {kind === 'OneOff' && (
        <DateField label="Due date" value={dueDate} onValueChange={setDueDate} error={errors.dueDate?.[0]} />
      )}
      {kind === 'Recurring' && (
        <>
          <div className="flex flex-col gap-1">
            <div className="flex items-end gap-2">
              <NumberField label="Every" min={1} value={count} onValueChange={setCount} invalid={!!errors.interval} className="w-24" />
              <SelectField label="Unit" hideLabel options={units} value={unit} onValueChange={(next) => next && changeUnit(next)} invalid={!!errors.interval} className="flex-1" />
            </div>
            {errors.interval && <p className="text-sm text-red-700">{errors.interval[0]}</p>}
          </div>
          {/* Nothing to prefer for an Interval in days */}
          {preferredDays.length > 0 && (
            <SelectField
              label="Preferred day"
              options={preferredDays}
              value={dayChoice(preferredDay)}
              onValueChange={(choice) =>
                setPreferredDay(choice === 'WeekdayInMonth' ? (inMonth ? preferredDay : firstWeekdayInMonth) : choice)
              }
              noneLabel="Any day"
              error={inMonth ? undefined : errors.preferredDay?.[0]}
            />
          )}
          {inMonth && (
            <div className="flex flex-col gap-1">
              <div className="flex items-center gap-2">
                <SelectField
                  label={`Which ${inMonth.day}`}
                  hideLabel
                  options={ordinals}
                  value={inMonth.ordinal}
                  onValueChange={(ordinal) => ordinal && setPreferredDay(toWeekdayInMonth(ordinal, inMonth.day))}
                  invalid={!!errors.preferredDay}
                  className="flex-1"
                />
                <SelectField
                  label="Weekday"
                  hideLabel
                  options={weekdays}
                  value={inMonth.day}
                  onValueChange={(day) => day && setPreferredDay(toWeekdayInMonth(inMonth.ordinal, day))}
                  invalid={!!errors.preferredDay}
                  className="flex-1"
                />
                <span className="text-sm text-ink/70">of the month</span>
              </div>
              {errors.preferredDay && <p className="text-sm text-red-700">{errors.preferredDay[0]}</p>}
            </div>
          )}
          <div className="flex flex-col gap-2">
            <SwitchField
              label="Seasonal"
              description={seasonal ? 'Only due between these months' : 'Due all year'}
              checked={seasonal}
              onCheckedChange={setSeasonal}
            />
            {seasonal && (
              <div className="flex gap-2">
                <SelectField label="Season from" options={months} value={seasonFrom} onValueChange={setSeasonFrom} placeholder="Month" invalid={!!errors.season} className="flex-1" />
                <SelectField label="Season to" options={months} value={seasonTo} onValueChange={setSeasonTo} placeholder="Month" invalid={!!errors.season} className="flex-1" />
              </div>
            )}
            {errors.season && <p className="text-sm text-red-700">{errors.season[0]}</p>}
          </div>
        </>
      )}
      <SelectField label="Area" options={areaOptions} value={areaId} onValueChange={setAreaId} noneLabel="No Area" error={errors.areaId?.[0]} />
      <SegmentedField label="Importance"options={importances} value={importance} onValueChange={setImportance} />
      <NumberField label="Effort in minutes" min={0} value={effort} onValueChange={setEffort} error={errors.effort?.[0]} />
      {kind === 'Recurring' && !chore && (
        <DateField label="Last done" value={lastDone} onValueChange={setLastDone} error={errors.lastDone?.[0]} />
      )}
      <div className="flex flex-wrap gap-2">
        <button
          type="submit"
          disabled={save.isPending}
          className="flex-1 rounded-lg bg-brand px-4 py-2 font-semibold text-white hover:bg-brand/90 disabled:opacity-60"
        >
          Save
        </button>
        {!chore && (
          <button
            type="button"
            disabled={save.isPending}
            onClick={(event) => submit(event, true)}
            className="flex-1 rounded-lg border border-brand px-4 py-2 font-semibold text-brand hover:bg-brand/5 disabled:opacity-60"
          >
            Save and add another
          </button>
        )}
      </div>
    </form>
  )
}

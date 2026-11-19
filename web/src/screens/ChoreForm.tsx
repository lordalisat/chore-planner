import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useRef, useState, type FormEvent } from 'react'
import { api, ApiError, type FieldErrors } from '../api/client.ts'
import type { components } from '../api/schema'
import { choresQuery, type Chore } from '../chores.ts'
import { DateField } from '../ui/DateField.tsx'
import { NumberField } from '../ui/NumberField.tsx'
import { SegmentedField } from '../ui/SegmentedField.tsx'
import { SelectField, type Option } from '../ui/SelectField.tsx'
import { SwitchField } from '../ui/SwitchField.tsx'
import { TextField } from '../ui/TextField.tsx'
import { toast } from '../ui/toast.ts'
import { unreachableMessage } from './Unreachable.tsx'

type Request = components['schemas']['CreateRecurringChoreRequest']
type EditRequest = components['schemas']['EditChoreRequest']
type Unit = Request['interval']['unit']
type PreferredDay = NonNullable<Request['preferredDay']>
type Importance = NonNullable<Request['importance']>

const units: Option<Unit>[] = [
  { value: 'Day', label: 'days' },
  { value: 'Week', label: 'weeks' },
  { value: 'Month', label: 'months' },
  { value: 'Year', label: 'years' },
]
const weekdays: Option<PreferredDay>[] = (
  ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'] as const
).map((day) => ({ value: day, label: day }))
const daysInMonth: Option<PreferredDay>[] = [
  { value: 'FirstOfMonth', label: 'First of the month' },
  { value: 'LastOfMonth', label: 'Last of the month' },
]
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
// first or last of the month months or years
function preferredDaysFor(unit: Unit): Option<PreferredDay>[] {
  return [...(unit === 'Day' ? [] : weekdays), ...(unit === 'Month' || unit === 'Year' ? daysInMonth : [])]
}

/** Creates a Recurring Chore, or edits the given `chore`; `onDone` closes it. */
export function ChoreForm({ chore, onDone }: { chore?: Chore; onDone: () => void }) {
  const queryClient = useQueryClient()
  const [name, setName] = useState(chore?.name ?? '')
  const [count, setCount] = useState<number | null>(chore?.interval?.count ?? 1)
  const [unit, setUnit] = useState<Unit>(chore?.interval?.unit ?? 'Week')
  const [preferredDay, setPreferredDay] = useState<PreferredDay | null>(chore?.preferredDay ?? null)
  // Off is all year; the months are kept while off, so switching back on restores them
  const [seasonal, setSeasonal] = useState(!!chore?.season)
  const [seasonFrom, setSeasonFrom] = useState<string | null>(chore?.season ? String(chore.season.fromMonth) : null)
  const [seasonTo, setSeasonTo] = useState<string | null>(chore?.season ? String(chore.season.toMonth) : null)
  const [importance, setImportance] = useState<Importance>(chore?.importance ?? 'Normal')
  const [effort, setEffort] = useState<number | null>(chore?.effort ?? null)
  const [lastDone, setLastDone] = useState<string | null>(null)
  const [errors, setErrors] = useState<FieldErrors>({})
  const nameInput = useRef<HTMLInputElement>(null)

  const preferredDays = preferredDaysFor(unit)

  function changeUnit(next: Unit) {
    setUnit(next)
    if (!preferredDaysFor(next).some((day) => day.value === preferredDay)) setPreferredDay(null)
  }

  const save = useMutation({
    mutationFn: ({ request }: { request: Request; another: boolean }) => {
      if (!chore) return api('/api/chores/recurring', 'post', request)
      const { lastDone: _, ...edit } = request
      return api('/api/chores/{id}', 'put', edit satisfies EditRequest, { id: chore.id })
    },
    onSuccess: (_, { request, another }) => {
      void queryClient.invalidateQueries({ queryKey: choresQuery.queryKey })
      if (!another) return onDone()
      // Setting up many Chores at once: the next one most likely shares the schedule
      toast(`Added ${request.name}`)
      setName('')
      setImportance('Normal')
      setEffort(null)
      setLastDone(null)
      nameInput.current?.focus()
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 400) setErrors(error.errors)
      else toast(unreachableMessage)
    },
  })

  function submit(event: FormEvent, another = false) {
    event.preventDefault()
    // A Season needs both months; the API can only judge a whole one
    if (seasonal && (seasonFrom === null || seasonTo === null)) {
      setErrors({ season: ['Pick the months the Season runs from and to.'] })
      return
    }
    setErrors({})
    save.mutate({
      another,
      request: {
        name,
        interval: { count: count ?? 0, unit },
        preferredDay,
        season: seasonal && seasonFrom && seasonTo ? { fromMonth: Number(seasonFrom), toMonth: Number(seasonTo) } : null,
        importance,
        effort,
        lastDone,
      },
    })
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-4">
      <TextField ref={nameInput} label="Name" autoFocus value={name} onValueChange={setName} error={errors.name?.[0]} />
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
          value={preferredDay}
          onValueChange={setPreferredDay}
          noneLabel="Any day"
          error={errors.preferredDay?.[0]}
        />
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
      <SegmentedField label="Importance" options={importances} value={importance} onValueChange={setImportance} />
      <NumberField label="Effort in minutes" min={0} value={effort} onValueChange={setEffort} error={errors.effort?.[0]} />
      {/* Once a Chore exists, its Completions are what change Last done */}
      {!chore && <DateField label="Last done" value={lastDone} onValueChange={setLastDone} error={errors.lastDone?.[0]} />}
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

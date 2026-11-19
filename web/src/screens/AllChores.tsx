import { useMutation, useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { getRouteApi, Link } from '@tanstack/react-router'
import { useState } from 'react'
import { api } from '../api/client.ts'
import { archivedChoresQuery, choresQuery, type Chore } from '../chores.ts'
import { longDate, relativeDate, today } from '../dates.ts'
import { toast } from '../ui/toast.ts'
import { unreachableMessage } from './Unreachable.tsx'

const route = getRouteApi('/authenticated/chores')

const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
const unitNames = { Day: 'day', Week: 'week', Month: 'month', Year: 'year' }
const preferredDayNames: Record<NonNullable<Chore['preferredDay']>, string> = {
  Monday: 'Monday',
  Tuesday: 'Tuesday',
  Wednesday: 'Wednesday',
  Thursday: 'Thursday',
  Friday: 'Friday',
  Saturday: 'Saturday',
  Sunday: 'Sunday',
  FirstOfMonth: 'the first',
  LastOfMonth: 'the last',
}

/** "every 2 weeks on Tuesday", or nothing for a One-off Chore. */
function schedule({ interval, preferredDay }: Chore) {
  if (!interval) return ''
  const unit = unitNames[interval.unit]
  const every = interval.count === 1 ? `every ${unit}` : `every ${interval.count} ${unit}s`
  return preferredDay ? `${every} on ${preferredDayNames[preferredDay]}` : every
}

const seasonName = ({ season }: Chore) =>
  season ? `${monthNames[season.fromMonth - 1]} – ${monthNames[season.toMonth - 1]}` : ''
const kindName = ({ kind }: Chore) => (kind === 'OneOff' ? 'One-off' : 'Recurring')
const daysPerUnit = { Day: 1, Week: 7, Month: 30, Year: 365 }
const importanceRank = { Low: 0, Normal: 1, High: 2 }

type Column = 'Name' | 'Kind' | 'Interval' | 'Season' | 'Importance' | 'Effort' | 'Due'
// What each column sorts by; null is blank, which sorts last either way
const sortKeys: Record<Column, (chore: Chore) => string | number | null> = {
  Name: (chore) => chore.name,
  Kind: kindName,
  Interval: ({ interval }) => (interval ? interval.count * daysPerUnit[interval.unit] : null),
  Season: ({ season }) => season?.fromMonth ?? null,
  Importance: ({ importance }) => importanceRank[importance],
  Effort: ({ effort }) => effort,
  Due: ({ dueDate }) => dueDate,
}
const columns = Object.keys(sortKeys) as Column[]

type Sort = { column: Column; descending: boolean }

// Ties go by Name
function sortChores(chores: Chore[], { column, descending }: Sort) {
  const key = sortKeys[column]
  return [...chores].sort((a, b) => {
    const [x, y] = [key(a), key(b)]
    if (x !== y) {
      if (x === null) return 1
      if (y === null) return -1
      const order = typeof x === 'string' ? x.localeCompare(String(y)) : x - Number(y)
      return descending ? -order : order
    }
    return a.name.localeCompare(b.name)
  })
}

const filterClass =
  'rounded-lg px-3 py-1.5 text-sm font-semibold text-brand aria-[current=page]:bg-brand aria-[current=page]:text-white'

/** Every Chore in one place, including the ones the Overview hides; Archived ones under their own filter. */
export function AllChores() {
  const { archived } = route.useSearch()

  return (
    <section className="flex flex-col gap-4">
      <div className="flex flex-wrap items-center gap-2">
        <h1 className="flex-1 text-xl font-semibold text-brand">All Chores</h1>
        <nav aria-label="Filter" className="flex gap-1 rounded-xl bg-white p-1 shadow-sm">
          <Link to="/chores" aria-current={archived ? undefined : 'page'} className={filterClass}>
            Active
          </Link>
          <Link to="/chores" search={{ archived: true }} aria-current={archived ? 'page' : undefined} className={filterClass}>
            Archived
          </Link>
        </nav>
      </div>
      {archived ? <ArchivedChores /> : <ActiveChores />}
    </section>
  )
}

function ActiveChores() {
  const { data: chores } = useSuspenseQuery(choresQuery)
  const [sort, setSort] = useState<Sort>({ column: 'Name', descending: false })
  const sorted = sortChores(chores, sort)
  const now = today()

  if (chores.length === 0) return <p className="py-12 text-center text-ink/70">No Chores yet.</p>
  return (
    <>
      {/* A phone gets a plain list */}
      <ul className="flex flex-col gap-2 md:hidden">
        {sorted.map((chore) => (
          <li key={chore.id}>
            <Link
              to="/chores/$id"
              params={{ id: chore.id }}
              className="flex flex-col gap-0.5 rounded-lg bg-white px-4 py-3 shadow-sm hover:shadow-md"
            >
              <span className="flex items-baseline gap-2">
                <span className="flex-1 font-medium">{chore.name}</span>
                <Marks chore={chore} />
                {chore.dueDate && <span className="text-sm text-ink/70">{relativeDate(chore.dueDate, now)}</span>}
              </span>
              <span className="text-sm text-ink/60">
                {[kindName(chore), schedule(chore), seasonName(chore)].filter(Boolean).join(' · ')}
              </span>
            </Link>
          </li>
        ))}
      </ul>
      <table aria-label="All Chores" className="hidden w-full rounded-lg bg-white text-left text-sm shadow-sm md:table">
        <thead className="border-b border-black/10 text-ink/70">
          <tr>
            {columns.map((column) => {
              const active = sort.column === column
              return (
                <th key={column} aria-sort={active ? (sort.descending ? 'descending' : 'ascending') : undefined} className="font-semibold">
                  <button
                    type="button"
                    onClick={() => setSort({ column, descending: active && !sort.descending })}
                    className="w-full px-3 py-2 text-left hover:text-brand"
                  >
                    {column}
                    <span aria-hidden="true">{active ? (sort.descending ? ' ↓' : ' ↑') : ''}</span>
                  </button>
                </th>
              )
            })}
          </tr>
        </thead>
        <tbody>
          {sorted.map((chore) => (
            <tr key={chore.id} className="border-b border-black/5 last:border-0 hover:bg-brand/5">
              <td className="px-3 py-2">
                <Link to="/chores/$id" params={{ id: chore.id }} className="font-medium hover:underline">
                  {chore.name}
                </Link>{' '}
                <Marks chore={chore} />
              </td>
              <td className="px-3 py-2">{kindName(chore)}</td>
              <td className="px-3 py-2">{schedule(chore)}</td>
              <td className="px-3 py-2">{seasonName(chore)}</td>
              <td className="px-3 py-2">{chore.importance}</td>
              <td className="px-3 py-2">{chore.effort != null && `${chore.effort} min`}</td>
              <td className="px-3 py-2">{chore.dueDate && relativeDate(chore.dueDate, now)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  )
}

function Marks({ chore }: { chore: Chore }) {
  const mark = chore.dormant ? 'Dormant' : chore.finished ? 'Done' : null
  return mark && <span className="rounded-full bg-black/5 px-2 py-0.5 text-xs text-ink/70">{mark}</span>
}

/** Read-only: an Archived Chore can only be Restored. */
function ArchivedChores() {
  const queryClient = useQueryClient()
  const { data: chores } = useSuspenseQuery(archivedChoresQuery)
  const restore = useMutation({
    mutationFn: (chore: Chore) => api('/api/chores/{id}/restore', 'post', { id: chore.id }),
    onSuccess: (_, chore) => {
      void queryClient.invalidateQueries({ queryKey: choresQuery.queryKey })
      toast(`Restored ${chore.name}`)
    },
    onError: () => toast(unreachableMessage),
  })

  if (chores.length === 0) return <p className="py-12 text-center text-ink/70">Nothing is Archived.</p>
  return (
    <table aria-label="Archived Chores" className="w-full rounded-lg bg-white text-left text-sm shadow-sm">
      <thead className="border-b border-black/10 text-ink/70">
        <tr>
          <th className="px-3 py-2 font-semibold">Name</th>
          <th className="px-3 py-2 font-semibold">Archived</th>
          <th className="px-3 py-2">
            <span className="sr-only">Actions</span>
          </th>
        </tr>
      </thead>
      <tbody>
        {[...chores]
          .sort((a, b) => a.name.localeCompare(b.name))
          .map((chore) => (
            <tr key={chore.id} className="border-b border-black/5 last:border-0">
              <td className="px-3 py-2 font-medium">{chore.name}</td>
              <td className="px-3 py-2">{chore.archivedOn && longDate(chore.archivedOn)}</td>
              <td className="px-3 py-2 text-right">
                <button
                  type="button"
                  disabled={restore.isPending}
                  onClick={() => restore.mutate(chore)}
                  className="rounded-lg border border-brand px-3 py-1 font-semibold text-brand hover:bg-brand/5 disabled:opacity-60"
                >
                  Restore
                </button>
              </td>
            </tr>
          ))}
      </tbody>
    </table>
  )
}

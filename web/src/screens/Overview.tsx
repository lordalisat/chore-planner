import { useSuspenseQuery } from '@tanstack/react-query'
import { choresQuery, type Chore } from '../chores.ts'
import { relativeDate, today } from '../dates.ts'

/**
 * What needs doing, most urgent first, with One-off Chores that have no Due Date under Anytime at the bottom; Dormant
 * Chores wait out of sight until their Season, and finished ones are gone.
 */
export function Overview() {
  const { data: chores } = useSuspenseQuery(choresQuery)
  const shown = chores
    .filter((chore) => !chore.dormant && !chore.finished)
    .sort((a, b) => (b.urgency ?? -1) - (a.urgency ?? -1))
  const scheduled = shown.filter((chore) => chore.dueDate)
  const anytime = shown.filter((chore) => !chore.dueDate)

  if (shown.length === 0) {
    return <p className="py-12 text-center text-ink/70">Nothing to do yet. Add a Chore to get started.</p>
  }
  const now = today()
  return (
    <div className="flex flex-col gap-6">
      {scheduled.length > 0 && <ChoreList chores={scheduled} today={now} />}
      {anytime.length > 0 && (
        <section className="flex flex-col gap-2">
          <h2 className="text-sm font-semibold uppercase tracking-wide text-ink/60">Anytime</h2>
          <ChoreList chores={anytime} today={now} />
        </section>
      )}
    </div>
  )
}

function ChoreList({ chores, today }: { chores: Chore[]; today: string }) {
  return (
    <ul className="flex flex-col gap-2">
      {chores.map((chore) => (
        <ChoreRow key={chore.id} chore={chore} today={today} />
      ))}
    </ul>
  )
}

function ChoreRow({ chore, today }: { chore: Chore; today: string }) {
  return (
    <li
      className={`flex items-baseline gap-3 rounded-lg border-l-4 bg-white px-4 py-3 shadow-sm ${
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
    </li>
  )
}

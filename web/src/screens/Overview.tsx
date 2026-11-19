import { useSuspenseQuery } from '@tanstack/react-query'
import { Link } from '@tanstack/react-router'
import { choresQuery, type Chore } from '../chores.ts'
import { relativeDate, today } from '../dates.ts'

/** What needs doing, most urgent first; Dormant Chores wait out of sight until their Season. */
export function Overview() {
  const { data: chores } = useSuspenseQuery(choresQuery)
  const shown = chores.filter((chore) => !chore.dormant).sort((a, b) => (b.urgency ?? -1) - (a.urgency ?? -1))

  if (shown.length === 0) {
    return <p className="py-12 text-center text-ink/70">Nothing to do yet. Add a Chore to get started.</p>
  }
  const now = today()
  return (
    <ul className="flex flex-col gap-2">
      {shown.map((chore) => (
        <ChoreRow key={chore.id} chore={chore} today={now} />
      ))}
    </ul>
  )
}

function ChoreRow({ chore, today }: { chore: Chore; today: string }) {
  return (
    <li>
      <Link
        to="/chores/$id"
        params={{ id: chore.id }}
        className={`flex items-baseline gap-3 rounded-lg border-l-4 bg-white px-4 py-3 shadow-sm hover:shadow-md ${
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
      </Link>
    </li>
  )
}

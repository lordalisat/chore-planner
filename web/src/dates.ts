// Dates travel as the API's "2026-11-18" text and are only ever whole days, so they are counted in UTC, where no day
// is shorter or longer than another

const dayNames = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']
const monthNames = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']
const msPerDay = 24 * 60 * 60 * 1000

const parse = (date: string) => new Date(`${date}T00:00:00Z`)

/** This device's date, as the API writes dates. */
export function today(): string {
  const now = new Date()
  return `${now.getFullYear()}-${String(now.getMonth() + 1).padStart(2, '0')}-${String(now.getDate()).padStart(2, '0')}`
}

/** A date as a list shows it: "today", "tomorrow", "in 3 days", "2 days overdue", or "Tue 24 Nov" beyond a week. */
export function relativeDate(date: string, from: string): string {
  const days = Math.round((parse(date).getTime() - parse(from).getTime()) / msPerDay)
  if (days < 0) return `${-days} ${-days === 1 ? 'day' : 'days'} overdue`
  if (days === 0) return 'today'
  if (days === 1) return 'tomorrow'
  if (days < 7) return `in ${days} days`
  const d = parse(date)
  return `${dayNames[d.getUTCDay()]} ${d.getUTCDate()} ${monthNames[d.getUTCMonth()]}`
}

/** A date in full, such as "8 Mar 2027". */
export function longDate(date: string): string {
  const d = parse(date)
  return `${d.getUTCDate()} ${monthNames[d.getUTCMonth()]} ${d.getUTCFullYear()}`
}

import { Field, FieldError, FieldLabel } from '@ui-sdk/react/field'
import { Select, SelectContent, SelectItem, SelectItemIndicator, SelectTrigger, SelectValue } from '@ui-sdk/react/select'

export type Option<T extends string> = { value: T; label: string }

/** A labelled pick from a list; `null` is nothing picked, shown as the placeholder. */
export function SelectField<T extends string>({
  label,
  hideLabel,
  options,
  value,
  onValueChange,
  placeholder,
  error,
  invalid,
  className,
}: {
  label: string
  /** For a field whose meaning shows from its neighbour, such as the unit after "Every 2". */
  hideLabel?: boolean
  options: Option<T>[]
  value: T | null
  onValueChange: (value: T | null) => void
  placeholder?: string
  error?: string
  /** Marks the field wrong when the message shows elsewhere, such as under a row of fields. */
  invalid?: boolean
  className?: string
}) {
  return (
    <Field invalid={error || invalid ? true : undefined} className={`flex flex-col gap-1 ${className ?? ''}`}>
      <FieldLabel className={hideLabel ? 'sr-only' : 'text-sm font-medium'}>{label}</FieldLabel>
      <Select<T> items={options} value={value} onValueChange={(next) => onValueChange(next)} placeholder={placeholder}>
        <SelectTrigger className="group flex cursor-pointer items-center gap-2 rounded-lg border border-black/20 bg-white px-3 py-2 text-left hover:border-black/40 focus:outline-2 focus:outline-brand data-invalid:border-red-700">
          <SelectValue className="flex-1 truncate group-data-placeholder:text-ink/50" />
          <ChevronIcon />
        </SelectTrigger>
        {/* As wide as the field it opens from, and no taller than the room below it */}
        <SelectContent
          sideOffset={4}
          className="max-h-[min(20rem,var(--available-height))] w-(--anchor-width) overflow-y-auto rounded-lg border border-black/20 bg-white p-1 shadow-lg"
        >
          {options.map((option) => (
            <SelectItem
              key={option.value}
              value={option.value}
              className="flex cursor-pointer items-center rounded-md px-3 py-2.5 outline-none data-highlighted:bg-cream data-selected:font-semibold data-selected:text-brand"
            >
              <span className="flex-1">{option.label}</span>
              <SelectItemIndicator aria-hidden="true">✓</SelectItemIndicator>
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
      {error && (
        <FieldError match className="text-sm text-red-700">
          {error}
        </FieldError>
      )}
    </Field>
  )
}

// Points down, and up while the list is open
function ChevronIcon() {
  return (
    <svg
      viewBox="0 0 24 24"
      className="size-4 shrink-0 text-ink/60 transition-transform group-data-open:rotate-180"
      fill="none"
      stroke="currentColor"
      strokeWidth="2"
      aria-hidden="true"
    >
      <path d="m6 9 6 6 6-6" strokeLinecap="round" strokeLinejoin="round" />
    </svg>
  )
}

import { Field, FieldError, FieldLabel } from '@ui-sdk/react/field'
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from '@ui-sdk/react/select'

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
        <SelectTrigger className="rounded-lg border border-black/20 bg-white px-3 py-2 text-left focus:outline-2 focus:outline-brand data-invalid:border-red-700 data-placeholder:text-ink/50">
          <SelectValue />
        </SelectTrigger>
        <SelectContent sideOffset={4} className="max-h-72 min-w-40 overflow-y-auto rounded-lg border border-black/10 bg-white p-1 shadow-lg">
          {options.map((option) => (
            <SelectItem
              key={option.value}
              value={option.value}
              className="cursor-pointer rounded-md px-3 py-2 data-highlighted:bg-cream data-selected:font-semibold"
            >
              {option.label}
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

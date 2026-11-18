import { Field, FieldError, FieldLabel } from '@ui-sdk/react/field'
import { NumberInput, NumberInputControl } from '@ui-sdk/react/number-input'

/** A labelled whole number; `null` is empty. */
export function NumberField({
  label,
  value,
  onValueChange,
  min,
  error,
  invalid,
  className,
}: {
  label: string
  value: number | null
  onValueChange: (value: number | null) => void
  min?: number
  error?: string
  /** Marks the field wrong when the message shows elsewhere, such as under a row of fields. */
  invalid?: boolean
  className?: string
}) {
  return (
    <Field invalid={error || invalid ? true : undefined} className={`flex flex-col gap-1 ${className ?? ''}`}>
      <FieldLabel className="text-sm font-medium">{label}</FieldLabel>
      <NumberInput value={value} onValueChange={(next) => onValueChange(next)} min={min} step={1}>
        <NumberInputControl className="w-full rounded-lg border border-black/20 bg-white px-3 py-2 focus:outline-2 focus:outline-brand data-invalid:border-red-700" />
      </NumberInput>
      {error && (
        <FieldError match className="text-sm text-red-700">
          {error}
        </FieldError>
      )}
    </Field>
  )
}

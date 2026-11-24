import { Field, FieldLabel } from '@ui-sdk/react/field'
import { SegmentedControl, SegmentedControlItem } from '@ui-sdk/react/segmented-control'
import type { Option } from './SelectField.tsx'

/** A labelled pick from a few options, all in view. */
export function SegmentedField<T extends string>({
  label,
  options,
  value,
  onValueChange,
}: {
  label: string
  options: Option<T>[]
  value: T
  onValueChange: (value: T) => void
}) {
  return (
    <Field className="flex flex-col gap-1">
      <FieldLabel className="text-sm font-medium">{label}</FieldLabel>
      <SegmentedControl<T>
        value={value}
        onValueChange={(next) => next !== null && onValueChange(next)}
        className="flex rounded-lg border border-black/45 bg-white p-1"
      >
        {options.map((option) => (
          <SegmentedControlItem
            key={option.value}
            value={option.value}
            className="flex-1 rounded-md px-3 py-1 data-checked:bg-brand data-checked:text-white"
          >
            {option.label}
          </SegmentedControlItem>
        ))}
      </SegmentedControl>
    </Field>
  )
}

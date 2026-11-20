import { Field, FieldError, FieldLabel } from '@ui-sdk/react/field'
import { Input } from '@ui-sdk/react/input'
import type { ComponentProps } from 'react'

type TextFieldProps = Omit<ComponentProps<typeof Input>, 'value' | 'defaultValue' | 'onValueChange'> & {
  label: string
  value: string
  onValueChange: (value: string) => void
  /** An error from the API for this field, such as a validation problem. */
  error?: string
}

/** A labelled text input that shows its own error. */
export function TextField({ label, error, className, ...input }: TextFieldProps) {
  return (
    <Field invalid={error ? true : undefined} className={`flex flex-col gap-1 ${className ?? ''}`}>
      <FieldLabel className="text-sm font-medium">{label}</FieldLabel>
      <Input
        {...input}
        // React's autoFocus renders no attribute; a dialog looks for this one to pick its first focus
        data-autofocus={input.autoFocus ? '' : undefined}
        className="rounded-lg border border-black/20 bg-white px-3 py-2 focus:outline-2 focus:outline-brand data-invalid:border-red-700"
      />
      {error && (
        <FieldError match className="text-sm text-red-700">
          {error}
        </FieldError>
      )}
    </Field>
  )
}

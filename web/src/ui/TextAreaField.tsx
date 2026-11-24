import { Field, FieldError, FieldLabel } from '@ui-sdk/react/field'
import { Textarea } from '@ui-sdk/react/textarea'
import type { ComponentProps } from 'react'

type TextAreaFieldProps = Omit<ComponentProps<typeof Textarea>, 'value' | 'defaultValue' | 'onValueChange'> & {
  label: string
  /** For a field its neighbours already explain, such as an Item's Description under its Name. */
  hideLabel?: boolean
  value: string
  onValueChange: (value: string) => void
  /** An error from the API for this field, such as a validation problem. */
  error?: string
}

/**
 * A labelled text area that grows with its text and shows its own error. Enter adds a line; Cmd+Enter on a Mac, Ctrl+Enter elsewhere,
 * submits the form, as Enter does in a one-line field.
 */
export function TextAreaField({ label, hideLabel, error, className, ...textarea }: TextAreaFieldProps) {
  return (
    <Field invalid={error ? true : undefined} className={`flex flex-col gap-1 ${className ?? ''}`}>
      <FieldLabel className={hideLabel ? 'sr-only' : 'text-sm font-medium'}>{label}</FieldLabel>
      <Textarea
        autoResize
        submitShortcut="mod+enter"
        rows={2}
        {...textarea}
        className="min-h-18 rounded-lg border border-black/20 bg-white px-3 py-2 focus:outline-2 focus:outline-brand data-invalid:border-red-700"
      />
      {error && (
        <FieldError match className="text-sm text-red-700">
          {error}
        </FieldError>
      )}
    </Field>
  )
}

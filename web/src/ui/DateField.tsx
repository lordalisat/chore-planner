import {
  CalendarDay,
  CalendarGrid,
  CalendarHeading,
  CalendarNext,
  CalendarPrevious,
  CalendarWeekday,
  CalendarWeekdays,
  CalendarWeeks,
  DatePicker,
  DatePickerClear,
  DatePickerContent,
  DatePickerInput,
  DatePickerTrigger,
} from '@ui-sdk/react/date-picker'
import { Field, FieldError, FieldLabel } from '@ui-sdk/react/field'

/** A labelled, clearable date as the API writes it ("2026-11-18"); weeks start on Monday. */
export function DateField({
  label,
  value,
  onValueChange,
  error,
}: {
  label: string
  value: string | null
  onValueChange: (value: string | null) => void
  error?: string
}) {
  return (
    <Field invalid={error ? true : undefined} className="flex flex-col gap-1">
      <FieldLabel className="text-sm font-medium">{label}</FieldLabel>
      {/* English, day first */}
      <DatePicker value={value} onValueChange={(next) => onValueChange(next as string | null)} weekStartsOn={1} locale="en-GB">
        <div className="flex items-center gap-1 rounded-lg border border-black/20 bg-white px-3 py-1 focus-within:outline-2 focus-within:outline-brand data-invalid:border-red-700">
          <DatePickerInput className="flex-1 py-1 outline-none" />
          <DatePickerClear aria-label="Clear date" className="rounded-md px-2 text-lg data-empty:hidden hover:bg-black/5">
            ×
          </DatePickerClear>
          <DatePickerTrigger className="rounded-md px-2 py-1 hover:bg-black/5">
            <CalendarIcon />
          </DatePickerTrigger>
        </div>
        <DatePickerContent sideOffset={4} className="rounded-lg border border-black/10 bg-white p-3 text-ink shadow-lg">
          <div className="mb-2 flex items-center">
            <CalendarPrevious className="rounded-md px-3 py-1 text-lg hover:bg-cream">‹</CalendarPrevious>
            <CalendarHeading className="flex-1 text-center font-semibold" />
            <CalendarNext className="rounded-md px-3 py-1 text-lg hover:bg-cream">›</CalendarNext>
          </div>
          <CalendarGrid className="border-collapse">
            <CalendarWeekdays>
              {(index) => <CalendarWeekday index={index} className="size-10 text-xs font-medium text-ink/60" />}
            </CalendarWeekdays>
            <CalendarWeeks>
              {(cell) => (
                <CalendarDay
                  value={cell.date}
                  className="size-10 cursor-pointer rounded-full text-center hover:bg-cream data-outside:text-ink/30 data-today:font-bold data-today:text-brand data-selected:bg-brand data-selected:text-white"
                />
              )}
            </CalendarWeeks>
          </CalendarGrid>
        </DatePickerContent>
      </DatePicker>
      {error && (
        <FieldError match className="text-sm text-red-700">
          {error}
        </FieldError>
      )}
    </Field>
  )
}

function CalendarIcon() {
  return (
    <svg viewBox="0 0 24 24" className="size-5" fill="none" stroke="currentColor" strokeWidth="2" aria-hidden="true">
      <rect x="3" y="5" width="18" height="16" rx="2" />
      <path d="M3 10h18M8 3v4M16 3v4" strokeLinecap="round" />
    </svg>
  )
}

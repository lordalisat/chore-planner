import { Checkbox, CheckboxIndicator } from '@ui-sdk/react/checkbox'

/** A box to tick, named by what it ticks; a 44px target around a small box. */
export function TickBox({ label, checked, onCheckedChange }: { label: string; checked: boolean; onCheckedChange: (checked: boolean) => void }) {
  return (
    <Checkbox
      aria-label={label}
      checked={checked}
      onCheckedChange={(next) => onCheckedChange(next === true)}
      className="group grid size-11 shrink-0 place-items-center"
    >
      <span className="grid size-6 place-items-center rounded-md border-2 border-brand text-white group-data-checked:bg-brand">
        <CheckboxIndicator>
          <svg viewBox="0 0 16 16" aria-hidden className="size-4">
            <path d="M3 8.5l3 3 7-7" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round" />
          </svg>
        </CheckboxIndicator>
      </span>
    </Checkbox>
  )
}

import { Field, FieldDescription, FieldLabel } from '@ui-sdk/react/field'
import { Switch, SwitchThumb } from '@ui-sdk/react/switch'

/** A labelled on/off setting, with an optional line saying what "on" means. */
export function SwitchField({
  label,
  description,
  checked,
  onCheckedChange,
}: {
  label: string
  description?: string
  checked: boolean
  onCheckedChange: (checked: boolean) => void
}) {
  return (
    <Field className="flex items-center gap-3">
      <div className="flex flex-1 flex-col">
        <FieldLabel className="text-sm font-medium">{label}</FieldLabel>
        {description && <FieldDescription className="text-sm text-ink/70">{description}</FieldDescription>}
      </div>
      <Switch
        checked={checked}
        onCheckedChange={(next) => onCheckedChange(next)}
        className="flex h-7 w-12 shrink-0 items-center rounded-full bg-black/45 p-0.5 transition-colors data-checked:bg-brand"
      >
        <SwitchThumb className="size-6 rounded-full bg-white shadow transition-[translate] data-checked:translate-x-5" />
      </Switch>
    </Field>
  )
}

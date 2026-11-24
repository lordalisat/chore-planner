import { limits } from '../chores.ts'
import { TextAreaField } from '../ui/TextAreaField.tsx'
import { TextField } from '../ui/TextField.tsx'

/** One of a Chore's Items as the form holds it; `id` is set for an Item the Chore has, so it keeps its tick. */
export type ItemDraft = { key: string; id: number | null; name: string; description: string }

/** A draft for each of a Chore's Items, or for a new, empty one. */
export const itemDraft = (item?: { id: number; name: string; description: string | null }): ItemDraft => ({
  // Only tells drafts apart in the list; crypto.randomUUID would need HTTPS, which a home network may not have
  key: Math.random().toString(36).slice(2),
  id: item?.id ?? null,
  name: item?.name ?? '',
  description: item?.description ?? '',
})

/** A Chore's Items: each with a Name and an optional Description, in an order the household sets. */
export function ItemsField({ items, onItemsChange, error }: { items: ItemDraft[]; onItemsChange: (items: ItemDraft[]) => void; error?: string }) {
  const change = (key: string, fields: Partial<ItemDraft>) => onItemsChange(items.map((item) => (item.key === key ? { ...item, ...fields } : item)))
  const move = (index: number, by: number) => {
    const moved = [...items]
    ;[moved[index], moved[index + by]] = [moved[index + by]!, moved[index]!]
    onItemsChange(moved)
  }

  return (
    <fieldset className="flex flex-col gap-2">
      <legend className="mb-1 text-sm font-medium">Items</legend>
      {items.map((item, index) => {
        const named = item.name.trim() || `item ${index + 1}`
        return (
          <div key={item.key} className="flex flex-col gap-2 rounded-lg border border-black/10 bg-white/60 p-2">
            <div className="flex items-end gap-1">
              <TextField
                label={`Item ${index + 1}`}
                value={item.name}
                onValueChange={(name) => change(item.key, { name })}
                maxLength={limits.itemName}
                className="flex-1"
              />
              <ItemButton label={`Move ${named} up`} disabled={index === 0} onClick={() => move(index, -1)}>
                ↑
              </ItemButton>
              <ItemButton label={`Move ${named} down`} disabled={index === items.length - 1} onClick={() => move(index, 1)}>
                ↓
              </ItemButton>
              <ItemButton label={`Remove ${named}`} onClick={() => onItemsChange(items.filter((other) => other.key !== item.key))}>
                ×
              </ItemButton>
            </div>
            <TextAreaField
              label={`Item ${index + 1} description`}
              hideLabel
              placeholder="Description (optional)"
              value={item.description}
              onValueChange={(description) => change(item.key, { description })}
              maxLength={limits.description}
            />
          </div>
        )
      })}
      {error && <p className="text-sm text-red-700">{error}</p>}
      {items.length < limits.items && (
        <button
          type="button"
          onClick={() => onItemsChange([...items, itemDraft()])}
          className="min-h-11 self-start rounded-lg border border-brand/30 px-4 text-sm font-medium text-brand hover:bg-brand/10"
        >
          Add item
        </button>
      )}
    </fieldset>
  )
}

function ItemButton({ label, disabled, onClick, children }: { label: string; disabled?: boolean; onClick: () => void; children: string }) {
  return (
    <button
      type="button"
      aria-label={label}
      disabled={disabled}
      onClick={onClick}
      className="grid size-11 shrink-0 place-items-center rounded-lg text-lg text-brand hover:bg-brand/10 disabled:opacity-30"
    >
      {children}
    </button>
  )
}

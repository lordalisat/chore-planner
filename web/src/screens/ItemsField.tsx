import { useEffect, useRef, useState } from 'react'
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
  // A new Item gets the cursor; an Item's Description field shows once it has text or someone asks for it
  const [added, setAdded] = useState<string | null>(null)
  const [describing, setDescribing] = useState<string[]>([])
  const errorLine = useRef<HTMLParagraphElement>(null)
  const list = useRef<HTMLFieldSetElement>(null)
  useEffect(() => {
    if (error) errorLine.current?.scrollIntoView({ block: 'nearest' })
  }, [error])
  // Once on screen: the new Item's Name, or the Description field just asked for
  const focus = (key: string, field: 'input' | 'textarea') =>
    list.current?.querySelector<HTMLElement>(`[data-item="${key}"] ${field}`)?.focus()
  useEffect(() => {
    if (added) focus(added, 'input')
  }, [added])
  const lastDescribing = describing.at(-1)
  useEffect(() => {
    if (lastDescribing) focus(lastDescribing, 'textarea')
  }, [lastDescribing])

  const change = (key: string, fields: Partial<ItemDraft>) => onItemsChange(items.map((item) => (item.key === key ? { ...item, ...fields } : item)))
  const move = (index: number, by: number) => {
    const moved = [...items]
    ;[moved[index], moved[index + by]] = [moved[index + by]!, moved[index]!]
    onItemsChange(moved)
  }
  const add = () => {
    const draft = itemDraft()
    setAdded(draft.key)
    onItemsChange([...items, draft])
  }

  return (
    <fieldset ref={list} className="flex flex-col gap-2">
      <legend className="mb-1 text-sm font-medium">Items</legend>
      {items.map((item, index) => {
        const named = item.name.trim() || `item ${index + 1}`
        return (
          <div key={item.key} data-item={item.key} className="flex flex-col gap-1 rounded-lg border border-black/10 bg-white/60 p-2">
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
              {/* Not ×, which closes the dialog the form may be in */}
              <ItemButton label={`Remove ${named}`} onClick={() => onItemsChange(items.filter((other) => other.key !== item.key))}>
                −
              </ItemButton>
            </div>
            {item.description || describing.includes(item.key) ? (
              <TextAreaField
                label={`Item ${index + 1} description`}
                hideLabel
                placeholder="Description (optional)"
                value={item.description}
                onValueChange={(description) => change(item.key, { description })}
                maxLength={limits.description}
              />
            ) : (
              <button
                type="button"
                aria-label={`Add description to ${named}`}
                onClick={() => setDescribing([...describing, item.key])}
                className="-my-1 min-h-11 self-start px-1 text-sm text-brand hover:underline"
              >
                Add description
              </button>
            )}
          </div>
        )
      })}
      {error && (
        <p ref={errorLine} className="text-sm text-red-700">
          {error}
        </p>
      )}
      {items.length < limits.items && (
        <button
          type="button"
          onClick={add}
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

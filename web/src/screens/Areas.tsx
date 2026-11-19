import { useMutation, useQueryClient, useSuspenseQuery } from '@tanstack/react-query'
import { useState, type FormEvent } from 'react'
import { api, ApiError, type FieldErrors } from '../api/client.ts'
import { areasQuery, type Area } from '../areas.ts'
import { choresQuery } from '../chores.ts'
import { ConfirmDialog } from '../ui/ConfirmDialog.tsx'
import { Dialog } from '../ui/Dialog.tsx'
import { SelectField, type Option } from '../ui/SelectField.tsx'
import { TextField } from '../ui/TextField.tsx'
import { toast } from '../ui/toast.ts'
import { unreachableMessage } from './Unreachable.tsx'

/** Every Area in one flat list, each with the Area it sits inside; tapping one edits it. */
export function Areas() {
  const { data: areas } = useSuspenseQuery(areasQuery)
  // undefined is closed, null a new Area
  const [editing, setEditing] = useState<Area | null | undefined>(undefined)
  const nameOf = (id: number | null | undefined) => areas.find((area) => area.id === id)?.name

  return (
    <section className="flex flex-col gap-4">
      <div className="flex items-center">
        <h1 className="flex-1 text-xl font-semibold text-brand">Areas</h1>
        <button
          type="button"
          onClick={() => setEditing(null)}
          className="rounded-lg bg-brand px-4 py-2 font-semibold text-white hover:bg-brand/90"
        >
          New Area
        </button>
      </div>
      {areas.length === 0 ? (
        <p className="py-12 text-center text-ink/70">No Areas yet. Add one, such as the kitchen or the garden.</p>
      ) : (
        <ul className="flex flex-col gap-2">
          {areas.map((area) => (
            <li key={area.id}>
              <button
                type="button"
                onClick={() => setEditing(area)}
                className="flex w-full items-baseline gap-3 rounded-lg bg-white px-4 py-3 text-left shadow-sm hover:shadow-md"
              >
                <span className="flex-1 font-medium">{area.name}</span>
                {area.parentId != null && <span className="text-sm text-ink/60">in {nameOf(area.parentId)}</span>}
              </button>
            </li>
          ))}
        </ul>
      )}
      <Dialog open={editing !== undefined} onOpenChange={(open) => !open && setEditing(undefined)} title={editing ? 'Edit Area' : 'New Area'}>
        {/* Mounted only while open, so each starts from the Area's own values */}
        {editing !== undefined && <AreaForm area={editing ?? undefined} areas={areas} onDone={() => setEditing(undefined)} />}
      </Dialog>
    </section>
  )
}

const top = 'top'

/** Creates an Area, or edits and deletes the given one; `onDone` closes it. */
function AreaForm({ area, areas, onDone }: { area?: Area; areas: Area[]; onDone: () => void }) {
  const queryClient = useQueryClient()
  const [name, setName] = useState(area?.name ?? '')
  const [parent, setParent] = useState<string>(area?.parentId != null ? String(area.parentId) : top)
  const [errors, setErrors] = useState<FieldErrors>({})
  const [confirming, setConfirming] = useState(false)
  // An Area can't sit inside itself; one inside it the API turns down
  const parents: Option<string>[] = [
    { value: top, label: 'Nothing, at the top' },
    ...areas.filter((other) => other.id !== area?.id).map((other) => ({ value: String(other.id), label: other.name })),
  ]

  const refetch = () => {
    void queryClient.invalidateQueries({ queryKey: areasQuery.queryKey })
    // Chores show their Area's name
    void queryClient.invalidateQueries({ queryKey: choresQuery.queryKey })
  }

  const save = useMutation({
    mutationFn: () => {
      const request = { name, parentId: parent === top ? null : Number(parent) }
      return area ? api('/api/areas/{id}', 'put', request, { id: area.id }) : api('/api/areas', 'post', request)
    },
    onSuccess: () => {
      refetch()
      onDone()
    },
    onError: (error) => {
      if (error instanceof ApiError && error.status === 400) setErrors(error.errors)
      else toast(unreachableMessage)
    },
  })

  const remove = useMutation({
    mutationFn: (id: number) => api('/api/areas/{id}', 'delete', { id }),
    onSuccess: () => {
      refetch()
      onDone()
    },
    onError: () => toast(unreachableMessage),
  })

  function submit(event: FormEvent) {
    event.preventDefault()
    setErrors({})
    save.mutate()
  }

  return (
    <form onSubmit={submit} className="flex flex-col gap-4">
      <TextField label="Name" autoFocus value={name} onValueChange={setName} error={errors.name?.[0]} />
      <SelectField label="Inside" options={parents} value={parent} onValueChange={(next) => setParent(next ?? top)} error={errors.parentId?.[0]} />
      <div className="flex flex-wrap gap-2">
        <button
          type="submit"
          disabled={save.isPending}
          className="flex-1 rounded-lg bg-brand px-4 py-2 font-semibold text-white hover:bg-brand/90 disabled:opacity-60"
        >
          Save
        </button>
        {area && (
          <button
            type="button"
            disabled={remove.isPending}
            onClick={() => setConfirming(true)}
            className="rounded-lg px-4 py-2 font-semibold text-red-700 hover:bg-red-700/5 disabled:opacity-60"
          >
            Delete
          </button>
        )}
      </div>
      {area && (
        <ConfirmDialog
          open={confirming}
          onOpenChange={setConfirming}
          title={`Delete ${area.name}?`}
          description={`${usedBy(area)} They stay, without an Area, and Areas inside it move to the top.`}
          confirmLabel="Delete"
          onConfirm={() => remove.mutate(area.id)}
        />
      )}
    </form>
  )
}

function usedBy({ name, choreCount }: Area) {
  if (choreCount === 0) return `No Chores use ${name}.`
  return choreCount === 1 ? `1 Chore uses ${name}.` : `${choreCount} Chores use ${name}.`
}

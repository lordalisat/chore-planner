import { useMutation, useQueryClient } from '@tanstack/react-query'
import { useState } from 'react'
import { api } from '../api/client.ts'
import { choreQuery, choresQuery, type Chore } from '../chores.ts'
import { ConfirmDialog } from '../ui/ConfirmDialog.tsx'
import { toast } from '../ui/toast.ts'
import { unreachableMessage } from './Unreachable.tsx'

/**
 * Deletes a Chore; `onDeleted` leaves its screen. One with Completions is Archived at once with an Undo toast; one
 * without is gone for good, so it asks first.
 */
export function DeleteChore({ chore, onDeleted }: { chore: Chore; onDeleted: () => void }) {
  const queryClient = useQueryClient()
  const [confirming, setConfirming] = useState(false)
  const refetch = () => queryClient.invalidateQueries({ queryKey: choresQuery.queryKey })

  const remove = useMutation({
    mutationFn: () => api('/api/chores/{id}', 'delete', { id: chore.id }),
    onSuccess: ({ archived }) => {
      void refetch()
      onDeleted()
      // Refetching would only find it gone, while reopening it should say so rather than show it from the cache
      queryClient.removeQueries({ queryKey: choreQuery(chore.id).queryKey, exact: true })
      // Outlives this component, which onDeleted unmounts
      toast({
        title: `${archived ? 'Archived' : 'Deleted'} ${chore.name}`,
        ...(archived && {
          action: {
            label: 'Undo',
            onAction: () =>
              api('/api/chores/{id}/restore', 'post', { id: chore.id }).then(refetch, () => toast(unreachableMessage)),
          },
        }),
      })
    },
    onError: () => toast(unreachableMessage),
  })

  return (
    <>
      <button
        type="button"
        disabled={remove.isPending}
        onClick={() => (chore.completionCount > 0 ? remove.mutate() : setConfirming(true))}
        className="rounded-lg px-4 py-2 font-semibold text-red-700 hover:bg-red-700/5 disabled:opacity-60"
      >
        Delete
      </button>
      <ConfirmDialog
        open={confirming}
        onOpenChange={setConfirming}
        title={`Delete ${chore.name}?`}
        description="It has never been done, so it is removed for good and can't be brought back."
        confirmLabel="Delete for good"
        onConfirm={() => remove.mutate()}
      />
    </>
  )
}

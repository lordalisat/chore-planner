import {
  AlertDialog,
  AlertDialogBackdrop,
  AlertDialogClose,
  AlertDialogContent,
  AlertDialogDescription,
  AlertDialogTitle,
} from '@ui-sdk/react/alert-dialog'

/**
 * Asks before something that can't be undone; `onConfirm` runs when the person agrees, then it closes. `consequence` says
 * what agreeing does, for an agent to ask the person before it agrees for them.
 */
export function ConfirmDialog({
  open,
  onOpenChange,
  title,
  description,
  confirmLabel,
  consequence,
  onConfirm,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  description: string
  confirmLabel: string
  consequence: string
  onConfirm: () => void
}) {
  return (
    <AlertDialog open={open} onOpenChange={(next) => onOpenChange(next)}>
      <AlertDialogBackdrop className="fixed inset-0 bg-black/40" />
      <AlertDialogContent className="fixed inset-x-4 top-1/2 -translate-y-1/2 rounded-2xl bg-cream p-6 shadow-xl md:left-1/2 md:w-full md:max-w-sm md:-translate-x-1/2">
        <AlertDialogTitle className="text-lg font-semibold text-brand">{title}</AlertDialogTitle>
        <AlertDialogDescription className="mt-2 text-ink/80">{description}</AlertDialogDescription>
        <div className="mt-6 flex justify-end gap-2">
          <AlertDialogClose className="rounded-lg px-4 py-2 font-semibold hover:bg-black/5">Cancel</AlertDialogClose>
          <AlertDialogClose
            value="confirm"
            data-agent-confirm={consequence}
            action={onConfirm}
            className="rounded-lg bg-red-700 px-4 py-2 font-semibold text-white hover:bg-red-800"
          >
            {confirmLabel}
          </AlertDialogClose>
        </div>
      </AlertDialogContent>
    </AlertDialog>
  )
}

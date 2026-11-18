import { Dialog as SdkDialog, DialogBackdrop, DialogClose, DialogContent, DialogTitle } from '@ui-sdk/react/dialog'
import type { ReactNode } from 'react'

/** A modal with a title and a close button; focus starts on its first field. */
export function Dialog({
  open,
  onOpenChange,
  title,
  children,
}: {
  open: boolean
  onOpenChange: (open: boolean) => void
  title: string
  children: ReactNode
}) {
  return (
    <SdkDialog open={open} onOpenChange={(next) => onOpenChange(next)}>
      <DialogBackdrop className="fixed inset-0 bg-black/40" />
      <DialogContent className="fixed inset-x-0 bottom-0 max-h-[90dvh] overflow-y-auto rounded-t-2xl bg-cream p-4 shadow-xl md:inset-x-auto md:top-1/2 md:bottom-auto md:left-1/2 md:w-full md:max-w-lg md:-translate-x-1/2 md:-translate-y-1/2 md:rounded-2xl md:p-6">
        <div className="mb-4 flex items-center">
          <DialogTitle className="flex-1 text-xl font-semibold text-brand">{title}</DialogTitle>
          <DialogClose aria-label="Close" className="rounded-lg px-3 py-1 text-2xl leading-none hover:bg-black/5">
            ×
          </DialogClose>
        </div>
        {children}
      </DialogContent>
    </SdkDialog>
  )
}

import type { ToastType } from '@ui-sdk/react/toast'
import { Toast, ToastAction, ToastClose, ToastDescription, ToastRegion, ToastTitle } from '@ui-sdk/react/toast'

// What each kind of toast looks like, so what happened shows at a glance and not by colour alone
const looks: Partial<Record<ToastType, { icon: string; className: string }>> = {
  success: { icon: '✓', className: 'bg-brand' },
  info: { icon: '→', className: 'bg-sky-800' },
  warning: { icon: '!', className: 'bg-amber-700' },
  error: { icon: '✕', className: 'bg-red-700' },
}

/** Where toasts show, once near the app's root: one above the other, each closable. */
export function Toasts() {
  return (
    // The UI SDK centres the region itself (left 50%, translated back by half its width); setting left or right breaks that.
    // Always expanded, so toasts list one above the other instead of collapsing into a stack that grows on hover.
    <ToastRegion
      side="bottom"
      align="center"
      expanded
      // The toasts sit in the SDK's own list inside the region, so the gap between them goes on that list
      className="bottom-24 w-[calc(100vw-2rem)] max-w-sm md:bottom-6 [&_[data-slot=toast-list]]:flex [&_[data-slot=toast-list]]:flex-col [&_[data-slot=toast-list]]:gap-2"
    >
      {(toast) => {
        const look = looks[toast.type] ?? { icon: '', className: 'bg-ink' }
        return (
          <Toast className={`flex w-full max-w-sm items-center gap-3 rounded-lg px-4 py-3 text-white shadow-lg ${look.className}`}>
            {look.icon && (
              <span aria-hidden className="grid size-6 shrink-0 place-items-center rounded-full bg-white/20 text-sm font-bold">
                {look.icon}
              </span>
            )}
            <div className="min-w-0 flex-1">
              <ToastTitle className="font-medium" />
              <ToastDescription className="text-sm text-white/80" />
            </div>
            <ToastAction className="rounded-md px-2 py-1 font-semibold text-cream hover:bg-white/10" />
            <ToastClose aria-label="Close" className="-mr-1 rounded-md px-1.5 py-0.5 text-lg leading-none text-white/70 hover:bg-white/10 hover:text-white">
              ×
            </ToastClose>
          </Toast>
        )
      }}
    </ToastRegion>
  )
}

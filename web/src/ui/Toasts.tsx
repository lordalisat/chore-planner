import { Toast, ToastAction, ToastDescription, ToastRegion, ToastTitle } from '@ui-sdk/react/toast'

/** Where toasts show, once near the app's root. */
export function Toasts() {
  return (
    // The UI SDK centres the region itself (left 50%, translated back by half its width); setting left or right breaks that
    <ToastRegion side="bottom" align="center" className="bottom-24 flex w-[calc(100vw-2rem)] max-w-sm flex-col items-center gap-2 md:bottom-6">
      {() => (
        <Toast className="flex w-full max-w-sm items-center gap-3 rounded-lg bg-ink px-4 py-3 text-white shadow-lg">
          <div className="flex-1">
            <ToastTitle className="font-medium" />
            <ToastDescription className="text-sm text-white/80" />
          </div>
          <ToastAction className="rounded-md px-2 py-1 font-semibold text-cream hover:bg-white/10" />
        </Toast>
      )}
    </ToastRegion>
  )
}

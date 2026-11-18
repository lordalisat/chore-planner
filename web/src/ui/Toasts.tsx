import { Toast, ToastAction, ToastDescription, ToastRegion, ToastTitle } from '@ui-sdk/react/toast'

/** Where toasts show, once near the app's root. */
export function Toasts() {
  return (
    <ToastRegion side="bottom" align="center" className="fixed inset-x-4 bottom-24 z-50 flex flex-col items-center gap-2 md:bottom-6">
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

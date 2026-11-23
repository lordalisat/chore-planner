import { createToaster } from '@ui-sdk/react/toast'

/**
 * Shows a toast in the app's <Toasts /> from anywhere, like toast("Saved") or toast("Done", { action }). Each closes
 * itself after 15 seconds, waiting while the pointer or focus is on it or the window is in the background.
 */
export const toast = createToaster({ duration: 15_000 })

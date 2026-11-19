import { useSyncExternalStore } from 'react'

/** Tailwind's `lg` breakpoint, for behaviour that differs by width rather than only layout. */
export const fromLg = '(min-width: 64rem)'

/** Whether the screen matches a media query, following it as the window resizes. */
export function useMediaQuery(query: string) {
  return useSyncExternalStore(
    (onChange) => {
      const list = matchMedia(query)
      list.addEventListener('change', onChange)
      return () => list.removeEventListener('change', onChange)
    },
    () => matchMedia(query).matches,
  )
}

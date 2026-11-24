import { Button } from '@ui-sdk/react/button'

/**
 * A table row's name, as the button that opens it from the keyboard or for an agent; it reads as plain text, since a
 * click anywhere on the row opens it too.
 */
export function RowButton({ onClick, children }: { onClick: () => void; children: string }) {
  return (
    <Button
      onClick={(event) => {
        // The row opens it on a click as well; once is enough
        event.stopPropagation()
        onClick()
      }}
      className="text-left font-medium focus-visible:outline-2 focus-visible:outline-brand"
    >
      {children}
    </Button>
  )
}

import { Menu as SdkMenu, MenuContent, MenuItem, MenuTrigger } from '@ui-sdk/react/menu'
import type { ReactNode } from 'react'

export type MenuEntry = { label: string; onSelect: () => void }

/** A button opening a list of actions. */
export function Menu({ trigger, label, entries }: { trigger: ReactNode; label: string; entries: MenuEntry[] }) {
  return (
    <SdkMenu>
      <MenuTrigger aria-label={label} className="rounded-lg px-3 py-2 hover:bg-white/10">
        {trigger}
      </MenuTrigger>
      <MenuContent
        align="end"
        sideOffset={4}
        className="min-w-40 rounded-lg border border-black/10 bg-white p-1 text-ink shadow-lg"
      >
        {entries.map((entry) => (
          <MenuItem
            key={entry.label}
            onSelect={entry.onSelect}
            className="cursor-pointer rounded-md px-3 py-2 data-highlighted:bg-cream"
          >
            {entry.label}
          </MenuItem>
        ))}
      </MenuContent>
    </SdkMenu>
  )
}

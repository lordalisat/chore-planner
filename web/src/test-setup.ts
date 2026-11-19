import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'

// The UI SDK's popups (dialog, select, date picker) use the popover API, which jsdom lacks
HTMLElement.prototype.showPopover ??= function () {}
HTMLElement.prototype.hidePopover ??= function () {}
Element.prototype.getAnimations ??= () => []
// Toasts measure themselves with ResizeObserver, which jsdom lacks too
globalThis.ResizeObserver ??= class {
  observe() {}
  unobserve() {}
  disconnect() {}
}
document.head.append(Object.assign(document.createElement('style'), { textContent: '[popover] { display: block !important }' }))

afterEach(() => {
  cleanup()
  vi.unstubAllGlobals()
})

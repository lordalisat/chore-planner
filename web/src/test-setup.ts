import '@testing-library/jest-dom/vitest'
import { cleanup } from '@testing-library/react'
import { afterEach, vi } from 'vitest'
import { toast } from './ui/toast.ts'

declare global {
  var __UI_SDK_STRICT__: boolean | undefined
}
// The UI SDK's development warnings fail the test that caused them
globalThis.__UI_SDK_STRICT__ = true

// What the UI SDK needs that jsdom lacks, or has without the parts that matter, so assigned rather than kept
HTMLElement.prototype.showPopover = function () {}
HTMLElement.prototype.hidePopover = function () {}
Element.prototype.getAnimations = () => []
Element.prototype.scrollIntoView = function () {}
Element.prototype.checkVisibility = () => true
Element.prototype.getClientRects = () => [] as unknown as DOMRectList
document.elementFromPoint = () => null
document.elementsFromPoint = () => []
// Toasts measure themselves to stack, with an observer jsdom lacks
globalThis.ResizeObserver ??= class {
  observe() {}
  unobserve() {}
  disconnect() {}
}
// jsdom has no media queries; a test is narrower than every breakpoint unless it stubs matchMedia itself
window.matchMedia ??= (query: string) =>
  ({ matches: false, media: query, addEventListener: () => {}, removeEventListener: () => {} }) as unknown as MediaQueryList
document.head.append(Object.assign(document.createElement('style'), { textContent: '[popover] { display: block !important }' }))

afterEach(() => {
  cleanup()
  // Toasts live outside React, so one test's would linger into the next
  toast.dismiss()
  vi.unstubAllGlobals()
})

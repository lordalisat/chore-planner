/** Scrolls the nearest thing that scrolls around `element` (a dialog, a side panel, or else the page) to its top. */
export function scrollToTop(element: HTMLElement) {
  for (let parent = element.parentElement; parent; parent = parent.parentElement) {
    if (/auto|scroll/.test(getComputedStyle(parent).overflowY)) {
      parent.scrollTop = 0
      return
    }
  }
  if (document.scrollingElement) document.scrollingElement.scrollTop = 0
}

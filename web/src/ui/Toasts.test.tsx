import { act, render, screen } from '@testing-library/react'
import { afterEach, expect, test, vi } from 'vitest'
import { toast } from './toast.ts'
import { Toasts } from './Toasts.tsx'

afterEach(() => {
  vi.useRealTimers()
})

test('a toast closes itself after 15 seconds', () => {
  vi.useFakeTimers()
  render(<Toasts />)

  act(() => void toast.success('Added Laundry'))
  act(() => vi.advanceTimersByTime(10_000))
  expect(screen.getByRole('listitem', { name: /Added Laundry/ })).toBeInTheDocument()

  act(() => vi.advanceTimersByTime(6_000))
  expect(screen.queryByRole('listitem', { name: /Added Laundry/ })).not.toBeInTheDocument()
})

import { render, screen } from '@testing-library/react'
import { expect, test, vi } from 'vitest'
import { connectBrowserAgent } from './agent.ts'
import { ConfirmDialog } from './ConfirmDialog.tsx'

// WebMCP as a browser would offer it; the bridge registers its tools here
const tools = new Map<string, { execute: (input: object) => Promise<unknown> }>()
Object.assign(document, {
  modelContext: {
    registerTool: async (tool: { name: string; execute: (input: object) => Promise<unknown> }) => {
      tools.set(tool.name, tool)
    },
  },
})
connectBrowserAgent()

test("a browser's assistant gets the app's tools, and deletes for good only once the person agrees", async () => {
  expect([...tools.keys()].sort()).toEqual(['act', 'query', 'snapshot'])
  const onConfirm = vi.fn()
  render(
    <ConfirmDialog
      open
      onOpenChange={() => {}}
      title="Delete Laundry?"
      description="It is removed for good."
      confirmLabel="Delete for good"
      consequence="Deletes Laundry for good"
      onConfirm={onConfirm}
    />,
  )
  await screen.findByRole('alertdialog')
  const press = { steps: [{ ref: 'Delete Laundry?', action: 'press', args: ['Delete for good'] }] }
  const ask = vi.spyOn(window, 'confirm')

  ask.mockReturnValueOnce(false)
  await tools.get('act')!.execute(press)
  expect(ask).toHaveBeenCalledWith('An assistant wants to do this: Deletes Laundry for good. Allow it?')
  expect(onConfirm).not.toHaveBeenCalled()

  ask.mockReturnValueOnce(true)
  await tools.get('act')!.execute(press)
  expect(onConfirm).toHaveBeenCalledOnce()
})

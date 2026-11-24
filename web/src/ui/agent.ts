import { createAgent, registerTools } from '@ui-sdk/react/agent'

// For tests that drive the app as an agent would
export { createAgent }

/**
 * Lets the assistant built into a browser (WebMCP) read and use the app's controls. Anything marked
 * `data-agent-confirm`, such as deleting for good, waits for the person to agree. Does nothing in a browser without
 * WebMCP, so no one else pays for the bridge.
 */
export function connectBrowserAgent() {
  if (!('modelContext' in document)) return
  const agent = createAgent({
    allow: (step) => !step.confirm || window.confirm(`An assistant wants to do this: ${step.confirm}. Allow it?`),
  })
  // The bridge above already asks the person, once; asking here too would ask twice
  registerTools(agent, { allow: () => true })
}

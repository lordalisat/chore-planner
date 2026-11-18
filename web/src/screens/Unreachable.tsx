export const unreachableMessage = "The server can't be reached right now. Try again later."

export function Unreachable() {
  return (
    <main className="mx-auto max-w-sm px-4 py-12">
      <h1 className="text-2xl font-semibold text-brand">Chores</h1>
      <p role="alert" className="mt-4">
        {unreachableMessage}
      </p>
    </main>
  )
}

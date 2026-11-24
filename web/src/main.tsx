import { QueryClient } from '@tanstack/react-query'
import { StrictMode } from 'react'
import { createRoot } from 'react-dom/client'
import './index.css'
import App from './App.tsx'
import { createAppRouter } from './router.tsx'
import { connectBrowserAgent } from './ui/agent.ts'

// Refetches when the app regains focus, which stands in for live updates in v1
const queryClient = new QueryClient()
const router = createAppRouter(queryClient)
connectBrowserAgent()

createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <App queryClient={queryClient} router={router} />
  </StrictMode>,
)

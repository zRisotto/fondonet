import { useEffect, useState } from 'react'
import { UserCheck } from 'lucide-react'
import { AppShell } from './components/AppShell'
import { AdminDashboard } from './pages/AdminDashboard'
import { LoginPage } from './pages/LoginPage'
import { clearSession, getSession, saveSession } from './services/api'
import type { LoginResponse } from './types'

export default function App() {
  const [session, setSession] = useState<LoginResponse | null>(() => getSession())

  useEffect(() => {
    function handleUnauthorized() {
      setSession(null)
    }
    window.addEventListener('fondonet:unauthorized', handleUnauthorized)
    return () => window.removeEventListener('fondonet:unauthorized', handleUnauthorized)
  }, [])

  useEffect(() => {
    if (!session) {
      if (window.location.hash !== '#/login') {
        window.history.replaceState(null, '', '#/login')
      }
      return
    }

    const expectedHash = session.rol === 'Admin' ? '#/admin' : '#/ahorrador'
    if (!window.location.hash.startsWith(expectedHash)) {
      window.history.replaceState(null, '', expectedHash)
    }
  }, [session])

  function handleLoginSuccess(newSession: LoginResponse) {
    saveSession(newSession)
    setSession(newSession)
  }

  function handleLogout() {
    clearSession()
    setSession(null)
  }

  if (!session) {
    return <LoginPage onLoginSuccess={handleLoginSuccess} />
  }

  if (session.rol === 'Admin') {
    return <AdminDashboard session={session} onLogout={handleLogout} />
  }

  return (
    <AppShell session={session} onLogout={handleLogout}>
      <div className="bg-white border border-slate-200 shadow-sm rounded-md p-6">
        <div className="flex items-center gap-2 text-slate-900 font-semibold text-sm mb-1">
          <UserCheck className="w-4 h-4 text-slate-700" strokeWidth={1.75} />
          <span>Portal del Ahorrador</span>
        </div>
        <p className="text-xs text-slate-600">
          Sesión activa como <strong>{session.usuario}</strong> (ID ahorrador:{' '}
          {session.ahorradorId ?? '—'}). El estado de cuenta y simulador de crédito se
          implementan en la tanda 3.
        </p>
      </div>
    </AppShell>
  )
}

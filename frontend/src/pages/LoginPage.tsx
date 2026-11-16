import { useState, type FormEvent } from 'react'
import { AlertCircle, Building2, KeyRound, Loader2, LogIn } from 'lucide-react'
import { api } from '../services/api'
import type { LoginResponse } from '../types'

interface LoginPageProps {
  onLoginSuccess: (session: LoginResponse) => void
}

const DEMO_ACCOUNTS = [
  { label: 'Admin', usuario: 'admin', password: 'Admin123!' },
  { label: 'Ana (Caja 1)', usuario: 'ana', password: 'Ahorrador123!' },
  { label: 'Carlos (Caja 1)', usuario: 'carlos', password: 'Ahorrador123!' },
  { label: 'Lucía (Caja 1)', usuario: 'lucia', password: 'Ahorrador123!' },
  { label: 'Jorge (Caja 2)', usuario: 'jorge', password: 'Ahorrador123!' },
]

export function LoginPage({ onLoginSuccess }: LoginPageProps) {
  const [usuario, setUsuario] = useState('')
  const [password, setPassword] = useState('')
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)

  async function handleSubmit(e: FormEvent<HTMLFormElement>) {
    e.preventDefault()
    if (!usuario.trim() || !password) {
      setError('Ingrese su usuario y contraseña.')
      return
    }

    setIsLoading(true)
    setError(null)
    try {
      const response = await api.login({ usuario: usuario.trim(), password })
      onLoginSuccess(response)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al iniciar sesión.')
    } finally {
      setIsLoading(false)
    }
  }

  function fillDemo(demoUsuario: string, demoPassword: string) {
    setUsuario(demoUsuario)
    setPassword(demoPassword)
    setError(null)
  }

  return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center p-4">
      <div className="w-full max-w-md bg-white border border-slate-200 shadow-sm rounded-md p-6">
        <div className="flex items-center gap-3 mb-6">
          <div className="w-9 h-9 rounded-md bg-slate-900 text-white flex items-center justify-center">
            <Building2 className="w-5 h-5" strokeWidth={1.75} />
          </div>
          <div>
            <h1 className="text-base font-semibold text-slate-900">FondoNet</h1>
            <p className="text-xs text-slate-500">
              Sistema de Caja de Ahorro y Préstamos Multiempresa
            </p>
          </div>
        </div>

        <form onSubmit={handleSubmit} className="space-y-4" noValidate>
          <div>
            <label
              htmlFor="usuario"
              className="block text-xs font-medium text-slate-700 mb-1"
            >
              Usuario
            </label>
            <input
              id="usuario"
              type="text"
              autoComplete="username"
              value={usuario}
              onChange={(e) => setUsuario(e.target.value)}
              disabled={isLoading}
              placeholder="Ej. admin o ana"
              className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-md shadow-sm focus:outline-none focus:border-slate-400 disabled:bg-slate-50"
            />
          </div>

          <div>
            <label
              htmlFor="password"
              className="block text-xs font-medium text-slate-700 mb-1"
            >
              Contraseña
            </label>
            <input
              id="password"
              type="password"
              autoComplete="current-password"
              value={password}
              onChange={(e) => setPassword(e.target.value)}
              disabled={isLoading}
              placeholder="••••••••"
              className="w-full px-3 py-2 text-sm bg-white border border-slate-200 rounded-md shadow-sm focus:outline-none focus:border-slate-400 disabled:bg-slate-50"
            />
          </div>

          {error && (
            <div className="flex items-start gap-2 p-3 text-xs text-red-700 bg-red-50 border border-red-200 rounded-md">
              <AlertCircle className="w-4 h-4 shrink-0 mt-0.5" strokeWidth={1.75} />
              <span>{error}</span>
            </div>
          )}

          <button
            type="submit"
            disabled={isLoading}
            className="w-full inline-flex items-center justify-center gap-2 px-4 py-2 text-sm font-medium text-white bg-slate-900 rounded-md shadow-sm hover:bg-slate-800 disabled:opacity-60 transition-colors"
          >
            {isLoading ? (
              <>
                <Loader2 className="w-4 h-4 animate-spin" strokeWidth={1.75} />
                Verificando...
              </>
            ) : (
              <>
                <LogIn className="w-4 h-4" strokeWidth={1.75} />
                Iniciar sesión
              </>
            )}
          </button>
        </form>

        {import.meta.env.DEV && (
          <div className="mt-6 pt-4 border-t border-slate-200">
            <div className="flex items-center gap-1.5 text-xs font-medium text-slate-500 mb-2">
              <KeyRound className="w-3.5 h-3.5" strokeWidth={1.75} />
              <span>Acceso rápido (modo desarrollo)</span>
            </div>
            <div className="flex flex-wrap gap-1.5">
              {DEMO_ACCOUNTS.map((acc) => (
                <button
                  key={acc.usuario}
                  type="button"
                  onClick={() => fillDemo(acc.usuario, acc.password)}
                  className="px-2 py-1 text-xs text-slate-700 bg-slate-50 border border-slate-200 rounded-md hover:bg-slate-100 transition-colors"
                >
                  {acc.label}
                </button>
              ))}
            </div>
          </div>
        )}
      </div>
    </div>
  )
}

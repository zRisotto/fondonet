import type { ReactNode } from 'react'
import { Building2, LogOut, ShieldCheck, User } from 'lucide-react'
import type { LoginResponse } from '../types'

export interface NavTab<T extends string = string> {
  id: T
  label: string
  icon: ReactNode
}

interface AppShellProps<T extends string = string> {
  session: LoginResponse
  tabs?: NavTab<T>[]
  activeTab?: T
  onTabChange?: (tab: T) => void
  onLogout: () => void
  children: ReactNode
}

export function AppShell<T extends string = string>({
  session,
  tabs,
  activeTab,
  onTabChange,
  onLogout,
  children,
}: AppShellProps<T>) {
  return (
    <div className="min-h-screen bg-slate-50 text-slate-900 flex flex-col">
      <header className="bg-white border-b border-slate-200 sticky top-0 z-20">
        <div className="max-w-7xl mx-auto px-4 sm:px-6">
          <div className="h-14 flex items-center justify-between gap-4">
            <div className="flex items-center gap-3">
              <div className="w-8 h-8 rounded-md bg-slate-900 text-white flex items-center justify-center">
                <Building2 className="w-4 h-4" strokeWidth={1.75} />
              </div>
              <div>
                <div className="flex items-center gap-2">
                  <span className="text-sm font-semibold tracking-tight text-slate-900">
                    FondoNet
                  </span>
                  <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-xs font-medium bg-slate-100 text-slate-700 border border-slate-200">
                    {session.rol === 'Admin' ? (
                      <ShieldCheck className="w-3.5 h-3.5 text-slate-600" strokeWidth={1.75} />
                    ) : (
                      <User className="w-3.5 h-3.5 text-slate-600" strokeWidth={1.75} />
                    )}
                    {session.rol}
                  </span>
                </div>
                <p className="text-xs text-slate-500">Caja de Ahorro Multiempresa</p>
              </div>
            </div>

            <div className="flex items-center gap-3">
              <span className="text-xs text-slate-600 hidden sm:inline">
                Usuario: <strong className="font-medium text-slate-900">{session.usuario}</strong>
              </span>
              <button
                type="button"
                onClick={onLogout}
                className="inline-flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium text-slate-700 bg-white border border-slate-200 rounded-md shadow-sm hover:bg-slate-50 transition-colors"
              >
                <LogOut className="w-3.5 h-3.5" strokeWidth={1.75} />
                Salir
              </button>
            </div>
          </div>

          {tabs && tabs.length > 0 && onTabChange && (
            <nav className="flex items-center gap-1 -mb-px overflow-x-auto">
              {tabs.map((tab) => {
                const isActive = tab.id === activeTab
                return (
                  <button
                    key={tab.id}
                    type="button"
                    onClick={() => onTabChange(tab.id)}
                    className={`inline-flex items-center gap-2 px-3 py-2.5 text-xs font-medium border-b-2 transition-colors whitespace-nowrap ${
                      isActive
                        ? 'border-slate-900 text-slate-900'
                        : 'border-transparent text-slate-500 hover:text-slate-800 hover:border-slate-300'
                    }`}
                  >
                    {tab.icon}
                    {tab.label}
                  </button>
                )
              })}
            </nav>
          )}
        </div>
      </header>

      <main className="flex-1 max-w-7xl w-full mx-auto px-4 sm:px-6 py-6">{children}</main>
    </div>
  )
}

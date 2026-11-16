import { useState } from 'react'
import { CreditCard, FileSpreadsheet, TrendingUp } from 'lucide-react'
import { AppShell, type NavTab } from '../components/AppShell'
import { AdminPrestamosTab } from '../components/AdminPrestamosTab'
import type { LoginResponse } from '../types'

type AdminTabId = 'prestamos' | 'nomina' | 'rendimientos'

interface AdminDashboardProps {
  session: LoginResponse
  onLogout: () => void
}

const ADMIN_TABS: NavTab<AdminTabId>[] = [
  {
    id: 'prestamos',
    label: 'Préstamos',
    icon: <CreditCard className="w-4 h-4" strokeWidth={1.75} />,
  },
  {
    id: 'nomina',
    label: 'Incidencias y Nómina',
    icon: <FileSpreadsheet className="w-4 h-4" strokeWidth={1.75} />,
  },
  {
    id: 'rendimientos',
    label: 'Rendimientos y SPEI',
    icon: <TrendingUp className="w-4 h-4" strokeWidth={1.75} />,
  },
]

export function AdminDashboard({ session, onLogout }: AdminDashboardProps) {
  const [activeTab, setActiveTab] = useState<AdminTabId>('prestamos')

  return (
    <AppShell
      session={session}
      tabs={ADMIN_TABS}
      activeTab={activeTab}
      onTabChange={setActiveTab}
      onLogout={onLogout}
    >
      {activeTab === 'prestamos' && <AdminPrestamosTab />}
      {activeTab === 'nomina' && (
        <div className="bg-white border border-slate-200 shadow-sm rounded-md p-6 text-xs text-slate-600">
          Módulo de Nómina y Layouts de Descuento (se habilita en la pasada 2).
        </div>
      )}
      {activeTab === 'rendimientos' && (
        <div className="bg-white border border-slate-200 shadow-sm rounded-md p-6 text-xs text-slate-600">
          Módulo de Cierre Anual, Rendimientos y Dispersión SPEI (se habilita en la pasada 2).
        </div>
      )}
    </AppShell>
  )
}

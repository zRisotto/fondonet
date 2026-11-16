import { Building2 } from 'lucide-react'

export default function App() {
  return (
    <div className="min-h-screen bg-slate-50 flex items-center justify-center p-6">
      <div className="bg-white border border-slate-200 shadow-sm rounded-md p-6 max-w-md w-full">
        <div className="flex items-center gap-3 mb-2">
          <Building2 className="w-5 h-5 text-slate-700" strokeWidth={1.75} />
          <h1 className="text-lg font-semibold text-slate-900">FondoNet</h1>
        </div>
        <p className="text-sm text-slate-600">
          Infraestructura Base (Sprint 1) activa y operativa.
        </p>
      </div>
    </div>
  )
}

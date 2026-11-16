import { useEffect, useState } from 'react'
import {
  AlertCircle,
  CheckCircle2,
  Clock,
  Eye,
  FileSpreadsheet,
  Filter,
  Loader2,
  RefreshCw,
  ShieldAlert,
  X,
} from 'lucide-react'
import { api, formatCurrency, formatPercent } from '../services/api'
import type {
  CajaAhorroResumenDto,
  PrestamoDetalleDto,
  PrestamoDto,
} from '../types'

type EstatusFiltro = 'TODOS' | 'ACTIVO' | 'LIQUIDADO'

export function AdminPrestamosTab() {
  const [cajas, setCajas] = useState<CajaAhorroResumenDto[]>([])
  const [cajaFiltro, setCajaFiltro] = useState<string>('')
  const [estatusFiltro, setEstatusFiltro] = useState<EstatusFiltro>('TODOS')
  const [prestamos, setPrestamos] = useState<PrestamoDto[]>([])
  const [isLoading, setIsLoading] = useState<boolean>(true)
  const [error, setError] = useState<string | null>(null)

  const [detalle, setDetalle] = useState<PrestamoDetalleDto | null>(null)
  const [isLoadingDetalle, setIsLoadingDetalle] = useState<boolean>(false)
  const [errorDetalle, setErrorDetalle] = useState<string | null>(null)

  async function cargarDatos(cajaIdStr: string) {
    setIsLoading(true)
    setError(null)
    try {
      const cajaAhorroId = cajaIdStr ? Number(cajaIdStr) : undefined
      const [cajasRes, prestamosRes] = await Promise.all([
        cajas.length === 0 ? api.listarCajas() : Promise.resolve(cajas),
        api.listarPrestamos({ cajaAhorroId }),
      ])
      if (cajas.length === 0) {
        setCajas(cajasRes)
      }
      setPrestamos(prestamosRes)
    } catch (err) {
      setError(err instanceof Error ? err.message : 'Error al cargar préstamos.')
    } finally {
      setIsLoading(false)
    }
  }

  useEffect(() => {
    void cargarDatos(cajaFiltro)
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [cajaFiltro])

  async function abrirDetalle(prestamoId: number) {
    setIsLoadingDetalle(true)
    setErrorDetalle(null)
    setDetalle(null)
    try {
      const data = await api.obtenerPrestamo(prestamoId)
      setDetalle(data)
    } catch (err) {
      setErrorDetalle(
        err instanceof Error ? err.message : 'No fue posible obtener las amortizaciones.',
      )
    } finally {
      setIsLoadingDetalle(false)
    }
  }

  function cerrarDetalle() {
    setDetalle(null)
    setErrorDetalle(null)
  }

  const prestamosFiltrados = prestamos.filter((p) =>
    estatusFiltro === 'TODOS' ? true : p.estatus === estatusFiltro,
  )

  return (
    <div className="space-y-4">
      <div className="bg-white border border-slate-200 shadow-sm rounded-md p-4 flex flex-col sm:flex-row sm:items-center sm:justify-between gap-3">
        <div>
          <h2 className="text-sm font-semibold text-slate-900">Cartera de Préstamos</h2>
          <p className="text-xs text-slate-500">
            Consulta global de créditos otorgados, avales y tablas de amortización quincenal.
          </p>
        </div>

        <div className="flex flex-wrap items-center gap-2">
          <div className="flex items-center gap-1.5">
            <Filter className="w-3.5 h-3.5 text-slate-400" strokeWidth={1.75} />
            <select
              aria-label="Filtrar por caja de ahorro"
              value={cajaFiltro}
              onChange={(e) => setCajaFiltro(e.target.value)}
              className="px-2.5 py-1.5 text-xs bg-white border border-slate-200 rounded-md shadow-sm text-slate-700 focus:outline-none focus:border-slate-400"
            >
              <option value="">Todas las cajas</option>
              {cajas.map((c) => (
                <option key={c.id} value={c.id}>
                  {c.nombre}
                </option>
              ))}
            </select>
          </div>

          <select
            aria-label="Filtrar por estatus"
            value={estatusFiltro}
            onChange={(e) => setEstatusFiltro(e.target.value as EstatusFiltro)}
            className="px-2.5 py-1.5 text-xs bg-white border border-slate-200 rounded-md shadow-sm text-slate-700 focus:outline-none focus:border-slate-400"
          >
            <option value="TODOS">Todos los estatus</option>
            <option value="ACTIVO">Activos</option>
            <option value="LIQUIDADO">Liquidados</option>
          </select>

          <button
            type="button"
            onClick={() => void cargarDatos(cajaFiltro)}
            disabled={isLoading}
            className="inline-flex items-center gap-1.5 px-2.5 py-1.5 text-xs font-medium text-slate-700 bg-white border border-slate-200 rounded-md shadow-sm hover:bg-slate-50 disabled:opacity-50"
          >
            <RefreshCw
              className={`w-3.5 h-3.5 ${isLoading ? 'animate-spin' : ''}`}
              strokeWidth={1.75}
            />
            Actualizar
          </button>
        </div>
      </div>

      {error && (
        <div className="flex items-center gap-2 p-3 text-xs text-red-700 bg-red-50 border border-red-200 rounded-md">
          <AlertCircle className="w-4 h-4 shrink-0" strokeWidth={1.75} />
          <span>{error}</span>
        </div>
      )}

      <div className="bg-white border border-slate-200 shadow-sm rounded-md overflow-hidden">
        {isLoading ? (
          <div className="p-8 flex items-center justify-center gap-2 text-xs text-slate-500">
            <Loader2 className="w-4 h-4 animate-spin" strokeWidth={1.75} />
            <span>Cargando préstamos...</span>
          </div>
        ) : prestamosFiltrados.length === 0 ? (
          <div className="p-8 text-center text-xs text-slate-500">
            No se encontraron préstamos con los filtros seleccionados.
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-left border-collapse">
              <thead>
                <tr className="border-b border-slate-200 bg-slate-50">
                  <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500">ID</th>
                  <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500">
                    Ahorrador
                  </th>
                  <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500">
                    Otorgamiento
                  </th>
                  <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500 text-right">
                    Monto
                  </th>
                  <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500 text-right">
                    Tasa Mensual
                  </th>
                  <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500 text-center">
                    Plazo
                  </th>
                  <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500 text-right">
                    Saldo Insoluto
                  </th>
                  <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500">
                    Aval
                  </th>
                  <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500">
                    Estatus
                  </th>
                  <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500 text-right">
                    Detalle
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-200 text-xs text-slate-700">
                {prestamosFiltrados.map((p) => (
                  <tr key={p.id} className="hover:bg-slate-50/80">
                    <td className="py-2.5 px-4 font-mono text-slate-500">#{p.id}</td>
                    <td className="py-2.5 px-4 font-medium text-slate-900">{p.nombreAhorrador}</td>
                    <td className="py-2.5 px-4 font-mono">{p.fechaOtorgamiento}</td>
                    <td className="py-2.5 px-4 text-right font-mono">{formatCurrency(p.monto)}</td>
                    <td className="py-2.5 px-4 text-right font-mono">
                      {formatPercent(p.tasaInteresMensual)}
                    </td>
                    <td className="py-2.5 px-4 text-center">{p.plazoQuincenas} qnas</td>
                    <td className="py-2.5 px-4 text-right font-mono font-medium text-slate-900">
                      {formatCurrency(p.saldoInsoluto)}
                    </td>
                    <td className="py-2.5 px-4">
                      {p.requiereAval ? (
                        <div className="flex flex-col">
                          <span className="inline-flex items-center gap-1 text-amber-700 font-medium">
                            <ShieldAlert className="w-3.5 h-3.5" strokeWidth={1.75} />
                            Con aval
                          </span>
                          <span className="text-slate-600">{p.nombreAval ?? '—'}</span>
                        </div>
                      ) : (
                        <span className="text-slate-400">No requerido</span>
                      )}
                    </td>
                    <td className="py-2.5 px-4">
                      {p.estatus === 'ACTIVO' ? (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-xs font-medium bg-amber-50 text-amber-800 border border-amber-200">
                          <Clock className="w-3 h-3" strokeWidth={1.75} />
                          ACTIVO
                        </span>
                      ) : (
                        <span className="inline-flex items-center gap-1 px-2 py-0.5 rounded-md text-xs font-medium bg-emerald-50 text-emerald-800 border border-emerald-200">
                          <CheckCircle2 className="w-3 h-3" strokeWidth={1.75} />
                          LIQUIDADO
                        </span>
                      )}
                    </td>
                    <td className="py-2.5 px-4 text-right">
                      <button
                        type="button"
                        onClick={() => void abrirDetalle(p.id)}
                        className="inline-flex items-center gap-1 px-2.5 py-1 text-xs font-medium text-slate-700 bg-white border border-slate-200 rounded-md shadow-sm hover:bg-slate-50"
                      >
                        <Eye className="w-3.5 h-3.5" strokeWidth={1.75} />
                        Amortizaciones
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {(isLoadingDetalle || errorDetalle || detalle) && (
        <div className="bg-white border border-slate-200 shadow-sm rounded-md overflow-hidden">
          <div className="p-4 border-b border-slate-200 flex items-center justify-between bg-slate-50">
            <div className="flex items-center gap-2">
              <FileSpreadsheet className="w-4 h-4 text-slate-700" strokeWidth={1.75} />
              <h3 className="text-xs font-semibold uppercase tracking-wide text-slate-700">
                {detalle
                  ? `Tabla de Amortización · Préstamo #${detalle.prestamo.id} (${detalle.prestamo.nombreAhorrador})`
                  : 'Detalle de Amortizaciones'}
              </h3>
            </div>
            <button
              type="button"
              onClick={cerrarDetalle}
              className="p-1 text-slate-500 hover:text-slate-800 rounded-md hover:bg-slate-200/60"
              aria-label="Cerrar detalle"
            >
              <X className="w-4 h-4" strokeWidth={1.75} />
            </button>
          </div>

          {isLoadingDetalle ? (
            <div className="p-6 flex items-center justify-center gap-2 text-xs text-slate-500">
              <Loader2 className="w-4 h-4 animate-spin" strokeWidth={1.75} />
              <span>Consultando tabla de amortización...</span>
            </div>
          ) : errorDetalle ? (
            <div className="p-4 text-xs text-red-700 bg-red-50 flex items-center gap-2">
              <AlertCircle className="w-4 h-4 shrink-0" strokeWidth={1.75} />
              <span>{errorDetalle}</span>
            </div>
          ) : detalle ? (
            <div>
              <div className="p-4 grid grid-cols-2 sm:grid-cols-5 gap-3 border-b border-slate-200 bg-white text-xs">
                <div>
                  <span className="text-slate-500 block">Monto otorgado</span>
                  <span className="font-mono font-semibold text-slate-900">
                    {formatCurrency(detalle.prestamo.monto)}
                  </span>
                </div>
                <div>
                  <span className="text-slate-500 block">Saldo insoluto</span>
                  <span className="font-mono font-semibold text-slate-900">
                    {formatCurrency(detalle.prestamo.saldoInsoluto)}
                  </span>
                </div>
                <div>
                  <span className="text-slate-500 block">Tasa mensual</span>
                  <span className="font-mono text-slate-800">
                    {formatPercent(detalle.prestamo.tasaInteresMensual)}
                  </span>
                </div>
                <div>
                  <span className="text-slate-500 block">Aval</span>
                  <span className="text-slate-800">
                    {detalle.prestamo.requiereAval
                      ? detalle.prestamo.nombreAval ?? 'Sí'
                      : 'No requiere'}
                  </span>
                </div>
                <div>
                  <span className="text-slate-500 block">Fecha liquidación</span>
                  <span className="font-mono text-slate-800">
                    {detalle.prestamo.fechaLiquidacion ?? 'En curso'}
                  </span>
                </div>
              </div>

              <div className="overflow-x-auto max-h-96">
                <table className="w-full text-left border-collapse">
                  <thead className="sticky top-0 bg-slate-50 border-b border-slate-200">
                    <tr>
                      <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500">
                        Pago
                      </th>
                      <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500">
                        Vencimiento
                      </th>
                      <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500 text-right">
                        Saldo Inicial
                      </th>
                      <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500 text-right">
                        Capital
                      </th>
                      <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500 text-right">
                        Interés
                      </th>
                      <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500 text-right">
                        Cuota Fija
                      </th>
                      <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500 text-right">
                        Saldo Final
                      </th>
                      <th className="py-2.5 px-4 text-xs uppercase font-medium text-slate-500 text-center">
                        Estado
                      </th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-slate-200 text-xs text-slate-700">
                    {detalle.amortizaciones.map((a) => (
                      <tr key={a.numeroPago} className="hover:bg-slate-50/80">
                        <td className="py-2.5 px-4 font-mono text-slate-500">{a.numeroPago}</td>
                        <td className="py-2.5 px-4 font-mono">{a.fechaVencimiento}</td>
                        <td className="py-2.5 px-4 text-right font-mono">
                          {formatCurrency(a.saldoInicial)}
                        </td>
                        <td className="py-2.5 px-4 text-right font-mono">
                          {formatCurrency(a.capital)}
                        </td>
                        <td className="py-2.5 px-4 text-right font-mono">
                          {formatCurrency(a.interes)}
                        </td>
                        <td className="py-2.5 px-4 text-right font-mono font-medium text-slate-900">
                          {formatCurrency(a.cuota)}
                        </td>
                        <td className="py-2.5 px-4 text-right font-mono">
                          {formatCurrency(a.saldoFinal)}
                        </td>
                        <td className="py-2.5 px-4 text-center">
                          {a.pagada ? (
                            <span className="inline-flex items-center px-2 py-0.5 rounded-md text-xs font-medium bg-emerald-50 text-emerald-700 border border-emerald-200">
                              Pagada
                            </span>
                          ) : (
                            <span className="inline-flex items-center px-2 py-0.5 rounded-md text-xs font-medium bg-slate-100 text-slate-600 border border-slate-200">
                              Pendiente
                            </span>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          ) : null}
        </div>
      )}
    </div>
  )
}

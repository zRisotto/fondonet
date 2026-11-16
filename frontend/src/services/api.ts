import type {
  AhorradorResumenDto,
  AvalDisponibleDto,
  CajaAhorroResumenDto,
  CapacidadCreditoDto,
  DispersionSpeiDto,
  EstadoCuentaDto,
  EstatusCierreDto,
  IncidenciaNominaDto,
  LoginRequest,
  LoginResponse,
  PrestamoDetalleDto,
  PrestamoDto,
  ResultadoCierreAnualDto,
  ResultadoImportacionNomina,
  SimulacionPrestamoDto,
  SolicitudPrestamoRequest,
} from '../types'

const API_BASE_URL = (import.meta.env.VITE_API_URL || 'http://localhost:5000').replace(/\/$/, '')
const SESSION_KEY = 'fondonet_session'

const mxnFormatter = new Intl.NumberFormat('es-MX', {
  style: 'currency',
  currency: 'MXN',
  minimumFractionDigits: 2,
  maximumFractionDigits: 2,
})

export function formatCurrency(value: number): string {
  return mxnFormatter.format(value)
}

export function formatPercent(decimalRate: number): string {
  return `${(decimalRate * 100).toFixed(2)}%`
}

export function getSession(): LoginResponse | null {
  try {
    const raw = sessionStorage.getItem(SESSION_KEY)
    if (!raw) return null
    return JSON.parse(raw) as LoginResponse
  } catch {
    return null
  }
}

export function saveSession(session: LoginResponse): void {
  sessionStorage.setItem(SESSION_KEY, JSON.stringify(session))
}

export function clearSession(): void {
  sessionStorage.removeItem(SESSION_KEY)
}

interface ErrorResponse {
  error?: string
}

async function parseErrorMessage(response: Response): Promise<string> {
  try {
    const data = (await response.json()) as ErrorResponse
    if (data?.error) return data.error
  } catch {
    // ignore non-json error body
  }
  if (response.status === 401) return 'Sesión expirada o credenciales inválidas.'
  if (response.status === 403) return 'No tiene permisos para realizar esta operación.'
  if (response.status === 404) return 'El recurso solicitado no fue encontrado.'
  return `Error en la solicitud (HTTP ${response.status}).`
}

async function request<T>(path: string, init: RequestInit = {}): Promise<T> {
  const session = getSession()
  const headers = new Headers(init.headers)

  if (!headers.has('Content-Type') && !(init.body instanceof FormData)) {
    headers.set('Content-Type', 'application/json')
  }
  if (session?.token) {
    headers.set('Authorization', `Bearer ${session.token}`)
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...init,
    headers,
  })

  if (!response.ok) {
    if (response.status === 401 && path !== '/api/auth/login') {
      clearSession()
      window.dispatchEvent(new CustomEvent('fondonet:unauthorized'))
    }
    throw new Error(await parseErrorMessage(response))
  }

  return (await response.json()) as T
}

function extractFilenameFromContentDisposition(header: string | null, fallback: string): string {
  if (!header) return fallback
  const utf8Match = header.match(/filename\*=UTF-8''([^;\n]+)/i)
  if (utf8Match?.[1]) {
    try {
      return decodeURIComponent(utf8Match[1].trim().replace(/^"|"$/g, ''))
    } catch {
      return utf8Match[1].trim()
    }
  }
  const asciiMatch = header.match(/filename="?([^";\n]+)"?/i)
  if (asciiMatch?.[1]) {
    return asciiMatch[1].trim()
  }
  return fallback
}

async function downloadFile(path: string, fallbackName: string): Promise<string> {
  const session = getSession()
  const headers = new Headers()
  if (session?.token) {
    headers.set('Authorization', `Bearer ${session.token}`)
  }

  const response = await fetch(`${API_BASE_URL}${path}`, {
    method: 'GET',
    headers,
  })

  if (!response.ok) {
    if (response.status === 401) {
      clearSession()
      window.dispatchEvent(new CustomEvent('fondonet:unauthorized'))
    }
    throw new Error(await parseErrorMessage(response))
  }

  const filename = extractFilenameFromContentDisposition(
    response.headers.get('Content-Disposition'),
    fallbackName,
  )
  const blob = await response.blob()
  const objectUrl = URL.createObjectURL(blob)
  const link = document.createElement('a')
  link.href = objectUrl
  link.download = filename
  document.body.appendChild(link)
  link.click()
  link.remove()
  URL.revokeObjectURL(objectUrl)
  return filename
}

export const api = {
  login(payload: LoginRequest): Promise<LoginResponse> {
    return request<LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  listarCajas(): Promise<CajaAhorroResumenDto[]> {
    return request<CajaAhorroResumenDto[]>('/api/ahorradores/cajas')
  },

  listarAhorradores(cajaAhorroId?: number): Promise<AhorradorResumenDto[]> {
    const query = cajaAhorroId ? `?cajaAhorroId=${cajaAhorroId}` : ''
    return request<AhorradorResumenDto[]>(`/api/ahorradores${query}`)
  },

  listarAvalesDisponibles(ahorradorId?: number): Promise<AvalDisponibleDto[]> {
    const query = ahorradorId ? `?ahorradorId=${ahorradorId}` : ''
    return request<AvalDisponibleDto[]>(`/api/ahorradores/avales-disponibles${query}`)
  },

  obtenerEstadoCuenta(ahorradorId: number, anio?: number): Promise<EstadoCuentaDto> {
    const query = anio ? `?anio=${anio}` : ''
    return request<EstadoCuentaDto>(`/api/ahorradores/${ahorradorId}/estado-cuenta${query}`)
  },

  listarPrestamos(params?: { ahorradorId?: number; cajaAhorroId?: number }): Promise<PrestamoDto[]> {
    const search = new URLSearchParams()
    if (params?.ahorradorId) search.set('ahorradorId', String(params.ahorradorId))
    if (params?.cajaAhorroId) search.set('cajaAhorroId', String(params.cajaAhorroId))
    const qs = search.toString()
    return request<PrestamoDto[]>(`/api/prestamos${qs ? `?${qs}` : ''}`)
  },

  obtenerPrestamo(id: number): Promise<PrestamoDetalleDto> {
    return request<PrestamoDetalleDto>(`/api/prestamos/${id}`)
  },

  obtenerCapacidadCredito(ahorradorId: number, fecha?: string): Promise<CapacidadCreditoDto> {
    const query = fecha ? `?fecha=${encodeURIComponent(fecha)}` : ''
    return request<CapacidadCreditoDto>(`/api/prestamos/capacidad/${ahorradorId}${query}`)
  },

  simularPrestamo(payload: SolicitudPrestamoRequest): Promise<SimulacionPrestamoDto> {
    return request<SimulacionPrestamoDto>('/api/prestamos/simular', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  solicitarPrestamo(payload: SolicitudPrestamoRequest): Promise<PrestamoDetalleDto> {
    return request<PrestamoDetalleDto>('/api/prestamos', {
      method: 'POST',
      body: JSON.stringify(payload),
    })
  },

  importarNomina(cajaAhorroId: number, archivo: File): Promise<ResultadoImportacionNomina> {
    const formData = new FormData()
    formData.append('archivo', archivo)
    return request<ResultadoImportacionNomina>(`/api/nomina/importar/${cajaAhorroId}`, {
      method: 'POST',
      body: formData,
    })
  },

  obtenerEstatusCierre(anio: number): Promise<EstatusCierreDto> {
    return request<EstatusCierreDto>(`/api/cierre/${anio}/estatus`)
  },

  ejecutarCierre(anio: number, simulacion: boolean): Promise<ResultadoCierreAnualDto> {
    return request<ResultadoCierreAnualDto>(`/api/cierre/${anio}?simulacion=${simulacion}`, {
      method: 'POST',
    })
  },

  obtenerIncidenciasNomina(cajaAhorroId: number, quincena: string): Promise<IncidenciaNominaDto[]> {
    return request<IncidenciaNominaDto[]>(
      `/api/layouts/nomina/${cajaAhorroId}?quincena=${encodeURIComponent(quincena)}`,
    )
  },

  descargarIncidenciasNomina(cajaAhorroId: number, quincena: string): Promise<string> {
    return downloadFile(
      `/api/layouts/nomina/${cajaAhorroId}/archivo?quincena=${encodeURIComponent(quincena)}`,
      `incidencias_nomina_${cajaAhorroId}_${quincena}.csv`,
    )
  },

  obtenerDispersionSpei(cajaAhorroId: number, anio: number): Promise<DispersionSpeiDto[]> {
    return request<DispersionSpeiDto[]>(`/api/layouts/spei/${cajaAhorroId}/${anio}`)
  },

  descargarDispersionSpei(cajaAhorroId: number, anio: number): Promise<string> {
    return downloadFile(
      `/api/layouts/spei/${cajaAhorroId}/${anio}/archivo`,
      `dispersion_spei_${cajaAhorroId}_${anio}.txt`,
    )
  },
}

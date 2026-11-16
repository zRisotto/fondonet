export type RolUsuario = 'Admin' | 'Ahorrador'

export interface LoginRequest {
  usuario: string
  password: string
}

export interface LoginResponse {
  token: string
  expiraUtc: string
  usuario: string
  rol: RolUsuario
  ahorradorId: number | null
}

export interface CajaAhorroResumenDto {
  id: number
  nombre: string
  empresaNombre: string
  tasaInteresMensual: number
  porcentajeAhorro: number
}

export interface AhorradorResumenDto {
  id: number
  cajaAhorroId: number
  cajaAhorroNombre: string
  empresaNombre: string
  claveEmpleado: string
  nombre: string
  curp: string
  clabe: string
  estatus: 'ACTIVO' | 'INACTIVO' | 'LICENCIA'
  salarioBase: number
  ahorroVoluntarioQuincenal: number
  fechaIngreso: string
  fechaBaja: string | null
  saldoCicloActual: number
}

export interface AvalDisponibleDto {
  id: number
  claveEmpleado: string
  nombre: string
}

export interface MovimientoAhorroDto {
  id: number
  fecha: string
  tipo: 'ORDINARIO' | 'PATRONAL' | 'VOLUNTARIO' | 'RENDIMIENTO' | 'RETIRO'
  monto: number
  concepto: string
}

export interface SolicitudPrestamoRequest {
  ahorradorId: number
  monto: number
  plazoQuincenas: number
  tasaInteresMensual?: number | null
  fechaSolicitud?: string | null
  avalAhorradorId?: number | null
}

export interface CapacidadCreditoDto {
  ahorradorId: number
  fechaCorte: string
  ahorroOrdinario: number
  ahorroVoluntario: number
  ahorroBase: number
  montoMaximoSinAval: number
}

export interface AmortizacionDto {
  numeroPago: number
  fechaVencimiento: string
  saldoInicial: number
  capital: number
  interes: number
  cuota: number
  saldoFinal: number
  pagada: boolean
}

export interface SimulacionPrestamoDto {
  capacidad: CapacidadCreditoDto
  monto: number
  tasaInteresMensual: number
  plazoQuincenas: number
  requiereAval: boolean
  cuotaQuincenal: number
  totalIntereses: number
  amortizaciones: AmortizacionDto[]
}

export interface PrestamoDto {
  id: number
  ahorradorId: number
  nombreAhorrador: string
  fechaOtorgamiento: string
  monto: number
  tasaInteresMensual: number
  plazoQuincenas: number
  estatus: 'ACTIVO' | 'LIQUIDADO'
  saldoInsoluto: number
  requiereAval: boolean
  avalAhorradorId: number | null
  nombreAval: string | null
  fechaLiquidacion: string | null
}

export interface PrestamoDetalleDto {
  prestamo: PrestamoDto
  amortizaciones: AmortizacionDto[]
}

export interface EstadoCuentaDto {
  ahorrador: AhorradorResumenDto
  anio: number
  totalOrdinario: number
  totalPatronal: number
  totalVoluntario: number
  totalRendimientos: number
  totalRetiros: number
  saldoNeto: number
  capacidadPrestamoSinAval: number
  movimientos: MovimientoAhorroDto[]
  prestamos: PrestamoDto[]
}

export interface ErrorRenglon {
  renglon: number
  mensaje: string
}

export interface CambioAhorrador {
  renglon: number
  ahorradorId: number
  claveEmpleado: string
  tipoMovimiento: string
  estatusAnterior: string
  estatusNuevo: string
  salarioBase: number
}

export interface ResultadoImportacionNomina {
  cajaAhorroId: number
  nombreArchivo: string
  renglonesLeidos: number
  renglonesAplicados: number
  cambios: CambioAhorrador[]
  errores: ErrorRenglon[]
}

export interface EstatusCierreDto {
  anio: number
  cerrado: boolean
}

export interface CierreAhorradorDto {
  ahorradorId: number
  nombre: string
  factor: number
  rendimiento: number
  retiro: number
}

export interface CierreCajaDto {
  cajaAhorroId: number
  nombreCaja: string
  bolsa: number
  sumaFactores: number
  totalRendimientos: number
  totalRetiros: number
  ahorradores: CierreAhorradorDto[]
}

export interface ResultadoCierreAnualDto {
  anio: number
  simulacion: boolean
  yaCerrado: boolean
  totalBolsa: number
  totalRendimientos: number
  cajas: CierreCajaDto[]
}

export interface IncidenciaNominaDto {
  claveEmpleado: string
  descuentoAhorroOrdinario: number
  descuentoAhorroVoluntario: number
  cuotaAmortizacionPrestamo: number
  totalDeduccion: number
}

export interface DispersionSpeiDto {
  cuentaRetiro: string
  clabeDestino: string
  montoTotal: number
  nombreBeneficiario: string
  concepto: string
}

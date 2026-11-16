# Contexto y Requerimientos del Sistema: Caja de Ahorro Multiempresa

## 1. Módulo de Captación y Descuentos por Nómina
- **Sincronización de Altas, Bajas e Incidencias:**
  - Importación por archivo (formato estándar `.csv` o `.xlsx`) con los campos: `ClaveEmpleado`, `Fecha`, `TipoMovimiento` (ALTA, BAJA, INCAPACIDAD, LICENCIA), `SalarioBase`.
  - Actualización automática del estatus del ahorrador en el sistema según la incidencia.
- **Cálculo y Layout de Descuentos (Nómina):**
  - Generación de archivo plano de incidencias para nómina que reporte por empleado: `ClaveEmpleado`, `DescuentoAhorroOrdinario`, `DescuentoAhorroVoluntario`, `CuotaAmortizacionPrestamo`, `TotalDeduccion`.
- **Ahorro Voluntario:**
  - Registro contable separado del ahorro ordinario; no genera aportación patronal pero sí genera rendimientos anuales.

## 2. Módulo de Préstamos Internos (Tasas Sociales)
- **Capacidad de Crédito (Regla de negocio):**
  - Monto máximo permitido: 80% del saldo acumulado en ahorro ordinario + voluntario a la fecha de solicitud.
  - Excepción: Solicitudes superiores al 80% requieren marcar la bandera `RequiereAval = true` y capturar datos de un aval registrado.
- **Amortización:**
  - Método sobre saldos insolutos: El interés quincenal/mensual se aplica únicamente sobre el saldo deudor pendiente.
  - Tasa parametrizable: 1% a 2% mensual.
  - Cobro automático deducido mediante el layout de nómina.

## 3. Distribución de Rendimientos (Cierre Anual)
- **Bolsa de Excedentes:**
  - Suma de todos los intereses reales cobrados por préstamos liquidados o en curso durante el año fiscal.
- **Algoritmo de Reparto (Días / Saldo Promedio Ponderado):**
  - Factor por usuario: $\sum (\text{Saldo Diario} \times \text{Días de Permanencia}) / 365$.
  - Distribución: $(\text{Factor Individual} / \text{Suma de Factores}) \times \text{Bolsa de Intereses}$.
- **Layout de Dispersión Bancaria:**
  - Archivo de texto plano delimitado para dispersión masiva SPEI (estándar bancario BBVA/Banamex) conteniendo: `CuentaRetiro`, `CLABE_Destino`, `MontoTotal` (Ahorro + Rendimientos), `NombreBeneficiario`, `Concepto`.

## 4. Requerimientos de Base de Datos y Datos Semilla
- **Multiempresa (Multi-tenant):**
  - Esquema con entidades `Empresa` y `CajaAhorro` vinculadas a las cuentas de los empleados para soportar múltiples compañías en la misma instancia.
- **Datos de Empleados:**
  - Cada ahorrador debe contener: `Id`, `CajaAhorroId`, `Nombre`, `CURP`, `FechaNacimiento` (para cálculo de edad), `CLABE`, `Estatus` (Activo, Inactivo, Licencia), `FechaIngreso`, `FechaBaja`.
- **Volumen de Datos Inicial (Seeding):**
  - Histórico de movimientos quincenales completos para al menos 5 ahorradores durante todo el año inmediato anterior (ciclo cerrado) y el año en curso (ciclo activo).
  - Incluir al menos 2 préstamos activos, 1 préstamo liquidado y 1 empleado dado de baja.

## 5. Consultas Analíticas Distribuidas (Parte 2)
Consultas preparadas para ejecutarse contra el motor analítico/federador sobre el modelo consolidado:
1. Listado global de ahorradores, estatus actual y caja de ahorro/empresa a la que pertenecen.
2. Listado de ahorradores dados de baja en el ciclo.
3. Listado de ahorradores con su edad calculada a partir de `FechaNacimiento`.
4. Listado de ahorradores con préstamos registrados en un año fiscal $X$.
5. Listado de ahorradores activos en un año $X$, desglosando balance de movimientos (total aportado, total amortizado) y utilidad neta asignada por rendimientos.
# FondoNet — Caja de Ahorro Multiempresa

Proyecto escolar mediano (no producción). Prioriza que funcione, sea legible y se pueda demostrar.

## Stack
- Backend: ASP.NET Core 8 Web API, EF Core + Pomelo (MariaDB 11)
- Frontend: React 18 + TypeScript + Vite + Tailwind + lucide-react
- OLAP: Trino con catálogo JDBC a MariaDB
- Infra: Docker Compose, una sola red bridge

## Estructura
```
backend/src/{Domain,Application,Infrastructure,API}   backend/Dockerfile
frontend/src/{components,pages,services,types}        frontend/Dockerfile
trino/etc/{config.properties,jvm.config,node.properties,catalog/mariadb.properties}
docker-compose.yml  README.md  docs/{PROYECTO.md,PLAN.md}
```
- Domain sin dependencias externas. Application: DTOs e interfaces. Infrastructure: EF Core. API: controllers delgados.

## Cómo trabajar
- Un sprint a la vez (ver `docs/PLAN.md`). No avances sin que el usuario apruebe el checklist.
- Cambios quirúrgicos: edita solo lo necesario. Reescribe un archivo completo solo si es nuevo o si el cambio toca más de la mitad.
- No instales dependencias, no refactorices ni renombres lo que no se pidió.
- Si un requerimiento es ambiguo, pregunta antes de asumir.
- Antes de buscar en el repo, usa la estructura de arriba.
- Requerimientos de negocio completos: `docs/PROYECTO.md` (léelo solo cuando la tarea lo necesite).

## Docker
- Servicios se resuelven por nombre (`mariadb:3306`, `backend`, `trino`), nunca `localhost`.
- Antes de cerrar una tarea, revisa cadenas de conexión y URLs entre servicios.

## Backend
- Contrato = Swagger/DTOs de Application. No inventes endpoints.
- Dinero siempre `decimal`, nunca `double`. Redondeo a 2 decimales; el residuo del reparto de rendimientos se asigna al último ahorrador para que la suma cuadre exacto.
- Roles: Admin y Ahorrador.

## Frontend
- Estilo Linear/Stripe: fondo `slate-50`, `border-slate-200`, `shadow-sm`, `rounded-md`, fuente Inter. Sin gradientes morados ni sombras grandes.
- Tablas compactas (`py-2.5 px-4`, encabezados `text-xs uppercase text-slate-500`).
- Íconos solo `lucide-react` (`strokeWidth={1.75}`).
- TypeScript estricto: sin `any`, todo payload con `interface`/`type`.
- Los componentes no hacen `fetch`; todo va por `services/api.ts` tipado.
- Cada tabla/formulario maneja `isLoading`, `error` y estado vacío.

## Reglas de negocio clave
- Préstamo máximo = 80% del ahorro (ordinario + voluntario). Si excede: `RequiereAval = true` + datos del aval.
- Interés sobre saldo insoluto, tasa parametrizable 1%–2% mensual, cobro vía layout de nómina.
- Ahorro voluntario: sin aportación patronal, sí genera rendimientos.
- Bolsa = intereses cobrados en el año. Reparto: `factor = Σ(saldo diario × días) / 365`; `rendimiento = factor / Σfactores × bolsa`.
- Importación de nómina `.csv`/`.xlsx`: `ClaveEmpleado, Fecha, TipoMovimiento (ALTA|BAJA|INCAPACIDAD|LICENCIA), SalarioBase`; actualiza el estatus del ahorrador.
- Layouts de salida: incidencias de nómina y dispersión SPEI (ver campos en `docs/PROYECTO.md`).
- Multiempresa: `Empresa` → `CajaAhorro` → `Ahorrador`.

## Decisiones de modelo
- El ciclo de ahorro se liquida cada año: al 31/dic hay un `RETIRO` por el saldo total (ahorro + patronal + rendimientos) de cada ahorrador activo; el saldo arranca en 0 el 1/ene. Los préstamos no se descuentan del retiro.
- `PATRONAL` es 1:1 con `ORDINARIO` y cuenta para el saldo. `VOLUNTARIO` no tiene patronal.
- Préstamos: cuota fija quincenal; tasa quincenal = tasa mensual / 2; interés sobre saldo insoluto.
- Los préstamos cruzan el año: siguen corriendo en el ciclo siguiente aunque el ahorro se haya liquidado.
- `salario_base` es mensual.
- Tope del 80% se mide contra el saldo del ciclo vigente a la fecha de solicitud.

## Datos semilla (Sprint 2)
- Mínimo 5 ahorradores, movimientos quincenales del año anterior (cerrado) y el actual (activo).
- Mínimo 2 préstamos activos, 1 liquidado y 1 baja laboral.

## Comandos
- Levantar todo: `docker compose up -d --build`
- Swagger: `http://localhost:<puerto-backend>/swagger`

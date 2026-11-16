# Plan de Implementación y Workflow de Agente: FondoNet

## 1. Modo de Operación del Agente
- **Autonomía Supervisada:** Ejecuta exactamente un sprint a la vez. No avances al siguiente sprint sin que el usuario valide y apruebe el checklist actual.
- **Eficiencia de Contexto:** Modifica archivos de forma quirúrgica; no rescribas archivos completos a menos que se trate de su creación inicial.
- **Docker-First:** Todo servicio debe resolverse por nombres de contenedor dentro de la red de Docker (`mariadb:3306`, `backend:5000`, etc.), nunca `localhost`.

---

## 2. Roadmap por Sprints

### Sprint 1: Orquestación e Infraestructura Base
- **Objetivo:** Levantar entorno reproducible en Docker con conectividad verificada.
- **Entregables:**
  - `docker-compose.yml` con servicios: `mariadb`, `trino`, `backend` y `frontend` en red bridge común.
  - Configuración inicial de Trino (`catalog/mariadb.properties`).
- **Verificación:** `docker compose up -d` levanta todos los contenedores con status healthy. Consulta de prueba exitosa en Trino apuntando a MariaDB vía CLI/DBeaver.

### Sprint 2: Persistencia, Modelo Multi-Tenant y Seeding
- **Objetivo:** Base de datos lista con modelo multiempresa y datos históricos de prueba.
- **Entregables:**
  - Esquema relacional en MariaDB: `Empresas`, `CajasAhorro`, `Ahorradores`, `MovimientosAhorro`, `Prestamos`, `Amortizaciones`.
  - Script SQL de seeding: Movimientos quincenales completos para 6 ahorradores del año anterior (ciclo cerrado) y año actual (ciclo activo), con al menos 2 préstamos y 1 baja laboral registrada.
  - Entidades y `DbContext` en .NET con EF Core / Pomelo.
- **Verificación:** Inserción limpia del script sin errores de llaves foráneas; comprobación de conteo de registros por consulta directa.

### Sprint 3: Core Transaccional y Lógica Financiera (.NET API)
- **Objetivo:** Lógica de negocio y endpoints REST funcionales.
- **Entregables:**
  - Parser de nómina para altas/bajas/incidencias (`.csv`/`.xlsx`).
  - Motor de amortización de préstamos sobre saldos insolutos (validación estricta de tope del 80%).
  - Tarea periódica y algoritmo de cierre anual por Días/Saldo Promedio Ponderado.
  - Generador de layouts de salida: Incidencias de nómina y archivo bancario SPEI.
  - Control de acceso básico (Admin vs. Ahorrador).
- **Verificación:** Pruebas vía Swagger (`/swagger`): simular solicitud de préstamo (con validación de tope), simular carga de nómina y validar que la suma de rendimientos cuadre exactamente con la bolsa de intereses.

### Sprint 4: Frontend y Consultas Distribuidas (Entrega Final)
- **Objetivo:** Interfaz gráfica limpia (Stripe/Linear style) y queries analíticas.
- **Entregables:**
  - Frontend React + Tailwind: Dashboard Admin (gestión préstamos, carga de incidencias, corrida de rendimientos) y portal Ahorrador (estado de cuenta, simulador de crédito).
  - Script SQL de las 5 consultas requeridas configuradas y listas para ejecutarse en Trino (Censo, Bajas, Edades, Préstamos por año, Utilidades y balances).
  - `README.md` conciso con instrucciones de despliegue (`docker compose up`) y guía de ejecución de las queries analíticas.
- **Verificación:** Flujo web completo navegable y ejecución de las 5 consultas en Trino arrojando los resultados esperados sobre los datos del Sprint 2.

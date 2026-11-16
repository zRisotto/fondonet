-- =====================================================================
-- FondoNet · Esquema relacional (MariaDB 11)
-- Se ejecuta automáticamente desde /docker-entrypoint-initdb.d
-- solo cuando el volumen de datos está vacío.
-- Convención: tablas y columnas en snake_case minúsculas.
-- Dinero: DECIMAL(18,2). Tasas y porcentajes: DECIMAL(5,4).
-- Catálogos como VARCHAR + CHECK (no ENUM) para compatibilidad con Trino.
-- =====================================================================
SET NAMES utf8mb4;
USE fondonet;

CREATE TABLE empresas (
    id          INT AUTO_INCREMENT PRIMARY KEY,
    nombre      VARCHAR(150) NOT NULL,
    rfc         VARCHAR(13)  NOT NULL,
    activa      BOOLEAN      NOT NULL DEFAULT TRUE,
    fecha_alta  DATE         NOT NULL,
    CONSTRAINT uq_empresas_rfc UNIQUE (rfc)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE cajas_ahorro (
    id                    INT AUTO_INCREMENT PRIMARY KEY,
    empresa_id            INT          NOT NULL,
    nombre                VARCHAR(150) NOT NULL,
    cuenta_retiro         CHAR(18)     NOT NULL,           -- cuenta origen para dispersión SPEI
    tasa_interes_mensual  DECIMAL(5,4) NOT NULL,           -- tasa por defecto de préstamos
    porcentaje_ahorro     DECIMAL(5,4) NOT NULL,           -- % del salario quincenal (ordinario); patronal aporta 1:1
    activa                BOOLEAN      NOT NULL DEFAULT TRUE,
    CONSTRAINT fk_cajas_ahorro_empresa FOREIGN KEY (empresa_id) REFERENCES empresas (id),
    CONSTRAINT ck_cajas_ahorro_tasa CHECK (tasa_interes_mensual BETWEEN 0.0100 AND 0.0200),
    CONSTRAINT ck_cajas_ahorro_porcentaje CHECK (porcentaje_ahorro > 0 AND porcentaje_ahorro < 1)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE ahorradores (
    id                           INT AUTO_INCREMENT PRIMARY KEY,
    caja_ahorro_id               INT           NOT NULL,
    clave_empleado               VARCHAR(20)   NOT NULL,
    nombre                       VARCHAR(150)  NOT NULL,
    curp                         CHAR(18)      NOT NULL,
    fecha_nacimiento             DATE          NOT NULL,
    clabe                        CHAR(18)      NOT NULL,
    estatus                      VARCHAR(20)   NOT NULL DEFAULT 'ACTIVO',
    salario_base                 DECIMAL(18,2) NOT NULL,   -- mensual
    ahorro_voluntario_quincenal  DECIMAL(18,2) NOT NULL DEFAULT 0,
    fecha_ingreso                DATE          NOT NULL,
    fecha_baja                   DATE          NULL,
    CONSTRAINT fk_ahorradores_caja FOREIGN KEY (caja_ahorro_id) REFERENCES cajas_ahorro (id),
    CONSTRAINT uq_ahorradores_curp UNIQUE (curp),
    CONSTRAINT uq_ahorradores_clave UNIQUE (caja_ahorro_id, clave_empleado),
    CONSTRAINT ck_ahorradores_estatus CHECK (estatus IN ('ACTIVO', 'INACTIVO', 'LICENCIA')),
    CONSTRAINT ck_ahorradores_baja CHECK (fecha_baja IS NULL OR fecha_baja >= fecha_ingreso),
    CONSTRAINT ck_ahorradores_voluntario CHECK (ahorro_voluntario_quincenal >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Monto con signo: aportaciones y rendimientos > 0, RETIRO < 0.
CREATE TABLE movimientos_ahorro (
    id            INT AUTO_INCREMENT PRIMARY KEY,
    ahorrador_id  INT           NOT NULL,
    fecha         DATE          NOT NULL,
    tipo          VARCHAR(20)   NOT NULL,
    monto         DECIMAL(18,2) NOT NULL,
    concepto      VARCHAR(200)  NOT NULL,
    CONSTRAINT fk_movimientos_ahorrador FOREIGN KEY (ahorrador_id) REFERENCES ahorradores (id),
    CONSTRAINT ck_movimientos_tipo CHECK (tipo IN ('ORDINARIO', 'PATRONAL', 'VOLUNTARIO', 'RENDIMIENTO', 'RETIRO')),
    CONSTRAINT ck_movimientos_signo CHECK ((tipo = 'RETIRO' AND monto < 0) OR (tipo <> 'RETIRO' AND monto > 0)),
    INDEX ix_movimientos_ahorrador_fecha (ahorrador_id, fecha)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE prestamos (
    id                    INT AUTO_INCREMENT PRIMARY KEY,
    ahorrador_id          INT           NOT NULL,
    fecha_otorgamiento    DATE          NOT NULL,
    monto                 DECIMAL(18,2) NOT NULL,
    tasa_interes_mensual  DECIMAL(5,4)  NOT NULL,
    plazo_quincenas       INT           NOT NULL,
    estatus               VARCHAR(20)   NOT NULL DEFAULT 'ACTIVO',
    saldo_insoluto        DECIMAL(18,2) NOT NULL,
    requiere_aval         BOOLEAN       NOT NULL DEFAULT FALSE,
    aval_ahorrador_id     INT           NULL,
    fecha_liquidacion     DATE          NULL,
    CONSTRAINT fk_prestamos_ahorrador FOREIGN KEY (ahorrador_id) REFERENCES ahorradores (id),
    CONSTRAINT fk_prestamos_aval FOREIGN KEY (aval_ahorrador_id) REFERENCES ahorradores (id),
    CONSTRAINT ck_prestamos_monto CHECK (monto > 0),
    CONSTRAINT ck_prestamos_tasa CHECK (tasa_interes_mensual BETWEEN 0.0100 AND 0.0200),
    CONSTRAINT ck_prestamos_plazo CHECK (plazo_quincenas > 0),
    CONSTRAINT ck_prestamos_estatus CHECK (estatus IN ('ACTIVO', 'LIQUIDADO')),
    CONSTRAINT ck_prestamos_aval CHECK (requiere_aval = FALSE OR aval_ahorrador_id IS NOT NULL),
    CONSTRAINT ck_prestamos_aval_distinto CHECK (aval_ahorrador_id IS NULL OR aval_ahorrador_id <> ahorrador_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

-- Tabla de amortización quincenal sobre saldo insoluto (cuota fija).
-- pagada = 1 cuando la cuota ya se descontó vía nómina.
CREATE TABLE amortizaciones (
    id                 INT AUTO_INCREMENT PRIMARY KEY,
    prestamo_id        INT           NOT NULL,
    numero_pago        INT           NOT NULL,
    fecha_vencimiento  DATE          NOT NULL,
    saldo_inicial      DECIMAL(18,2) NOT NULL,
    capital            DECIMAL(18,2) NOT NULL,
    interes            DECIMAL(18,2) NOT NULL,
    cuota              DECIMAL(18,2) NOT NULL,
    saldo_final        DECIMAL(18,2) NOT NULL,
    pagada             BOOLEAN       NOT NULL DEFAULT FALSE,
    CONSTRAINT fk_amortizaciones_prestamo FOREIGN KEY (prestamo_id) REFERENCES prestamos (id),
    CONSTRAINT uq_amortizaciones_pago UNIQUE (prestamo_id, numero_pago),
    CONSTRAINT ck_amortizaciones_cuota CHECK (cuota = capital + interes),
    CONSTRAINT ck_amortizaciones_saldo CHECK (saldo_final = saldo_inicial - capital AND saldo_final >= 0)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

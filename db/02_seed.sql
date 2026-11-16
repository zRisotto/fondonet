-- =====================================================================
-- FondoNet · Datos semilla
-- Ciclo anterior 2025 (cerrado, con rendimientos repartidos) y
-- ciclo actual 2026 (activo, con corte en la última quincena pagada).
-- Movimientos, amortizaciones y rendimientos se GENERAN con CTE.
-- =====================================================================
SET NAMES utf8mb4;
USE fondonet;

SET @inicio_historico := '2025-01-15';   -- primera quincena con descuento
SET @ini_anterior     := '2025-01-01';   -- ejercicio cerrado
SET @fin_anterior     := '2025-12-31';
SET @fecha_corte      := '2026-10-31';   -- última quincena aplicada del ciclo activo

-- ---------------------------------------------------------------------
-- 1. Catálogos: empresas, cajas y ahorradores
-- ---------------------------------------------------------------------
INSERT INTO empresas (id, nombre, rfc, activa, fecha_alta) VALUES
    (1, 'Manufacturas del Bajío S.A. de C.V.',        'MBA100315AB1', TRUE, '2010-03-15'),
    (2, 'Servicios Logísticos del Norte S.A. de C.V.', 'SLN120820CD2', TRUE, '2012-08-20');

INSERT INTO cajas_ahorro (id, empresa_id, nombre, cuenta_retiro, tasa_interes_mensual, porcentaje_ahorro) VALUES
    (1, 1, 'Caja de Ahorro Bajío',  '012180000100200301', 0.0150, 0.0800),
    (2, 2, 'Caja de Ahorro Norte',  '002580000400500602', 0.0120, 0.1000);

INSERT INTO ahorradores
    (id, caja_ahorro_id, clave_empleado, nombre, curp, fecha_nacimiento, clabe, estatus,
     salario_base, ahorro_voluntario_quincenal, fecha_ingreso, fecha_baja) VALUES
    (1, 1, 'EMP-001', 'Ana María López Hernández', 'LOHA880412MGTPRN05', '1988-04-12', '012180001234567891', 'ACTIVO',   18000.00,  500.00, '2019-03-01', NULL),
    (2, 1, 'EMP-002', 'Carlos Ramírez Ortega',     'RAOC820923HGTMRR08', '1982-09-23', '002180009876543210', 'ACTIVO',   22000.00,    0.00, '2018-08-16', NULL),
    (3, 1, 'EMP-003', 'Lucía Fernández Torres',    'FETL950130MGTRRC02', '1995-01-30', '014180005555666677', 'ACTIVO',   15000.00,  300.00, '2025-04-01', NULL),
    (4, 2, 'EMP-101', 'Jorge Méndez Ruiz',         'MERJ791105HNLNZR01', '1979-11-05', '072580001112223334', 'ACTIVO',   25000.00, 1000.00, '2017-01-09', NULL),
    (5, 2, 'EMP-102', 'Sofía Castillo Navarro',    'CANS900718MNLSVF09', '1990-07-18', '021580004445556667', 'ACTIVO',   16000.00,    0.00, '2020-10-01', NULL),
    (6, 2, 'EMP-103', 'Ricardo Vega Morales',      'VEMR850214HNLGRC04', '1985-02-14', '044580007778889990', 'INACTIVO', 14000.00,  200.00, '2021-05-17', '2026-06-30');

-- ---------------------------------------------------------------------
-- 2. Movimientos quincenales (días 15 y último de mes)
--    ORDINARIO  = salario_base / 2 * porcentaje_ahorro de la caja
--    PATRONAL   = igual al ordinario (1:1)
--    VOLUNTARIO = ahorro_voluntario_quincenal (sin aportación patronal)
--    Solo entre fecha_ingreso y fecha_baja (si existe) y hasta el corte.
-- ---------------------------------------------------------------------
INSERT INTO movimientos_ahorro (ahorrador_id, fecha, tipo, monto, concepto)
WITH RECURSIVE quincenas (fecha) AS (
    SELECT CAST(@inicio_historico AS DATE)
    UNION ALL
    SELECT IF(DAY(fecha) = 15, LAST_DAY(fecha), LAST_DAY(fecha) + INTERVAL 15 DAY)
    FROM quincenas
    WHERE IF(DAY(fecha) = 15, LAST_DAY(fecha), LAST_DAY(fecha) + INTERVAL 15 DAY) <= @fecha_corte
),
tipos (tipo) AS (
    SELECT 'ORDINARIO' UNION ALL SELECT 'PATRONAL' UNION ALL SELECT 'VOLUNTARIO'
),
calculo AS (
    SELECT a.id AS ahorrador_id,
           q.fecha,
           t.tipo,
           CASE t.tipo
               WHEN 'VOLUNTARIO' THEN a.ahorro_voluntario_quincenal
               ELSE ROUND(a.salario_base / 2 * c.porcentaje_ahorro, 2)
           END AS monto
    FROM quincenas q
    JOIN ahorradores a
      ON q.fecha >= a.fecha_ingreso
     AND (a.fecha_baja IS NULL OR q.fecha <= a.fecha_baja)
    JOIN cajas_ahorro c ON c.id = a.caja_ahorro_id
    CROSS JOIN tipos t
)
SELECT ahorrador_id, fecha, tipo, monto,
       CONCAT('Descuento quincenal ', LOWER(tipo), ' ', DATE_FORMAT(fecha, '%Y-%m-%d'))
FROM calculo
WHERE monto > 0
ORDER BY fecha, ahorrador_id, tipo;

-- ---------------------------------------------------------------------
-- 3. Préstamos
--    El ciclo se liquida cada año (paso 6b): los préstamos de 2026 se
--    validan contra el saldo acumulado SOLO desde el 1/ene/2026.
--    Tope = 80% del saldo a la fecha de solicitud. Los montos se eligieron
--    para cumplir tanto con saldo total (incl. PATRONAL) como con el criterio
--    estricto ordinario + voluntario:
--    1 Ana     2025-06-30  10,000  (tope 80% > monto)                 LIQUIDADO
--    2 Sofía   2025-05-15   5,000  (tope 80% > monto)                 LIQUIDADO
--    3 Carlos  2026-05-15   6,000  (tope 80%: 12,672 / 6,336 estricto) ACTIVO
--    4 Lucía   2026-03-15   6,000  (tope 80%:  6,000 / 3,600 estricto) ACTIVO, requiere aval (Carlos)
--    5 Jorge   2026-02-15   5,000  (tope 80%:  8,400 / 5,400 estricto) ACTIVO
--    El de Lucía excede el tope en ambos criterios -> requiere_aval = TRUE.
--    estatus, saldo_insoluto y fecha_liquidacion se recalculan en el paso 5.
-- ---------------------------------------------------------------------
INSERT INTO prestamos
    (id, ahorrador_id, fecha_otorgamiento, monto, tasa_interes_mensual, plazo_quincenas,
     estatus, saldo_insoluto, requiere_aval, aval_ahorrador_id) VALUES
    (1, 1, '2025-06-30', 10000.00, 0.0150, 12, 'ACTIVO', 10000.00, FALSE, NULL),
    (2, 5, '2025-05-15',  5000.00, 0.0120, 12, 'ACTIVO',  5000.00, FALSE, NULL),
    (3, 2, '2026-05-15',  6000.00, 0.0150, 18, 'ACTIVO',  6000.00, FALSE, NULL),
    (4, 3, '2026-03-15',  6000.00, 0.0150, 24, 'ACTIVO',  6000.00, TRUE,  2),
    (5, 4, '2026-02-15',  5000.00, 0.0120, 24, 'ACTIVO',  5000.00, FALSE, NULL);

-- ---------------------------------------------------------------------
-- 4. Amortizaciones sobre saldo insoluto (cuota fija quincenal)
--    tasa quincenal = tasa mensual / 2
--    interés  = ROUND(saldo_insoluto * tasa quincenal, 2)
--    capital  = cuota fija - interés (el último pago liquida el saldo exacto)
--    pagada   = vencimiento <= fecha de corte
-- ---------------------------------------------------------------------
INSERT INTO amortizaciones
    (prestamo_id, numero_pago, fecha_vencimiento, saldo_inicial, capital, interes, cuota, saldo_final, pagada)
WITH RECURSIVE condiciones AS (
    SELECT id AS prestamo_id,
           plazo_quincenas AS plazo,
           tasa_interes_mensual / 2 AS tasa_quincenal,
           CAST(ROUND(monto * (tasa_interes_mensual / 2)
                / (1 - POW(1 + tasa_interes_mensual / 2, -plazo_quincenas)), 2) AS DECIMAL(18,2)) AS cuota_fija
    FROM prestamos
),
tabla AS (
    SELECT p.id AS prestamo_id,
           CAST(1 AS SIGNED) AS numero_pago,
           CAST(IF(DAY(p.fecha_otorgamiento) < 15,
                   DATE_FORMAT(p.fecha_otorgamiento, '%Y-%m-15'),
                   IF(p.fecha_otorgamiento < LAST_DAY(p.fecha_otorgamiento),
                      LAST_DAY(p.fecha_otorgamiento),
                      LAST_DAY(p.fecha_otorgamiento) + INTERVAL 15 DAY)) AS DATE) AS fecha_vencimiento,
           CAST(p.monto AS DECIMAL(18,2)) AS saldo_inicial,
           CAST(ROUND(p.monto * c.tasa_quincenal, 2) AS DECIMAL(18,2)) AS interes,
           CAST(IF(c.plazo = 1, p.monto, c.cuota_fija - ROUND(p.monto * c.tasa_quincenal, 2)) AS DECIMAL(18,2)) AS capital
    FROM prestamos p
    JOIN condiciones c ON c.prestamo_id = p.id
    UNION ALL
    SELECT t.prestamo_id,
           t.numero_pago + 1,
           IF(DAY(t.fecha_vencimiento) = 15, LAST_DAY(t.fecha_vencimiento), LAST_DAY(t.fecha_vencimiento) + INTERVAL 15 DAY),
           t.saldo_inicial - t.capital,
           ROUND((t.saldo_inicial - t.capital) * c.tasa_quincenal, 2),
           IF(t.numero_pago + 1 = c.plazo,
              t.saldo_inicial - t.capital,
              c.cuota_fija - ROUND((t.saldo_inicial - t.capital) * c.tasa_quincenal, 2))
    FROM tabla t
    JOIN condiciones c ON c.prestamo_id = t.prestamo_id
    WHERE t.numero_pago < c.plazo
)
SELECT prestamo_id, numero_pago, fecha_vencimiento, saldo_inicial, capital, interes,
       capital + interes          AS cuota,
       saldo_inicial - capital    AS saldo_final,
       fecha_vencimiento <= @fecha_corte AS pagada
FROM tabla
ORDER BY prestamo_id, numero_pago;

-- ---------------------------------------------------------------------
-- 5. Estatus y saldo insoluto de cada préstamo según pagos aplicados
-- ---------------------------------------------------------------------
UPDATE prestamos p
JOIN (
    SELECT prestamo_id,
           SUM(IF(pagada, capital, 0)) AS capital_pagado,
           SUM(NOT pagada)             AS pendientes,
           MAX(fecha_vencimiento)      AS ultimo_vencimiento
    FROM amortizaciones
    GROUP BY prestamo_id
) x ON x.prestamo_id = p.id
SET p.saldo_insoluto    = p.monto - x.capital_pagado,
    p.estatus           = IF(x.pendientes = 0, 'LIQUIDADO', 'ACTIVO'),
    p.fecha_liquidacion = IF(x.pendientes = 0, x.ultimo_vencimiento, NULL);

-- ---------------------------------------------------------------------
-- 6. Cierre del ejercicio 2025: reparto de rendimientos por caja
--    bolsa       = intereses pagados en el año (préstamos de la caja)
--    factor      = Σ(saldo diario × días) / 365
--    rendimiento = factor / Σfactores × bolsa, redondeado a 2 decimales;
--                  el residuo se asigna al último ahorrador (id mayor) de cada caja.
--    Se registra como movimiento RENDIMIENTO al 31/dic.
-- ---------------------------------------------------------------------
INSERT INTO movimientos_ahorro (ahorrador_id, fecha, tipo, monto, concepto)
WITH movimientos_dia AS (
    SELECT ahorrador_id, fecha, SUM(monto) AS monto
    FROM movimientos_ahorro
    WHERE fecha <= @fin_anterior
      AND NOT (tipo = 'RENDIMIENTO' AND fecha >= @ini_anterior)
    GROUP BY ahorrador_id, fecha
),
saldos AS (
    SELECT ahorrador_id,
           fecha,
           SUM(monto) OVER (PARTITION BY ahorrador_id ORDER BY fecha) AS saldo,
           LEAD(fecha) OVER (PARTITION BY ahorrador_id ORDER BY fecha) AS fecha_siguiente
    FROM movimientos_dia
),
factores AS (
    SELECT a.caja_ahorro_id,
           s.ahorrador_id,
           SUM(s.saldo * GREATEST(DATEDIFF(
                   LEAST(COALESCE(s.fecha_siguiente, @fin_anterior + INTERVAL 1 DAY), @fin_anterior + INTERVAL 1 DAY),
                   GREATEST(s.fecha, @ini_anterior)), 0)) / 365 AS factor
    FROM saldos s
    JOIN ahorradores a ON a.id = s.ahorrador_id
    GROUP BY a.caja_ahorro_id, s.ahorrador_id
),
bolsas AS (
    SELECT ah.caja_ahorro_id, SUM(am.interes) AS bolsa
    FROM amortizaciones am
    JOIN prestamos p    ON p.id = am.prestamo_id
    JOIN ahorradores ah ON ah.id = p.ahorrador_id
    WHERE am.pagada AND am.fecha_vencimiento BETWEEN @ini_anterior AND @fin_anterior
    GROUP BY ah.caja_ahorro_id
),
reparto AS (
    SELECT f.caja_ahorro_id,
           f.ahorrador_id,
           b.bolsa,
           ROUND(f.factor / SUM(f.factor) OVER (PARTITION BY f.caja_ahorro_id) * b.bolsa, 2) AS rendimiento,
           ROW_NUMBER() OVER (PARTITION BY f.caja_ahorro_id ORDER BY f.ahorrador_id DESC) AS orden_desc
    FROM factores f
    JOIN bolsas b ON b.caja_ahorro_id = f.caja_ahorro_id
    WHERE f.factor > 0
),
ajustado AS (
    SELECT ahorrador_id,
           CASE WHEN orden_desc = 1
                THEN bolsa - (SUM(rendimiento) OVER (PARTITION BY caja_ahorro_id) - rendimiento)
                ELSE rendimiento
           END AS monto
    FROM reparto
)
SELECT ahorrador_id, @fin_anterior, 'RENDIMIENTO', monto,
       CONCAT('Rendimiento ejercicio ', YEAR(@fin_anterior))
FROM ajustado
WHERE monto > 0
ORDER BY ahorrador_id;

-- ---------------------------------------------------------------------
-- 6b. Liquidación del ciclo 2025: RETIRO por el saldo total (ahorro +
--     patronal + rendimientos) de cada ahorrador activo al 31/dic/2025.
--     Deja el saldo en 0. Los préstamos NO se descuentan del retiro.
-- ---------------------------------------------------------------------
INSERT INTO movimientos_ahorro (ahorrador_id, fecha, tipo, monto, concepto)
SELECT a.id, @fin_anterior, 'RETIRO', -SUM(m.monto),
       CONCAT('Liquidación del ciclo ', YEAR(@fin_anterior))
FROM ahorradores a
JOIN movimientos_ahorro m ON m.ahorrador_id = a.id AND m.fecha <= @fin_anterior
WHERE a.fecha_ingreso <= @fin_anterior
  AND (a.fecha_baja IS NULL OR a.fecha_baja > @fin_anterior)
GROUP BY a.id
HAVING SUM(m.monto) > 0
ORDER BY a.id;

-- ---------------------------------------------------------------------
-- 7. Baja laboral (2026): RETIRO por el saldo acumulado desde el 1/ene/2026
--    (el ciclo 2025 ya se liquidó en 6b, por lo que no se duplica).
-- ---------------------------------------------------------------------
INSERT INTO movimientos_ahorro (ahorrador_id, fecha, tipo, monto, concepto)
SELECT a.id, a.fecha_baja, 'RETIRO', -SUM(m.monto), 'Liquidación por baja laboral'
FROM ahorradores a
JOIN movimientos_ahorro m ON m.ahorrador_id = a.id AND m.fecha <= a.fecha_baja
WHERE a.fecha_baja IS NOT NULL
GROUP BY a.id, a.fecha_baja
HAVING SUM(m.monto) > 0;

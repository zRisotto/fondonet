-- Consulta 5: Listado de ahorradores activos en un año X, desglosando balance de movimientos
--             (total aportado, total amortizado) y utilidad neta asignada por rendimientos.
WITH params AS (
    SELECT 2025 AS anio_fiscal -- <-- Año X editable en un único lugar (p. ej. 2025 o 2026)
),
mov_anual AS (
    SELECT
        m.ahorrador_id,
        SUM(CASE WHEN m.tipo = 'ORDINARIO'   THEN m.monto ELSE 0 END) AS ahorro_ordinario,
        SUM(CASE WHEN m.tipo = 'PATRONAL'    THEN m.monto ELSE 0 END) AS aportacion_patronal,
        SUM(CASE WHEN m.tipo = 'VOLUNTARIO'  THEN m.monto ELSE 0 END) AS ahorro_voluntario,
        SUM(CASE WHEN m.tipo IN ('ORDINARIO', 'PATRONAL', 'VOLUNTARIO') THEN m.monto ELSE 0 END) AS total_aportado,
        SUM(CASE WHEN m.tipo = 'RENDIMIENTO' THEN m.monto ELSE 0 END) AS utilidad_neta_rendimientos,
        SUM(CASE WHEN m.tipo = 'RETIRO'      THEN -m.monto ELSE 0 END) AS total_retirado,
        SUM(m.monto) AS balance_neto_ciclo
    FROM mariadb.fondonet.movimientos_ahorro m
    JOIN params prm ON YEAR(m.fecha) = prm.anio_fiscal
    GROUP BY m.ahorrador_id
),
amort_anual AS (
    SELECT
        p.ahorrador_id,
        SUM(CASE WHEN am.pagada <> 0 THEN am.capital ELSE 0 END) AS capital_amortizado,
        SUM(CASE WHEN am.pagada <> 0 THEN am.interes ELSE 0 END) AS interes_pagado,
        SUM(CASE WHEN am.pagada <> 0 THEN am.cuota   ELSE 0 END) AS total_amortizado
    FROM mariadb.fondonet.amortizaciones am
    JOIN mariadb.fondonet.prestamos p ON p.id = am.prestamo_id
    JOIN params prm ON YEAR(am.fecha_vencimiento) = prm.anio_fiscal
    GROUP BY p.ahorrador_id
)
SELECT
    prm.anio_fiscal                                AS anio_fiscal,
    a.id                                           AS ahorrador_id,
    a.clave_empleado,
    a.nombre                                       AS ahorrador,
    a.estatus                                      AS estatus_actual,
    c.nombre                                       AS caja_ahorro,
    COALESCE(m.ahorro_ordinario, 0)                AS ahorro_ordinario,
    COALESCE(m.aportacion_patronal, 0)             AS aportacion_patronal,
    COALESCE(m.ahorro_voluntario, 0)               AS ahorro_voluntario,
    COALESCE(m.total_aportado, 0)                  AS total_aportado,
    COALESCE(am.capital_amortizado, 0)             AS capital_amortizado,
    COALESCE(am.interes_pagado, 0)                 AS interes_pagado,
    COALESCE(am.total_amortizado, 0)               AS total_amortizado,
    COALESCE(m.utilidad_neta_rendimientos, 0)      AS utilidad_neta_rendimientos,
    COALESCE(m.total_retirado, 0)                  AS total_retirado,
    COALESCE(m.balance_neto_ciclo, 0)              AS balance_neto_ciclo
FROM params prm
JOIN mariadb.fondonet.ahorradores a  ON YEAR(a.fecha_ingreso) <= prm.anio_fiscal
                                    AND (a.fecha_baja IS NULL OR YEAR(a.fecha_baja) >= prm.anio_fiscal)
JOIN mariadb.fondonet.cajas_ahorro c ON c.id = a.caja_ahorro_id
LEFT JOIN mov_anual m                ON m.ahorrador_id = a.id
LEFT JOIN amort_anual am             ON am.ahorrador_id = a.id
ORDER BY a.id;

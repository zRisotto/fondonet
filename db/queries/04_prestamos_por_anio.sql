-- Consulta 4: Listado de ahorradores con préstamos registrados en un año fiscal X.
WITH params AS (
    SELECT 2026 AS anio_fiscal -- <-- Año X editable en un único lugar (p. ej. 2025 o 2026)
)
SELECT
    YEAR(p.fecha_otorgamiento) AS anio_fiscal,
    p.id                       AS prestamo_id,
    a.id                       AS ahorrador_id,
    a.clave_empleado,
    a.nombre                   AS ahorrador,
    c.nombre                   AS caja_ahorro,
    p.fecha_otorgamiento,
    p.monto,
    p.tasa_interes_mensual,
    p.plazo_quincenas,
    p.estatus                  AS estatus_prestamo,
    p.saldo_insoluto,
    p.requiere_aval,
    av.nombre                  AS aval,
    p.fecha_liquidacion
FROM params prm
JOIN mariadb.fondonet.prestamos p         ON YEAR(p.fecha_otorgamiento) = prm.anio_fiscal
JOIN mariadb.fondonet.ahorradores a       ON a.id = p.ahorrador_id
JOIN mariadb.fondonet.cajas_ahorro c      ON c.id = a.caja_ahorro_id
LEFT JOIN mariadb.fondonet.ahorradores av ON av.id = p.aval_ahorrador_id
ORDER BY p.fecha_otorgamiento, p.id;

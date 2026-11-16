-- Consulta 2: Listado de ahorradores dados de baja en el ciclo.
SELECT
    a.id               AS ahorrador_id,
    a.clave_empleado,
    a.nombre           AS ahorrador,
    a.estatus,
    a.fecha_ingreso,
    a.fecha_baja,
    YEAR(a.fecha_baja) AS anio_baja,
    c.nombre           AS caja_ahorro,
    e.nombre           AS empresa,
    COALESCE(SUM(CASE WHEN m.tipo = 'RETIRO' AND m.fecha = a.fecha_baja THEN -m.monto ELSE 0 END), 0) AS monto_liquidado_baja
FROM mariadb.fondonet.ahorradores a
JOIN mariadb.fondonet.cajas_ahorro c            ON c.id = a.caja_ahorro_id
JOIN mariadb.fondonet.empresas e                ON e.id = c.empresa_id
LEFT JOIN mariadb.fondonet.movimientos_ahorro m ON m.ahorrador_id = a.id
WHERE a.estatus = 'INACTIVO'
   OR a.fecha_baja IS NOT NULL
GROUP BY a.id, a.clave_empleado, a.nombre, a.estatus, a.fecha_ingreso, a.fecha_baja, c.nombre, e.nombre
ORDER BY a.fecha_baja DESC, a.id;

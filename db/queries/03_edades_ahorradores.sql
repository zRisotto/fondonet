-- Consulta 3: Listado de ahorradores con su edad calculada a partir de FechaNacimiento.
SELECT
    a.id               AS ahorrador_id,
    a.clave_empleado,
    a.nombre           AS ahorrador,
    a.fecha_nacimiento,
    DATE_DIFF('year', a.fecha_nacimiento, CURRENT_DATE) AS edad_anios,
    a.estatus,
    c.nombre           AS caja_ahorro,
    e.nombre           AS empresa
FROM mariadb.fondonet.ahorradores a
JOIN mariadb.fondonet.cajas_ahorro c ON c.id = a.caja_ahorro_id
JOIN mariadb.fondonet.empresas e     ON e.id = c.empresa_id
ORDER BY edad_anios DESC, a.id;

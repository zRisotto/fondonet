-- Consulta 1: Listado global de ahorradores, estatus actual y caja de ahorro/empresa a la que pertenecen.
SELECT
    a.id                          AS ahorrador_id,
    a.clave_empleado,
    a.nombre                      AS ahorrador,
    a.curp,
    a.estatus,
    a.salario_base,
    a.ahorro_voluntario_quincenal,
    a.fecha_ingreso,
    a.fecha_baja,
    c.id                          AS caja_ahorro_id,
    c.nombre                      AS caja_ahorro,
    e.id                          AS empresa_id,
    e.nombre                      AS empresa,
    e.rfc                         AS rfc_empresa
FROM mariadb.fondonet.ahorradores a
JOIN mariadb.fondonet.cajas_ahorro c ON c.id = a.caja_ahorro_id
JOIN mariadb.fondonet.empresas e     ON e.id = c.empresa_id
ORDER BY e.id, c.id, a.id;

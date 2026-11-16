-- =====================================================================
-- FondoNet · Usuarios (autenticación JWT) y control de cierres anuales
-- Formato de password_hash: PBKDF2-HMAC-SHA256 -> iteraciones$saltBase64$hashBase64
-- Contraseñas demo de semilla:
--   admin                           -> Admin123!
--   ana, carlos, lucia, jorge, sofia, ricardo -> Ahorrador123!
-- =====================================================================
SET NAMES utf8mb4;
USE fondonet;

CREATE TABLE IF NOT EXISTS usuarios (
    id              INT AUTO_INCREMENT PRIMARY KEY,
    nombre_usuario  VARCHAR(50)  NOT NULL,
    password_hash   VARCHAR(255) NOT NULL,
    rol             VARCHAR(20)  NOT NULL,
    ahorrador_id    INT          NULL,
    activo          BOOLEAN      NOT NULL DEFAULT TRUE,
    CONSTRAINT uq_usuarios_nombre UNIQUE (nombre_usuario),
    CONSTRAINT fk_usuarios_ahorrador FOREIGN KEY (ahorrador_id) REFERENCES ahorradores (id),
    CONSTRAINT ck_usuarios_rol CHECK (rol IN ('ADMIN', 'AHORRADOR')),
    CONSTRAINT ck_usuarios_vinculo CHECK ((rol = 'ADMIN' AND ahorrador_id IS NULL)
                                       OR (rol = 'AHORRADOR' AND ahorrador_id IS NOT NULL))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

CREATE TABLE IF NOT EXISTS cierres_anuales (
    id                 INT AUTO_INCREMENT PRIMARY KEY,
    caja_ahorro_id     INT           NOT NULL,
    anio               INT           NOT NULL,
    fecha_ejecucion    DATETIME      NOT NULL,
    bolsa              DECIMAL(18,2) NOT NULL,
    total_rendimientos DECIMAL(18,2) NOT NULL,
    total_retiros      DECIMAL(18,2) NOT NULL,
    CONSTRAINT uq_cierres_caja_anio UNIQUE (caja_ahorro_id, anio),
    CONSTRAINT fk_cierres_caja FOREIGN KEY (caja_ahorro_id) REFERENCES cajas_ahorro (id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;

INSERT IGNORE INTO usuarios (id, nombre_usuario, password_hash, rol, ahorrador_id, activo) VALUES
    (1, 'admin',   '100000$Zm9uZG9uZXQtc2FsdC0wMQ==$62NUvSc9j6QRmAHhn4lG2XXzu8AZppsPC5C/Efi63I8=', 'ADMIN',     NULL, TRUE),
    (2, 'ana',     '100000$Zm9uZG9uZXQtc2FsdC0wMg==$sue0I7BYdu4BU1GjsJM6Q1NmMkh7dBx9FMiWvECvaGs=', 'AHORRADOR', 1,    TRUE),
    (3, 'carlos',  '100000$Zm9uZG9uZXQtc2FsdC0wMg==$sue0I7BYdu4BU1GjsJM6Q1NmMkh7dBx9FMiWvECvaGs=', 'AHORRADOR', 2,    TRUE),
    (4, 'lucia',   '100000$Zm9uZG9uZXQtc2FsdC0wMg==$sue0I7BYdu4BU1GjsJM6Q1NmMkh7dBx9FMiWvECvaGs=', 'AHORRADOR', 3,    TRUE),
    (5, 'jorge',   '100000$Zm9uZG9uZXQtc2FsdC0wMg==$sue0I7BYdu4BU1GjsJM6Q1NmMkh7dBx9FMiWvECvaGs=', 'AHORRADOR', 4,    TRUE),
    (6, 'sofia',   '100000$Zm9uZG9uZXQtc2FsdC0wMg==$sue0I7BYdu4BU1GjsJM6Q1NmMkh7dBx9FMiWvECvaGs=', 'AHORRADOR', 5,    TRUE),
    (7, 'ricardo', '100000$Zm9uZG9uZXQtc2FsdC0wMg==$sue0I7BYdu4BU1GjsJM6Q1NmMkh7dBx9FMiWvECvaGs=', 'AHORRADOR', 6,    FALSE);

-- El ejercicio 2025 ya viene cerrado y liquidado en 02_seed.sql.
INSERT IGNORE INTO cierres_anuales (id, caja_ahorro_id, anio, fecha_ejecucion, bolsa, total_rendimientos, total_retiros) VALUES
    (1, 1, 2025, '2025-12-31 23:59:59', 494.18, 494.18, 116294.18),
    (2, 2, 2025, '2025-12-31 23:59:59', 197.13, 197.13, 160997.13);

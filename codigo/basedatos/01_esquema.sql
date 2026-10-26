-- As bajo la manga - Esquema de la base de datos (Supabase / PostgreSQL)
-- Se ejecuta una sola vez en el editor SQL de Supabase. Corresponde al Anexo B.3 del análisis.
-- Una base creada antes del 22/10 (un solo barbero) se actualiza con 04_peluqueros.sql.

-- btree_gist permite combinar "mismo peluquero" (=) con "horarios que se cruzan" (&&) en la restricción anti-choques.
CREATE EXTENSION IF NOT EXISTS btree_gist;

CREATE TABLE servicios (
    id            SERIAL PRIMARY KEY,
    nombre        VARCHAR(60)   NOT NULL UNIQUE,
    descripcion   VARCHAR(200)  NOT NULL DEFAULT '',
    duracion_min  INTEGER       NOT NULL CHECK (duracion_min BETWEEN 30 AND 180 AND duracion_min % 30 = 0),  -- RN-04
    precio        NUMERIC(8,2)  NOT NULL CHECK (precio > 0),
    imagen        VARCHAR(200)  NOT NULL DEFAULT '',
    activo        BOOLEAN       NOT NULL DEFAULT TRUE                                                     -- RN-16
);

CREATE TABLE clientes (
    id              SERIAL PRIMARY KEY,
    nombre          VARCHAR(50)  NOT NULL,
    apellido        VARCHAR(50)  NOT NULL,
    telefono        CHAR(8)      NOT NULL UNIQUE,   -- sin +591 (RN-11)
    correo          VARCHAR(100),
    fecha_registro  TIMESTAMP    NOT NULL           -- hora de Bolivia, puesta desde C# (RN-10)
);

CREATE TABLE usuarios (
    id                 SERIAL PRIMARY KEY,
    nombre             VARCHAR(50)  NOT NULL,
    apellido           VARCHAR(50)  NOT NULL,
    nombre_usuario     VARCHAR(20)  NOT NULL UNIQUE,
    contrasena_hash    VARCHAR(200) NOT NULL,       -- "sal:hash" PBKDF2 (RNF-02)
    rol                VARCHAR(20)  NOT NULL CHECK (rol IN ('Administrador', 'Recepcionista')),
    activo             BOOLEAN      NOT NULL DEFAULT TRUE,
    intentos_fallidos  INTEGER      NOT NULL DEFAULT 0,
    bloqueado_hasta    TIMESTAMP                     -- RN-18
);

-- Peluqueros (RF-24, RF-25). No se eliminan: se desactivan (RN-16).
CREATE TABLE peluqueros (
    id            SERIAL PRIMARY KEY,
    nombre        VARCHAR(50)  NOT NULL,
    apellido      VARCHAR(50)  NOT NULL,
    especialidad  VARCHAR(60)  NOT NULL DEFAULT '',
    activo        BOOLEAN      NOT NULL DEFAULT TRUE
);

CREATE TABLE reservas (
    id              SERIAL PRIMARY KEY,
    cliente_id      INTEGER       NOT NULL REFERENCES clientes(id),
    servicio_id     INTEGER       NOT NULL REFERENCES servicios(id),
    peluquero_id    INTEGER       NOT NULL REFERENCES peluqueros(id),   -- RN-21
    fecha           DATE          NOT NULL,
    hora_inicio     TIME          NOT NULL,
    hora_fin        TIME          NOT NULL CHECK (hora_fin > hora_inicio),
    precio          NUMERIC(8,2)  NOT NULL,                               -- copiado del servicio (RN-15)
    estado          VARCHAR(12)   NOT NULL DEFAULT 'Confirmada'
                    CHECK (estado IN ('Confirmada', 'Atendida', 'Cancelada', 'NoAsistio')),
    notas           VARCHAR(200),
    origen          VARCHAR(6)    NOT NULL CHECK (origen IN ('Web', 'Manual')),
    correo_enviado  BOOLEAN,                                              -- vacío = sin correo (RN-20)
    creado_en       TIMESTAMP     NOT NULL,
    creado_por      INTEGER       REFERENCES usuarios(id),
    -- Anti-choques (RN-06, RN-21, RNF-07): dos reservas Confirmadas o Atendidas DEL MISMO PELUQUERO no se superponen.
    -- Dos peluqueros distintos sí pueden atender a la misma hora.
    -- Si se intenta, PostgreSQL responde con el código 23P01 y C# lanza HorarioNoDisponibleException.
    CONSTRAINT reservas_sin_choque EXCLUDE USING gist (
        peluquero_id WITH =,
        tsrange(fecha + hora_inicio, fecha + hora_fin, '[)') WITH &&
    ) WHERE (estado IN ('Confirmada', 'Atendida'))
);

CREATE INDEX reservas_fecha ON reservas (fecha);

-- Seguridad por filas (RNF-04): sin reglas, nadie lee las tablas desde la API pública de Supabase.
-- El sistema entra con su propia cadena de conexión (Session pooler) y no se ve afectado.
ALTER TABLE servicios ENABLE ROW LEVEL SECURITY;
ALTER TABLE clientes  ENABLE ROW LEVEL SECURITY;
ALTER TABLE usuarios  ENABLE ROW LEVEL SECURITY;
ALTER TABLE peluqueros ENABLE ROW LEVEL SECURITY;
ALTER TABLE reservas  ENABLE ROW LEVEL SECURITY;

-- As bajo la manga - Actualización 22/10: varios peluqueros (RF-24, RF-25, RN-21).
-- Para una base creada con la versión anterior (un solo barbero). Se ejecuta una vez en el editor SQL de Supabase.
-- Una base nueva no lo necesita: 01_esquema.sql ya trae estos cambios.

-- btree_gist: permite "mismo peluquero" (=) dentro de la restricción anti-choques (Supabase la guarda en el esquema extensions).
CREATE EXTENSION IF NOT EXISTS btree_gist WITH SCHEMA extensions;

CREATE TABLE IF NOT EXISTS peluqueros (
    id            SERIAL PRIMARY KEY,
    nombre        VARCHAR(50)  NOT NULL,
    apellido      VARCHAR(50)  NOT NULL,
    especialidad  VARCHAR(60)  NOT NULL DEFAULT '',
    activo        BOOLEAN      NOT NULL DEFAULT TRUE
);
ALTER TABLE peluqueros ENABLE ROW LEVEL SECURITY;

-- El barbero que ya trabajaba: se le asignan todas las reservas existentes. Su nombre se cambia en Panel → Peluqueros.
INSERT INTO peluqueros (nombre, apellido, especialidad)
SELECT 'Barbero', 'Principal', 'Cortes y barba'
WHERE NOT EXISTS (SELECT 1 FROM peluqueros);

ALTER TABLE reservas ADD COLUMN IF NOT EXISTS peluquero_id INTEGER REFERENCES peluqueros(id);
UPDATE reservas SET peluquero_id = (SELECT MIN(id) FROM peluqueros) WHERE peluquero_id IS NULL;
ALTER TABLE reservas ALTER COLUMN peluquero_id SET NOT NULL;

-- Valor por defecto TEMPORAL: mientras el hosting siga con la versión anterior (que no envía peluquero_id),
-- sus reservas quedan con el primer peluquero. Después de publicar la versión nueva se puede quitar con:
--   ALTER TABLE reservas ALTER COLUMN peluquero_id DROP DEFAULT;
DO $$
BEGIN
    EXECUTE format('ALTER TABLE reservas ALTER COLUMN peluquero_id SET DEFAULT %s', (SELECT MIN(id) FROM peluqueros));
END $$;

-- Anti-choques por peluquero (RN-06, RN-21): el mismo peluquero no puede tener dos citas que se crucen.
ALTER TABLE reservas DROP CONSTRAINT IF EXISTS reservas_sin_choque;
ALTER TABLE reservas ADD CONSTRAINT reservas_sin_choque EXCLUDE USING gist (
    peluquero_id WITH =,
    tsrange(fecha + hora_inicio, fecha + hora_fin, '[)') WITH &&
) WHERE (estado IN ('Confirmada', 'Atendida'));

-- Permisos del usuario de la aplicación (ver 03_rol_aplicacion.sql).
GRANT SELECT, INSERT, UPDATE, DELETE ON peluqueros TO barberia_app;
GRANT USAGE, SELECT ON SEQUENCE peluqueros_id_seq TO barberia_app;
DROP POLICY IF EXISTS app_peluqueros ON peluqueros;
CREATE POLICY app_peluqueros ON peluqueros FOR ALL TO barberia_app USING (true) WITH CHECK (true);

-- As bajo la manga - Datos iniciales (servicios y peluquero de ejemplo).
-- El Administrador inicial NO se crea aquí: lo crea la aplicación con su contraseña cifrada (PBKDF2).
INSERT INTO servicios (nombre, descripcion, duracion_min, precio, imagen) VALUES
    ('Corte de cabello',   'Corte clásico o moderno',              30,  35.00, 'corte.jpg'),
    ('Perfilado de barba', 'Perfilado y arreglo de barba',         30,  25.00, 'barba.jpg'),
    ('Corte + barba',      'Corte de cabello y perfilado de barba', 60,  55.00, 'corte-barba.jpg'),
    ('Diseño de cejas',    'Perfilado de cejas',                   30,  15.00, 'cejas.jpg'),
    ('Tinte',              'Coloración completa',                  90, 120.00, 'tinte.jpg');

-- Al menos un peluquero activo (el barbero actual). Los demás se agregan desde Panel → Peluqueros.
INSERT INTO peluqueros (nombre, apellido, especialidad) VALUES ('Barbero', 'Principal', 'Cortes y barba');

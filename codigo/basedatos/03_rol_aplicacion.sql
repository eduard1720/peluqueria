-- As bajo la manga - Usuario de base de datos propio de la aplicación (mínimo privilegio).
-- Se ejecuta una vez en el editor SQL de Supabase, cambiando <CONTRASENA> por una contraseña larga.
-- La aplicación se conecta por el Session pooler con el usuario "barberia_app.<id del proyecto>".
-- La contraseña NO se guarda en GitHub: va en appsettings.Local.json (ignorado por git) o en el hosting.
CREATE ROLE barberia_app LOGIN PASSWORD '<CONTRASENA>';
GRANT USAGE ON SCHEMA public TO barberia_app;
GRANT SELECT, INSERT, UPDATE, DELETE ON servicios, clientes, usuarios, reservas, peluqueros TO barberia_app;
GRANT USAGE, SELECT ON ALL SEQUENCES IN SCHEMA public TO barberia_app;

-- Con RLS activo, solo este usuario puede leer y escribir; la API pública de Supabase sigue sin acceso.
CREATE POLICY app_servicios ON servicios FOR ALL TO barberia_app USING (true) WITH CHECK (true);
CREATE POLICY app_clientes  ON clientes  FOR ALL TO barberia_app USING (true) WITH CHECK (true);
CREATE POLICY app_usuarios  ON usuarios  FOR ALL TO barberia_app USING (true) WITH CHECK (true);
CREATE POLICY app_reservas  ON reservas  FOR ALL TO barberia_app USING (true) WITH CHECK (true);
CREATE POLICY app_peluqueros ON peluqueros FOR ALL TO barberia_app USING (true) WITH CHECK (true);

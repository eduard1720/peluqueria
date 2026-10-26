using Logica.Datos;
using Logica.Gestores;
using Logica.Integraciones;
using Logica.Utilidades;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);
// appsettings.Local.json: cadena de Supabase y claves para ejecutar el proyecto descargado. No se sube a GitHub.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
// En el hosting la cadena llega como variable de entorno (ConnectionStrings__Supabase) y tiene prioridad.
builder.Configuration.AddEnvironmentVariables();

// MVC + token antiforgery en todos los formularios POST (RNF-04).
builder.Services.AddControllersWithViews(o => o.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

// Inicio de sesión con cookie protegida (RNF-03).
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(o =>
    {
        o.LoginPath = "/Cuenta/Login";
        o.AccessDeniedPath = "/Cuenta/Login";
        o.ExpireTimeSpan = TimeSpan.FromHours(8);
        o.Cookie.HttpOnly = true;
        // En el hosting todo va por HTTPS, así que la cookie sale segura; en local (http://localhost) también funciona.
        o.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    });

// Clases de la capa lógica. Las cadenas y claves vienen de la configuración (user-secrets o el hosting),
// nunca del código (RNF-05). Se crean recién cuando un controlador las pide.
string carpetaDatos = Path.Combine(builder.Environment.ContentRootPath, "App_Data");
builder.Services.AddSingleton(new RegistroErrores(carpetaDatos));
builder.Services.AddSingleton(_ => new ConexionBD(builder.Configuration.GetConnectionString("Supabase") ?? ""));
builder.Services.AddSingleton(sp => new EnviadorCorreo(
    builder.Configuration["Brevo:ClaveApi"] ?? "",
    builder.Configuration["Brevo:CorreoRemitente"] ?? "",
    builder.Configuration["Brevo:NombreRemitente"] ?? "As bajo la manga",
    builder.Configuration["Barberia:Telefono"] ?? "",
    sp.GetRequiredService<RegistroErrores>()));
builder.Services.AddSingleton<GestorReservas>();
builder.Services.AddSingleton<GestorClientes>();
builder.Services.AddSingleton<GestorServicios>();
builder.Services.AddSingleton<GestorPeluqueros>();
builder.Services.AddSingleton<GestorUsuarios>();
builder.Services.AddSingleton<GeneradorReportes>();

var app = builder.Build();

// Primer Administrador: se crea solo si la tabla usuarios está vacía y hay datos en la configuración
// (AdministradorInicial:Usuario y AdministradorInicial:Contrasena, en appsettings.Local.json o el hosting).
string usuarioInicial = builder.Configuration["AdministradorInicial:Usuario"] ?? "";
string contrasenaInicial = builder.Configuration["AdministradorInicial:Contrasena"] ?? "";
if (usuarioInicial != "" && contrasenaInicial != "")
{
    try
    {
        app.Services.GetRequiredService<GestorUsuarios>().CrearAdministradorInicialSiNoHay(usuarioInicial, contrasenaInicial);
    }
    catch (Exception ex)
    {
        app.Services.GetRequiredService<RegistroErrores>().Registrar("Program.AdministradorInicial", ex);
    }
}

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();   // solo HTTPS (RNF-06)
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.MapStaticAssets();
app.MapControllerRoute(name: "default", pattern: "{controller=Reservar}/{action=Index}/{id?}").WithStaticAssets();
app.Run();

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ProyectoCine.Data;

var builder = WebApplication.CreateBuilder(args);

// Activa MVC (controladores + vistas)
builder.Services.AddControllersWithViews();

// Conexión a SQL Server
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configura Identity (usuarios, login, roles)
builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.SignIn.RequireConfirmedAccount = false;
    options.Password.RequireDigit = false;
    options.Password.RequiredLength = 6;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
})
.AddRoles<IdentityRole>()
.AddEntityFrameworkStores<ApplicationDbContext>();

// Habilitar sesiones
builder.Services.AddSession();
builder.Services.AddHttpContextAccessor();

var app = builder.Build();

// 👉 1. Garantiza que la BD y tablas existan, y crea el usuario administrador por defecto
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();
    context.Database.EnsureCreated();

    var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
    var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

    // Asegurar que exista el rol Admin
    if (!await roleManager.RoleExistsAsync("Admin"))
    {
        await roleManager.CreateAsync(new IdentityRole("Admin"));
    }

    // Crear el usuario de prueba si no existe
    string emailPrueba = "admin@cine.com";
    var user = await userManager.FindByEmailAsync(emailPrueba);
    if (user == null)
    {
        user = new IdentityUser
        {
            UserName = emailPrueba,
            Email = emailPrueba,
            EmailConfirmed = true
        };

        var resultado = await userManager.CreateAsync(user, "Admin123*");
        if (resultado.Succeeded)
        {
            await userManager.AddToRoleAsync(user, "Admin");
        }
    }
}

// Manejo de errores y seguridad en producción
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Middleware principal
app.UseStaticFiles();       // Archivos estáticos (CSS, JS, imágenes)
app.UseHttpsRedirection();  // Forzar HTTPS
app.UseRouting();           // Sistema de rutas

app.UseSession();           // Activar sesiones
app.UseAuthentication();    // Login
app.UseAuthorization();     // Permisos

// Ruta por defecto: Inicio
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
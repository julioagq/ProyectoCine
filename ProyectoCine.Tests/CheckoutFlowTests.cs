using System.Net;
using System.Net.Http;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using ProyectoCine.Controllers;
using ProyectoCine.Data;
using ProyectoCine.Models;

namespace ProyectoCine.Tests;

public class CheckoutFlowTests
{
    [Fact]
    public async Task ProcesarPago_Aprobado_CreaOrden_VaciaCarrito_MarcaReservaPagada()
    {
        var context = CrearContexto();
        var user = new IdentityUser { Id = "u1", UserName = "u1@test.com", Email = "u1@test.com" };

        await context.Funciones.AddAsync(new Funcion
        {
            Id = 1,
            PeliculaId = 10,
            SalaId = 5,
            FechaHora = DateTime.Now.AddDays(1),
            Precio = 2500,
            Pelicula = new Pelicula { Id = 10, Titulo = "Demo", Genero = "Acción", Clasificacion = "TE", DuracionMinutos = 100, CineId = 1 },
            Sala = new Sala { Id = 5, Nombre = "Sala 1", Capacidad = 30, CineId = 1 }
        });

        await context.CarritoItems.AddAsync(new CarritoItem
        {
            UserId = user.Id,
            PeliculaId = 10,
            FuncionId = 1,
            TituloPelicula = "Demo",
            ButacaCodigo = "A1",
            Precio = 2500,
            Cantidad = 1
        });

        await context.ReservasButacas.AddAsync(new ReservaButaca
        {
            FuncionId = 1,
            ButacaCodigo = "A1",
            UserId = user.Id,
            Estado = "Pendiente",
            CreadaEn = DateTime.UtcNow,
            ExpiraEn = DateTime.UtcNow.AddMinutes(10)
        });

        await context.SaveChangesAsync();

        var controller = CrearController(context, user, HttpStatusCode.OK, "{\"aprobado\":true,\"transaccionId\":\"TX-1\"}");

        var resultado = await controller.ProcesarPago("4242424242424242", "Usuario Demo");

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Confirmacion", redirect.ActionName);

        Assert.Single(context.Ordenes);
        Assert.Empty(context.CarritoItems);

        var reserva = await context.ReservasButacas.SingleAsync();
        Assert.Equal("Pagada", reserva.Estado);
    }

    [Fact]
    public async Task ProcesarPago_Rechazado_LiberaReserva_YLimpiaCarrito()
    {
        var context = CrearContexto();
        var user = new IdentityUser { Id = "u2", UserName = "u2@test.com", Email = "u2@test.com" };

        await context.CarritoItems.AddAsync(new CarritoItem
        {
            UserId = user.Id,
            PeliculaId = 20,
            FuncionId = 2,
            TituloPelicula = "Demo 2",
            ButacaCodigo = "B2",
            Precio = 3000,
            Cantidad = 1
        });

        await context.ReservasButacas.AddAsync(new ReservaButaca
        {
            FuncionId = 2,
            ButacaCodigo = "B2",
            UserId = user.Id,
            Estado = "Pendiente",
            CreadaEn = DateTime.UtcNow,
            ExpiraEn = DateTime.UtcNow.AddMinutes(10)
        });

        await context.SaveChangesAsync();

        var controller = CrearController(context, user, HttpStatusCode.PaymentRequired, "{\"aprobado\":false}");

        var resultado = await controller.ProcesarPago("4000000000000000", "Usuario Demo");

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);

        Assert.Empty(context.Ordenes);
        Assert.Empty(context.CarritoItems);

        var reserva = await context.ReservasButacas.SingleAsync();
        Assert.Equal("Liberada", reserva.Estado);
    }

    [Fact]
    public async Task ProcesarPago_SinUsuario_RedireccionaLogin()
    {
        var context = CrearContexto();
        var userManager = MockUserManager();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync((IdentityUser?)null);

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new StubHttpMessageHandler(HttpStatusCode.OK, "{\"aprobado\":true,\"transaccionId\":\"TX-OK\"}")));

        var controller = new CheckoutController(context, userManager.Object, httpClientFactory.Object, NullLogger<CheckoutController>.Instance, null);
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        controller.TempData = new TempDataDictionary(controller.HttpContext, Mock.Of<ITempDataProvider>());

        var resultado = await controller.ProcesarPago("4242424242424242", "Test");

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Login", redirect.ActionName);
        Assert.Equal("Home", redirect.ControllerName);
    }

    [Fact]
    public async Task ProcesarPago_SinItems_RedireccionaCarrito()
    {
        var context = CrearContexto();
        var user = new IdentityUser { Id = "u3", UserName = "u3@test.com", Email = "u3@test.com" };
        var controller = CrearController(context, user, HttpStatusCode.OK, "{\"aprobado\":true,\"transaccionId\":\"TX-2\"}");

        var resultado = await controller.ProcesarPago("4242424242424242", "Usuario Demo");

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Carrito", redirect.ControllerName);
    }

    [Fact]
    public async Task ProcesarPago_ReservaNoVigente_NoGeneraOrden()
    {
        var context = CrearContexto();
        var user = new IdentityUser { Id = "u4", UserName = "u4@test.com", Email = "u4@test.com" };

        await context.CarritoItems.AddAsync(new CarritoItem
        {
            UserId = user.Id,
            PeliculaId = 40,
            FuncionId = 4,
            TituloPelicula = "Demo 4",
            ButacaCodigo = "C3",
            Precio = 2000,
            Cantidad = 1
        });

        await context.SaveChangesAsync();

        var controller = CrearController(context, user, HttpStatusCode.OK, "{\"aprobado\":true,\"transaccionId\":\"TX-4\"}");
        var resultado = await controller.ProcesarPago("4242424242424242", "Usuario Demo");

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Carrito", redirect.ControllerName);
        Assert.Empty(context.Ordenes);
    }

    [Fact]
    public async Task ProcesarPago_RespuestaInvalida_LiberaReserva()
    {
        var context = CrearContexto();
        var user = new IdentityUser { Id = "u5", UserName = "u5@test.com", Email = "u5@test.com" };

        await context.CarritoItems.AddAsync(new CarritoItem
        {
            UserId = user.Id,
            PeliculaId = 50,
            FuncionId = 5,
            TituloPelicula = "Demo 5",
            ButacaCodigo = "D4",
            Precio = 3500,
            Cantidad = 1
        });

        await context.ReservasButacas.AddAsync(new ReservaButaca
        {
            FuncionId = 5,
            ButacaCodigo = "D4",
            UserId = user.Id,
            Estado = "Pendiente",
            CreadaEn = DateTime.UtcNow,
            ExpiraEn = DateTime.UtcNow.AddMinutes(10)
        });

        await context.SaveChangesAsync();

        var controller = CrearController(context, user, HttpStatusCode.OK, "{\"algo\":\"inesperado\"}");
        var resultado = await controller.ProcesarPago("4242424242424242", "Usuario Demo");

        var redirect = Assert.IsType<RedirectToActionResult>(resultado);
        Assert.Equal("Index", redirect.ActionName);

        var reserva = await context.ReservasButacas.SingleAsync();
        Assert.Equal("Liberada", reserva.Estado);
        Assert.Empty(context.Ordenes);
    }

    [Fact]
    public async Task Confirmacion_OrdenNoExiste_RetornaNotFound()
    {
        var context = CrearContexto();
        var user = new IdentityUser { Id = "u6", UserName = "u6@test.com", Email = "u6@test.com" };
        var controller = CrearController(context, user, HttpStatusCode.OK, "{\"aprobado\":true,\"transaccionId\":\"TX-6\"}");

        var resultado = await controller.Confirmacion(999);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Confirmacion_OrdenDeOtroUsuario_RetornaNotFound()
    {
        var context = CrearContexto();
        await context.Ordenes.AddAsync(new Orden
        {
            Id = 22,
            UserId = "otro-usuario",
            FechaOrden = DateTime.Now,
            TotalGeneral = 1000,
            TransaccionId = "TX-22"
        });
        await context.SaveChangesAsync();

        var user = new IdentityUser { Id = "u7", UserName = "u7@test.com", Email = "u7@test.com" };
        var controller = CrearController(context, user, HttpStatusCode.OK, "{\"aprobado\":true,\"transaccionId\":\"TX-7\"}");

        var resultado = await controller.Confirmacion(22);

        Assert.IsType<NotFoundResult>(resultado);
    }

    [Fact]
    public async Task Confirmacion_OrdenPropia_RetornaVistaConModelo()
    {
        var context = CrearContexto();
        var ordenCreada = new Orden
        {
            UserId = "u8",
            FechaOrden = DateTime.Now,
            TotalGeneral = 2000,
            TransaccionId = "TX-33",
            Detalles = new List<DetalleOrden>
            {
                new() { PeliculaId = 1, TituloPelicula = "Demo", Cantidad = 1, PrecioUnitario = 2000, SubTotal = 2000 }
            }
        };

        await context.Ordenes.AddAsync(ordenCreada);
        await context.SaveChangesAsync();

        var user = new IdentityUser { Id = "u8", UserName = "u8@test.com", Email = "u8@test.com" };
        var controller = CrearController(context, user, HttpStatusCode.OK, "{\"aprobado\":true,\"transaccionId\":\"TX-8\"}");

        var resultado = await controller.Confirmacion(ordenCreada.Id);

        var view = Assert.IsType<ViewResult>(resultado);
        var orden = Assert.IsType<Orden>(view.Model);
        Assert.Equal(ordenCreada.Id, orden.Id);
        Assert.Equal("u8", orden.UserId);
    }

    private static ApplicationDbContext CrearContexto()
    {
        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new ApplicationDbContext(options);
    }

    private static CheckoutController CrearController(ApplicationDbContext context, IdentityUser user, HttpStatusCode statusCode, string json)
    {
        var userManager = MockUserManager();
        userManager.Setup(x => x.GetUserAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(user);
        userManager.Setup(x => x.GetUserId(It.IsAny<ClaimsPrincipal>())).Returns(user.Id);

        var httpClientFactory = new Mock<IHttpClientFactory>();
        httpClientFactory
            .Setup(x => x.CreateClient(It.IsAny<string>()))
            .Returns(new HttpClient(new StubHttpMessageHandler(statusCode, json)));

        var controller = new CheckoutController(
            context,
            userManager.Object,
            httpClientFactory.Object,
            NullLogger<CheckoutController>.Instance,
            null);

        var identity = new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim(ClaimTypes.Name, user.UserName ?? user.Email ?? string.Empty)
        }, "TestAuth");

        var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(identity) };
        httpContext.Request.Scheme = "http";
        httpContext.Request.Host = new HostString("localhost", 5000);

        controller.ControllerContext = new ControllerContext { HttpContext = httpContext };
        controller.TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>());

        return controller;
    }

    private static Mock<UserManager<IdentityUser>> MockUserManager()
    {
        var store = new Mock<IUserStore<IdentityUser>>();
        return new Mock<UserManager<IdentityUser>>(
            store.Object,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!,
            null!);
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _statusCode;
        private readonly string _json;

        public StubHttpMessageHandler(HttpStatusCode statusCode, string json)
        {
            _statusCode = statusCode;
            _json = json;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(_statusCode)
            {
                Content = new StringContent(_json, Encoding.UTF8, "application/json")
            };

            return Task.FromResult(response);
        }
    }
}

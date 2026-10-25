using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ProyectoCine.Models;
using System.Text.Json;

namespace ProyectoCine.Controllers
{
    public class HomeController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _environment;

        public HomeController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult AccessDenied()
        {
            TempData["Error"] = "No tienes los permisos necesarios para acceder a esta sección.";
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.TurnstileSiteKey = ObtenerTurnstileSiteKey();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            ViewBag.TurnstileSiteKey = ObtenerTurnstileSiteKey();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var captchaValido = await ValidarTurnstileAsync(model.CaptchaToken);
            if (!captchaValido)
            {
                ModelState.AddModelError(string.Empty, "La validación del captcha ha fallado.");
                return View(model);
            }

            var user = new IdentityUser
            {
                UserName = model.Email,
                Email = model.Email,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                if (!await _roleManager.RoleExistsAsync("Comprador"))
                {
                    await _roleManager.CreateAsync(new IdentityRole("Comprador"));
                }

                await _userManager.AddToRoleAsync(user, "Comprador");

                TempData["MensajeExito"] = "¡Registro exitoso! Por favor, inicia sesión con tus datos.";
                return RedirectToAction("Login", "Home");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Login()
        {
            ViewBag.TurnstileSiteKey = ObtenerTurnstileSiteKey();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, string captchaToken)
        {
            ViewBag.TurnstileSiteKey = ObtenerTurnstileSiteKey();

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewBag.Error = "Debe llenar todos los campos.";
                return View();
            }

            var captchaValido = await ValidarTurnstileAsync(captchaToken);
            if (!captchaValido)
            {
                ViewBag.Error = "La validación del captcha ha fallado.";
                return View();
            }

            var result = await _signInManager.PasswordSignInAsync(
                email,
                password,
                isPersistent: false,
                lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ViewBag.Error = "Tu cuenta ha sido bloqueada temporalmente por intentos fallidos.";
                return View();
            }

            ViewBag.Error = "Correo o contraseña incorrectos.";
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        private async Task<bool> ValidarTurnstileAsync(string? token)
        {
            var bypassDevelopment = _environment.IsDevelopment() &&
                                    _configuration.GetValue<bool>("Turnstile:AllowBypassInDevelopment");

            if (bypassDevelopment)
            {
                return true;
            }

            if (string.IsNullOrWhiteSpace(token))
            {
                return false;
            }

            var secretKey = _configuration["Turnstile:SecretKey"];
            if (string.IsNullOrWhiteSpace(secretKey))
            {
                return false;
            }

            var client = _httpClientFactory.CreateClient();
            var parameters = new Dictionary<string, string>
            {
                { "secret", secretKey },
                { "response", token }
            };

            var response = await client.PostAsync(
                "https://challenges.cloudflare.com/turnstile/v0/siteverify",
                new FormUrlEncodedContent(parameters));

            if (!response.IsSuccessStatusCode)
            {
                return false;
            }

            await using var responseStream = await response.Content.ReadAsStreamAsync();
            var verifyResponse = await JsonSerializer.DeserializeAsync<TurnstileVerifyResponse>(responseStream);
            return verifyResponse?.Success == true;
        }

        private string ObtenerTurnstileSiteKey()
        {
            if (_environment.IsDevelopment())
            {
                return _configuration["Turnstile:SiteKey"] ?? "1x00000000000000000000AA";
            }

            return _configuration["Turnstile:SiteKey"] ?? string.Empty;
        }

        private sealed class TurnstileVerifyResponse
        {
            public bool Success { get; set; }
        }
    }
}

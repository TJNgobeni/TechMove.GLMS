using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using TechMove.GLMS.Models;

namespace TechMove.GLMS.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;

        public AccountController(IHttpClientFactory httpClientFactory, IConfiguration configuration)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null, CancellationToken ct = default)
        {
            if (!ModelState.IsValid)
                return View(model);

            var apiBase = _configuration["ApiBaseUrl"] ?? "https://localhost:5001";
            var client = _httpClientFactory.CreateClient();
            client.BaseAddress = new Uri(apiBase);

            var payload = new
            {
                Email = model.Email,
                Password = model.Password
            };

            var resp = await client.PostAsJsonAsync("/api/auth/login", payload, ct);
            if (!resp.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Invalid login.");
                return View(model);
            }

            var auth = await resp.Content.ReadFromJsonAsync<LoginApiResponse>(cancellationToken: ct);
            if (auth?.AccessToken is not { Length: > 0 })
            {
                ModelState.AddModelError(string.Empty, "Login failed.");
                return View(model);
            }

            HttpContext.Session.SetString("AccessToken", auth.AccessToken);
            HttpContext.Session.SetString("RefreshToken", auth.RefreshToken ?? string.Empty);

            return LocalRedirect(returnUrl ?? "/");
        }

        public IActionResult Logout()
        {
            HttpContext.Session.Remove("AccessToken");
            HttpContext.Session.Remove("RefreshToken");
            return RedirectToAction(nameof(HomeController.Index), "Home");
        }
    }

    public class LoginApiResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
    }
}

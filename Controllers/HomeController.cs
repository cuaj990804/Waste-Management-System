using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SGA.Models;
using System.Diagnostics;

namespace SGA.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        [Authorize]
        public IActionResult Index()
        {
            // Si es Administrador o Supervisor, redirigir al índice de residuos no peligrosos
            if (User.IsInRole("Administrator") || User.IsInRole("Supervisor"))
            {
                return RedirectToAction("Index", "NonHazardous");
            }

            // Para usuarios regulares, mostrar menú de selección
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}

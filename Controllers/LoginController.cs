using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SGA.Data;
using SGA.Models;
using System.Security.Claims;

namespace SGA.Controllers
{
    public class LoginController : Controller
    {
        private readonly SgaContext _context;
        public LoginController(SgaContext context)
        {
            _context = context;
        }
        [HttpGet]
        public IActionResult Index()
        {
            return View();
        }
        // GET: LoginController
        [HttpPost]
        public async Task<IActionResult> Index(User _User)
        {
            var user = _context.Users.FirstOrDefault(u => u.EmployeeNumber == _User.EmployeeNumber);

            if (user != null)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, user.FirstName + " " + user.LastName),
                    new Claim(ClaimTypes.NameIdentifier, user.EmployeeNumber)
                };

                foreach (string rol in user.UserRole.Split(','))
                {
                    claims.Add(new Claim(ClaimTypes.Role, rol.Trim()));
                }



                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(claimsIdentity));

                var roles = user.UserRole.Split(',').Select(r => r.Trim()).ToList();

                if (roles.Contains("Administrator"))
                {
                    return RedirectToAction("Index", "Users");
                }
                else if (roles.Contains("Supervisor"))
                {
                    return RedirectToAction("Index", "NonHazardous");
                }
                else if (roles.Contains("User"))
                {
                    return RedirectToAction("Index", "Home");
                }
            }

            return View();
        }
        public async Task<IActionResult> SignOut()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Index");

        }
    }
}
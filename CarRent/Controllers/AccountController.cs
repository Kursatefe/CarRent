using CarRent.ExtensionMethods;
using Data;
using Entities;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CarRent.Controllers
{
    public class AccountController : Controller
    {
        private readonly DatabaseContext _context;

        public AccountController(DatabaseContext context)
        {
            _context = context;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost, AllowAnonymous]
        public async Task<IActionResult> Login(string kemail, string kpassword)
        {
            try
            {
                var kullanici = await _context.Users.FirstOrDefaultAsync(u => u.Email == kemail && u.Password == kpassword);
                if (kullanici != null)
                {
                    HttpContext.Session.SetString("kullanici", kullanici.Name);
                    HttpContext.Session.SetString("soyad", kullanici.Surname);
                    HttpContext.Session.SetString("hesap", kullanici.Email);
                    HttpContext.Session.SetString("tel", kullanici.Phone);
                    HttpContext.Session.SetString("sifre", kpassword);
                    HttpContext.Session.SetInt32("IsLoggedIn", 1);
                    HttpContext.Session.SetInt32("UserId", kullanici.Id); // Kullanıcı ID'sini oturuma ekleyin
                    HttpContext.Session.SetJson("musteri", kullanici);
                    HttpContext.Session.SetString("tel", kullanici.Phone);
                    HttpContext.Session.SetString("Adres", kullanici.Address);
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    TempData["Message"] = "<p class='alert alert-danger'>Giriş Başarısız!</p>";
                }
            }
            catch (Exception hata)
            {
                TempData["Message"] = hata.InnerException?.Message;
            }
            return View();
        }


        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind("Id,Name,Surname,Email,Phone,Password,CreateDate,Address")] User user)
        {
            if (ModelState.IsValid)
            {
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction("Login", "Account");
            }
            return View(user);
        }

        public async Task<IActionResult> LogoutAsync()
        {
            HttpContext.Session.Remove("kullanici");
            HttpContext.Session.Remove("soyad");
            HttpContext.Session.Remove("hesap");
            HttpContext.Session.Remove("tel");
            HttpContext.Session.Remove("sifre");
            HttpContext.Session.Remove("Adres");
            HttpContext.Session.SetInt32("IsLoggedIn", 0);
            await HttpContext.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }
        public async Task<IActionResult> Update()
        {
            var kullaniciEmail = HttpContext.Session.GetString("hesap");
            var kullanici = await _context.Users.FirstOrDefaultAsync(u => u.Email == kullaniciEmail);
            if (kullanici == null)
            {
                return NotFound();
            }

            return View(kullanici);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(int id, [Bind("Id,Name,Surname,Email,Phone,Password,Address")] User updatedCustomer)
        {
            if (id != updatedCustomer.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(updatedCustomer);
                    await _context.SaveChangesAsync();


                    HttpContext.Session.SetString("kullanici", updatedCustomer.Name);
                    HttpContext.Session.SetString("soyad", updatedCustomer.Surname);
                    HttpContext.Session.SetString("hesap", updatedCustomer.Email);
                    HttpContext.Session.SetString("tel", updatedCustomer.Phone);
                    HttpContext.Session.SetString("sifre", updatedCustomer.Password);
                    HttpContext.Session.SetString("Adres", updatedCustomer.Address);


                    return RedirectToAction("Index", "Account");
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!CustomerExists(updatedCustomer.Id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }
            return View(updatedCustomer);
        }



        private bool CustomerExists(int id)
        {
            return _context.Users.Any(e => e.Id == id);
        }

        public async Task<IActionResult> KullaniciYorum()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                // Kullanıcı oturum açmamışsa hata döndür
                return Unauthorized();
            }

            var userComments = await _context.Comments
                .Where(c => c.UserId == userId)
                .Include(c => c.Car) // Yorum yapılan araç bilgilerini de dahil et
                .ToListAsync();

            return View(userComments);
        }
        public async Task<IActionResult> RentedCars()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                // User is not authenticated
                return Unauthorized();
            }

            var rentedCars = await _context.Orders
                .Where(o => o.UserId == userId)
                .Include(o => o.Car)
                    .ThenInclude(c => c.Brand)
                .ToListAsync();

            return View(rentedCars);
        }

    }
}

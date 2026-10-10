using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VbeSort.Data;
using VbeSort.Models;

namespace VbeSort.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<User> _hasher = new();

        public AdminController(AppDbContext context) => _context = context;

        public async Task<IActionResult> Index() => View(await _context.Users.OrderBy(u => u.Id).ToListAsync());

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateUserViewModel model)
        {
            if (!ModelState.IsValid) { TempData["Error"] = "Neteisingi duomenys."; return RedirectToAction(nameof(Index)); }

            if (await _context.Users.AnyAsync(u => u.Username.ToLower() == model.Username.ToLower()))
            {
                TempData["Error"] = "Vartotojas su tokiu vardu jau egzistuoja.";
                return RedirectToAction(nameof(Index));
            }

            var user = new User { Username = model.Username.Trim(), Role = model.Role == "Admin" ? "Admin" : "Student", CreatedAt = DateTime.UtcNow };
            user.PasswordHash = _hasher.HashPassword(user, model.Password);
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Vartotojas '{user.Username}' sėkmingai sukurtas!";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return NotFound();
            if (user.Username.Equals(User.Identity?.Name, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "Negalite ištrinti savo paskyros.";
                return RedirectToAction(nameof(Index));
            }
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            TempData["Success"] = $"Vartotojas '{user.Username}' pašalintas.";
            return RedirectToAction(nameof(Index));
        }
    }
}

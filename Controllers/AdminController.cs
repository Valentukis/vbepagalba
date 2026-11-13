using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VbeSort.Data;
using VbeSort.Models;

namespace VbeSort.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _hasher = new();

    public AdminController(AppDbContext context) => _context = context;

    public async Task<IActionResult> Index()
    {
        var users = await _context.Users.OrderBy(u => u.Id).ToListAsync();

        var solvedCounts = await _context.ExerciseProgresses
            .Where(p => p.Status == ExerciseStatus.Solved)
            .GroupBy(p => p.UserId)
            .Select(g => new { UserId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.UserId, x => x.Count);

        var vm = new AdminDashboardViewModel
        {
            Users = users,
            SolvedByUserId = solvedCounts
        };

        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserViewModel model)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Neteisingi duomenys (vardas min. 3 simb., slaptažodis min. 4 simb.).";
            return RedirectToAction(nameof(Index));
        }

        var username = model.Username.Trim();
        if (await _context.Users.AnyAsync(u => u.Username.ToLower() == username.ToLower()))
        {
            TempData["Error"] = "Vartotojas su tokiu vardu jau egzistuoja.";
            return RedirectToAction(nameof(Index));
        }

        var user = new User
        {
            Username = username,
            DisplayName = string.IsNullOrWhiteSpace(model.DisplayName) ? username : model.DisplayName.Trim(),
            Role = model.Role == "Admin" ? "Admin" : "Student",
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = _hasher.HashPassword(user, model.Password);
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Vartotojas '{user.Username}' sėkmingai sukurtas!";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ResetPassword(int id, string newPassword)
    {
        var user = await _context.Users.FindAsync(id);
        if (user is null) return NotFound();

        if (string.IsNullOrWhiteSpace(newPassword) || newPassword.Trim().Length < 4)
        {
            TempData["Error"] = "Naujas slaptažodis turi būti bent 4 simbolių.";
            return RedirectToAction(nameof(Index));
        }

        user.PasswordHash = _hasher.HashPassword(user, newPassword.Trim());
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Vartotojo '{user.Username}' slaptažodis atstatytas.";
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

        var progresses = _context.ExerciseProgresses.Where(p => p.UserId == id);
        var exams = _context.ExamAttempts.Where(a => a.UserId == id);

        _context.ExerciseProgresses.RemoveRange(progresses);
        _context.ExamAttempts.RemoveRange(exams);
        _context.Users.Remove(user);
        await _context.SaveChangesAsync();

        TempData["Success"] = $"Vartotojas '{user.Username}' pašalintas.";
        return RedirectToAction(nameof(Index));
    }
}

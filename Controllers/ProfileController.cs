using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VbeSort.Data;
using VbeSort.Infrastructure;
using VbeSort.Models;
using VbeSort.Services;

namespace VbeSort.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly AppDbContext _db;
    private readonly ExerciseService _exercises;
    private readonly StudyProgressService _progressService;
    private readonly PasswordHasher<User> _hasher = new();

    public ProfileController(
        AppDbContext db,
        ExerciseService exercises,
        StudyProgressService progressService)
    {
        _db = db;
        _exercises = exercises;
        _progressService = progressService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        var user = await RequireCurrentUserAsync();
        if (user is null) return RedirectToAction("Login", "Account");

        var progressMap = await _progressService.GetUserProgressMapAsync(user.Id);
        var masteries = _progressService.ComputeTopicMasteries(progressMap);
        var examHistory = await _db.ExamAttempts
            .Where(a => a.UserId == user.Id)
            .OrderByDescending(a => a.CompletedAtUtc)
            .Take(20)
            .ToListAsync(ct);

        var allExercises = _exercises.GetAllExercises();
        var dueCount = progressMap.Values.Count(p =>
            p.Status == ExerciseStatus.NeedsReview || p.Status == ExerciseStatus.Hard);

        var notedExercises = allExercises
            .Where(e => progressMap.TryGetValue(e.Id, out var p) && !string.IsNullOrWhiteSpace(p.PersonalNote))
            .Select(e => (e, progressMap[e.Id]))
            .OrderByDescending(x => x.Item2.UpdatedAtUtc)
            .Take(15)
            .ToList();

        var vm = new ProfileDashboardViewModel
        {
            User = user,
            TotalExercisesInSystem = masteries.Sum(m => m.TotalExercises),
            SolvedUniqueCount = masteries.Sum(m => m.SolvedCount),
            DueReviewCount = dueCount,
            TopicMasteries = masteries,
            ExamHistory = examHistory,
            NotedExercises = notedExercises
        };

        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        var user = await RequireCurrentUserAsync();
        if (user is null) return RedirectToAction("Login", "Account");

        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Naujas slaptažodis turi būti bent 4 simbolių ir sutapti su pakartojimu.";
            return RedirectToAction(nameof(Index));
        }

        if (_hasher.VerifyHashedPassword(user, user.PasswordHash, model.CurrentPassword) == PasswordVerificationResult.Failed)
        {
            TempData["Error"] = "Dabartinis slaptažodis neteisingas.";
            return RedirectToAction(nameof(Index));
        }

        user.PasswordHash = _hasher.HashPassword(user, model.NewPassword);
        await _db.SaveChangesAsync();

        TempData["Success"] = "Slaptažodis sėkmingai pakeistas.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<User?> RequireCurrentUserAsync()
    {
        var id = User.GetUserId();
        if (id.HasValue)
        {
            var byId = await _db.Users.FindAsync(id.Value);
            if (byId is not null) return byId;
        }

        var name = User.Identity?.Name;
        if (string.IsNullOrWhiteSpace(name)) return null;

        return await _db.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == name.ToLower());
    }
}

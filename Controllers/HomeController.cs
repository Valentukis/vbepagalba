using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VbeSort.Data;
using VbeSort.Infrastructure;
using VbeSort.Models;
using VbeSort.Services;

namespace VbeSort.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ExerciseService _exerciseService;
    private readonly ExamPdfService _examPdfService;
    private readonly StudyProgressService _progressService;
    private readonly AppDbContext _db;

    public HomeController(
        ExerciseService exerciseService,
        ExamPdfService examPdfService,
        StudyProgressService progressService,
        AppDbContext db)
    {
        _exerciseService = exerciseService;
        _examPdfService = examPdfService;
        _progressService = progressService;
        _db = db;
    }

    [AllowAnonymous]
    public async Task<IActionResult> Index()
    {
        var topics = _exerciseService.GetTopics();

        var vm = new HomeIndexViewModel
        {
            Topics = topics,
            TotalExercisesCount = topics.Sum(t => t.Exercises.Count)
        };

        var user = await ResolveCurrentUserAsync();
        if (user is not null)
        {
            vm.CurrentUser = user;
            var progressMap = await _progressService.GetUserProgressMapAsync(user.Id);
            var masteries = _progressService.ComputeTopicMasteries(progressMap);

            vm.TopicMastery = masteries.ToDictionary(m => m.TopicName, StringComparer.OrdinalIgnoreCase);
            vm.TotalSolvedCount = masteries.Sum(m => m.SolvedCount);
            vm.DueForReviewCount = progressMap.Values.Count(p =>
                p.Status == ExerciseStatus.NeedsReview || p.Status == ExerciseStatus.Hard);
        }

        return View(vm);
    }

    [AllowAnonymous]
    public IActionResult Statistics()
    {
        var topics = _exerciseService.GetTopics();
        var allUnique = _exerciseService.GetAllExercises();

        var byYear = allUnique
            .GroupBy(e => e.Year)
            .OrderBy(g => g.Key)
            .ToDictionary(g => g.Key, g => g.Count());

        var vm = new StatisticsPageViewModel
        {
            Topics = topics,
            TotalExercises = topics.Sum(t => t.Exercises.Count),
            ExercisesByYear = byYear
        };

        return View(vm);
    }

    public async Task<IActionResult> Topic(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return NotFound();

        var topic = _exerciseService.GetTopic(name);
        if (topic is null) return NotFound();

        var user = await ResolveCurrentUserAsync();
        var progressMap = user is not null
            ? await _progressService.GetUserProgressMapAsync(user.Id)
            : new Dictionary<string, ExerciseProgress>();

        var masteries = _progressService.ComputeTopicMasteries(progressMap);
        var mastery = masteries.FirstOrDefault(m => m.TopicName.Equals(topic.Name, StringComparison.OrdinalIgnoreCase));

        var vm = new TopicPageViewModel
        {
            Topic = topic,
            Mastery = mastery,
            ProgressByExerciseId = progressMap
        };

        return View(vm);
    }

    [HttpGet]
    public async Task<IActionResult> Practice(string mode = "due")
    {
        var user = await ResolveCurrentUserAsync();
        if (user is null) return RedirectToAction("Login", "Account");

        var progressMap = await _progressService.GetUserProgressMapAsync(user.Id);
        var normalizedMode = (mode ?? "due").Trim().ToLowerInvariant();
        if (normalizedMode is not ("due" or "unseen" or "solved"))
        {
            normalizedMode = "due";
        }

        var allCount = _exerciseService.GetAllExercises().Count;
        var dueCount = progressMap.Values.Count(p =>
            p.Status == ExerciseStatus.NeedsReview || p.Status == ExerciseStatus.Hard);
        var solvedCount = progressMap.Values.Count(p => p.Status == ExerciseStatus.Solved);
        var unseenCount = Math.Max(0, allCount - dueCount - solvedCount);

        var queue = _progressService.BuildSmartQueue(normalizedMode, progressMap, limit: 30);

        var vm = new PracticeQueueViewModel
        {
            Mode = normalizedMode,
            Queue = queue,
            DueCount = dueCount,
            UnseenCount = unseenCount,
            SolvedCount = solvedCount
        };

        return View(vm);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Formulas() => View();

    [HttpGet]
    public async Task<IActionResult> MockExamSetup()
    {
        var topics = _exerciseService.GetTopics();
        var user = await ResolveCurrentUserAsync();

        var progressMap = user is not null
            ? await _progressService.GetUserProgressMapAsync(user.Id)
            : new Dictionary<string, ExerciseProgress>();

        var masteries = _progressService.ComputeTopicMasteries(progressMap)
            .ToDictionary(m => m.TopicName, StringComparer.OrdinalIgnoreCase);

        var recentAttempts = user is not null
            ? await _db.ExamAttempts
                .Where(a => a.UserId == user.Id)
                .OrderByDescending(a => a.CompletedAtUtc)
                .Take(6)
                .ToListAsync()
            : new List<ExamAttempt>();

        var sessions = _exerciseService.GetAvailableExamSessions();
        var reviewDueTotal = progressMap.Values.Count(p =>
            p.Status == ExerciseStatus.NeedsReview || p.Status == ExerciseStatus.Hard);

        var vm = new MockExamSetupViewModel
        {
            AvailableTopics = topics.Select(t => t.Name).ToList(),
            TopicDetails = topics,
            AvailableExamSessions = sessions,
            SelectedExamSession = sessions.FirstOrDefault()?.Key ?? string.Empty,
            MasteryByTopic = masteries,
            QuestionCount = 10,
            TimeLimitMinutes = 45,
            ExamMode = "full_session",
            ReviewDueTotalCount = reviewDueTotal,
            RecentAttempts = recentAttempts
        };

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> MockExam(MockExamSetupViewModel model)
    {
        var topics = _exerciseService.GetTopics();
        var sessions = _exerciseService.GetAvailableExamSessions();
        var user = await ResolveCurrentUserAsync();
        var progressMap = user is not null
            ? await _progressService.GetUserProgressMapAsync(user.Id)
            : new Dictionary<string, ExerciseProgress>();

        var reviewDueTotal = progressMap.Values.Count(p =>
            p.Status == ExerciseStatus.NeedsReview || p.Status == ExerciseStatus.Hard);
        var mode = (model.ExamMode ?? "full_session").Trim().ToLowerInvariant();

        if (mode == "full_session")
        {
            var chosenKey = string.IsNullOrWhiteSpace(model.SelectedExamSession)
                ? sessions.FirstOrDefault()?.Key ?? "2026_pagr"
                : model.SelectedExamSession.Trim();

            var parts = chosenKey.Split('_', 2);
            var year = parts.Length > 0 ? parts[0] : "2026";
            var session = parts.Length > 1 ? parts[1] : "pagr";

            var fullExercises = _exerciseService.GetExercisesByExamSession(year, session);
            var sessLabel = session == "pagr" ? "pagrindinės sesijos" : "pakartotinės sesijos";

            return View(new MockExamViewModel
            {
                Title = $"{year} m. {sessLabel} VBE egzaminas",
                ExamMode = "full_session",
                ExamYear = year,
                ExamSession = session,
                TimeLimitMinutes = Math.Clamp(model.TimeLimitMinutes <= 0 ? 180 : model.TimeLimitMinutes, 5, 240),
                SelectedTopics = fullExercises.Select(e => e.Topic).Distinct().ToList(),
                Exercises = fullExercises,
                ExistingProgress = progressMap
            });
        }

        if ((model.SelectedTopics == null || !model.SelectedTopics.Any()) && mode is "custom" or "unseen_only")
        {
            ModelState.AddModelError("", "Pasirinkite bent vieną temą.");
            model.AvailableTopics = topics.Select(t => t.Name).ToList();
            model.TopicDetails = topics;
            model.AvailableExamSessions = sessions;
            model.ReviewDueTotalCount = reviewDueTotal;
            model.MasteryByTopic = _progressService.ComputeTopicMasteries(progressMap)
                .ToDictionary(m => m.TopicName, StringComparer.OrdinalIgnoreCase);
            return View("MockExamSetup", model);
        }

        var selectedTopics = model.SelectedTopics != null && model.SelectedTopics.Any()
            ? model.SelectedTopics
            : topics.Select(t => t.Name).ToList();

        var exercises = _exerciseService.BuildMockExam(
            selectedTopics,
            model.QuestionCount,
            mode,
            progressMap);

        if (exercises.Count == 0)
        {
            var errMsg = mode == "review_due"
                ? "Neturite pakartojimui atidėtų užduočių. Pirmiausia temų puslapiuose pažymėkite užduotis mygtuku „Pakartoti vėliau“."
                : "Pagal pasirinktus kriterijus užduočių nerasta.";
            ModelState.AddModelError("", errMsg);
            model.AvailableTopics = topics.Select(t => t.Name).ToList();
            model.TopicDetails = topics;
            model.AvailableExamSessions = sessions;
            model.ReviewDueTotalCount = reviewDueTotal;
            model.MasteryByTopic = _progressService.ComputeTopicMasteries(progressMap)
                .ToDictionary(m => m.TopicName, StringComparer.OrdinalIgnoreCase);
            return View("MockExamSetup", model);
        }

        var vm = new MockExamViewModel
        {
            Title = "Bandomasis egzaminas",
            ExamMode = mode,
            TimeLimitMinutes = Math.Clamp(model.TimeLimitMinutes <= 0 ? 45 : model.TimeLimitMinutes, 5, 180),
            SelectedTopics = selectedTopics,
            Exercises = exercises,
            ExistingProgress = progressMap
        };

        return View(vm);
    }

    [AllowAnonymous]
    [HttpGet]
    public IActionResult ExamPdf(string year, string session)
    {
        if (string.IsNullOrWhiteSpace(year) || string.IsNullOrWhiteSpace(session))
        {
            return NotFound();
        }

        var pdfBytes = _examPdfService.BuildSessionExamPdf(year.Trim(), session.Trim());
        if (pdfBytes is null || pdfBytes.Length == 0)
        {
            return NotFound("Šios sesijos egzamino PDF nepavyko sugeneruoti.");
        }

        Response.Headers.ContentDisposition = $"inline; filename=\"{year}_{session}_egzaminas.pdf\"";
        return File(pdfBytes, "application/pdf");
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    private async Task<User?> ResolveCurrentUserAsync()
    {
        if (User.Identity?.IsAuthenticated != true) return null;

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

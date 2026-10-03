using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VbeSort.Models;
using VbeSort.Services;

namespace VbeSort.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly ExerciseService _exerciseService;

    public HomeController(ExerciseService exerciseService) => _exerciseService = exerciseService;

    [AllowAnonymous]
    public IActionResult Index() => View(_exerciseService.GetTopics());

    [AllowAnonymous]
    public IActionResult Statistics() => View(_exerciseService.GetTopics());

    public IActionResult Topic(string name)
    {
        var topic = _exerciseService.GetTopic(name);
        return topic == null ? NotFound() : View(topic);
    }

    [HttpGet]
    public IActionResult MockExamSetup() => View(new MockExamSetupViewModel { AvailableTopics = _exerciseService.GetTopics().Select(t => t.Name).ToList() });

    [HttpPost]
    public IActionResult MockExam(MockExamSetupViewModel model)
    {
        if (model.SelectedTopics == null || !model.SelectedTopics.Any())
        {
            ModelState.AddModelError("", "Pasirinkite bent vieną temą.");
            model.AvailableTopics = _exerciseService.GetTopics().Select(t => t.Name).ToList();
            return View("MockExamSetup", model);
        }

        var exercises = model.SelectedTopics
            .Select(_exerciseService.GetTopic)
            .Where(t => t != null)
            .SelectMany(t => t!.Exercises)
            .OrderBy(_ => Random.Shared.Next())
            .Take(Math.Clamp(model.QuestionCount, 1, 50))
            .ToList();

        return View(new MockExamViewModel { Exercises = exercises });
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}

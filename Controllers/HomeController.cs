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

    public HomeController(ExerciseService exerciseService)
    {
        _exerciseService = exerciseService;
    }

    [AllowAnonymous]
    public IActionResult Index()
    {
        var topics = _exerciseService.GetTopics();
        return View(topics);
    }

    public IActionResult Topic(string name)
    {
        var topic = _exerciseService.GetTopic(name);
        if (topic == null) return NotFound();
        return View(topic);
    }

    [HttpGet]
    public IActionResult MockExamSetup()
    {
        var topics = _exerciseService.GetTopics().Select(t => t.Name).ToList();
        var model = new MockExamSetupViewModel
        {
            AvailableTopics = topics
        };
        return View(model);
    }

    [HttpPost]
    public IActionResult MockExam(MockExamSetupViewModel model)
    {
        if (model.SelectedTopics == null || !model.SelectedTopics.Any())
        {
            ModelState.AddModelError("", "Prašome pasirinkti bent vieną temą.");
            model.AvailableTopics = _exerciseService.GetTopics().Select(t => t.Name).ToList();
            return View("MockExamSetup", model);
        }

        var allSelectedExercises = new List<Exercise>();
        foreach (var topicName in model.SelectedTopics)
        {
            var topic = _exerciseService.GetTopic(topicName);
            if (topic != null)
            {
                allSelectedExercises.AddRange(topic.Exercises);
            }
        }

        var random = new Random();
        var examExercises = allSelectedExercises
            .OrderBy(x => random.Next())
            .Take(model.QuestionCount)
            .ToList();

        var examViewModel = new MockExamViewModel
        {
            Exercises = examExercises
        };

        return View(examViewModel);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}

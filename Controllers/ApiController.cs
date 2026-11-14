using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using VbeSort.Data;
using VbeSort.Infrastructure;
using VbeSort.Models;
using VbeSort.Services;

namespace VbeSort.Controllers;

/// <summary>
/// JSON API endpointai užduočių pažangos ir bandomųjų egzaminų rezultatų išsaugojimui.
/// </summary>
[Route("Api")]
[ApiController]
[Authorize]
public class ApiController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly StudyProgressService _progressService;

    public ApiController(AppDbContext db, StudyProgressService progressService)
    {
        _db = db;
        _progressService = progressService;
    }

    [HttpPost("Progress")]
    public async Task<IActionResult> UpdateProgress([FromBody] RecordProgressRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ExerciseId))
        {
            return BadRequest(new { error = "Nenurodytas užduoties ID." });
        }

        var user = await ResolveUserAsync();
        if (user is null) return Unauthorized();

        var updated = await _progressService.RecordExerciseUpdateAsync(user.Id, request);

        return Ok(new
        {
            exerciseId = updated.ExerciseId,
            status = (int)updated.Status,
            statusText = updated.Status switch
            {
                ExerciseStatus.Solved => "Išspręsta",
                ExerciseStatus.Hard => "Atidėta pakartoti",
                ExerciseStatus.NeedsReview => "Atidėta pakartoti",
                _ => "Nespręsta"
            },
            personalNote = updated.PersonalNote
        });
    }

    [HttpPost("ExamAttempt")]
    public async Task<IActionResult> SaveExamAttempt([FromBody] SubmitExamAttemptRequest request)
    {
        var user = await ResolveUserAsync();
        if (user is null) return Unauthorized();

        if (request.Items == null || request.Items.Count == 0)
        {
            return BadRequest(new { error = "Egzamine nėra įvertintų užduočių." });
        }

        var attempt = await _progressService.RecordExamAttemptAsync(user.Id, request);

        return Ok(new
        {
            attemptId = attempt.Id,
            totalQuestions = attempt.TotalQuestions,
            solvedCorrectly = attempt.SolvedCorrectly,
            scorePercentage = attempt.ScorePercentage
        });
    }

    private async Task<User?> ResolveUserAsync()
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

using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using VbeSort.Data;
using VbeSort.Infrastructure;
using VbeSort.Models;

namespace VbeSort.Services;

public class StudyProgressService
{
    private readonly AppDbContext _db;
    private readonly ExerciseService _exercises;
    private readonly IClock _clock;

    public StudyProgressService(AppDbContext db, ExerciseService exercises, IClock clock)
    {
        _db = db;
        _exercises = exercises;
        _clock = clock;
    }

    public async Task<Dictionary<string, ExerciseProgress>> GetUserProgressMapAsync(int userId)
    {
        var list = await _db.ExerciseProgresses
            .Where(p => p.UserId == userId)
            .ToListAsync();

        return list.ToDictionary(p => p.ExerciseId, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ExerciseProgress> RecordExerciseUpdateAsync(int userId, RecordProgressRequest request)
    {
        var now = _clock.UtcNow;
        var ex = _exercises.GetExerciseById(request.ExerciseId);
        var topicName = ex?.Topic ?? string.Empty;

        var progress = await _db.ExerciseProgresses
            .FirstOrDefaultAsync(p => p.UserId == userId && p.ExerciseId == request.ExerciseId);

        if (progress is null)
        {
            progress = new ExerciseProgress
            {
                UserId = userId,
                ExerciseId = request.ExerciseId,
                TopicName = topicName
            };
            _db.ExerciseProgresses.Add(progress);
        }

        if (!string.IsNullOrEmpty(topicName))
        {
            progress.TopicName = topicName;
        }

        if (request.PersonalNote is not null)
        {
            progress.PersonalNote = request.PersonalNote.Trim();
        }

        progress.Status = request.Status;
        progress.UpdatedAtUtc = now;

        if (request.Status != ExerciseStatus.Unseen)
        {
            progress.AttemptCount++;
            progress.LastAttemptAtUtc = now;
            if (request.Status == ExerciseStatus.Solved)
            {
                progress.CorrectCount++;
            }
        }

        await _db.SaveChangesAsync();
        return progress;
    }

    public async Task<ExamAttempt> RecordExamAttemptAsync(int userId, SubmitExamAttemptRequest request)
    {
        var now = _clock.UtcNow;
        var details = new List<ExamAttemptItemDetail>();
        var totalPoints = 0;
        var maxPoints = 0;
        var solvedCount = 0;
        var partialCount = 0;

        var existingProgress = await GetUserProgressMapAsync(userId);

        foreach (var item in request.Items)
        {
            var ex = _exercises.GetExerciseById(item.ExerciseId);
            if (ex is null) continue;

            var maxItemPts = ex.SuggestedPoints;
            var outcome = (intOutcome)Math.Clamp(item.Outcome, 0, 2);
            var earnedPts = outcome switch
            {
                intOutcome.Correct => maxItemPts,
                intOutcome.Partial => Math.Max(1, maxItemPts / 2),
                _ => 0
            };

            maxPoints += maxItemPts;
            totalPoints += earnedPts;
            if (outcome == intOutcome.Correct) solvedCount++;
            else if (outcome == intOutcome.Partial) partialCount++;

            details.Add(new ExamAttemptItemDetail
            {
                ExerciseId = ex.Id,
                Topic = ex.Topic,
                Year = ex.Year,
                Session = ex.Session,
                Number = ex.Number,
                Outcome = outcome,
                PointsEarned = earnedPts,
                MaxPoints = maxItemPts
            });

            if (!existingProgress.TryGetValue(ex.Id, out var progress))
            {
                progress = new ExerciseProgress
                {
                    UserId = userId,
                    ExerciseId = ex.Id,
                    TopicName = ex.Topic
                };
                _db.ExerciseProgresses.Add(progress);
                existingProgress[ex.Id] = progress;
            }

            progress.TopicName = ex.Topic;
            progress.AttemptCount++;
            progress.LastAttemptAtUtc = now;
            progress.UpdatedAtUtc = now;

            if (outcome == intOutcome.Correct)
            {
                progress.CorrectCount++;
                progress.Status = ExerciseStatus.Solved;
            }
            else
            {
                progress.Status = ExerciseStatus.NeedsReview;
            }
        }

        var topicsList = request.Topics.Count > 0
            ? request.Topics
            : details.Select(d => d.Topic).Distinct().ToList();

        var attempt = new ExamAttempt
        {
            UserId = userId,
            Title = string.IsNullOrWhiteSpace(request.Title) ? "Bandomasis egzaminas" : request.Title.Trim(),
            Mode = string.IsNullOrWhiteSpace(request.Mode) ? "custom" : request.Mode.Trim(),
            TotalQuestions = details.Count,
            SolvedCorrectly = solvedCount,
            PartiallySolved = partialCount,
            TotalPointsEarned = totalPoints,
            MaxPossiblePoints = maxPoints,
            DurationSeconds = Math.Max(1, request.DurationSeconds),
            TopicsCsv = string.Join(", ", topicsList),
            DetailsJson = JsonSerializer.Serialize(details),
            StartedAtUtc = now.AddSeconds(-Math.Max(1, request.DurationSeconds)),
            CompletedAtUtc = now
        };

        _db.ExamAttempts.Add(attempt);
        await _db.SaveChangesAsync();
        return attempt;
    }

    public List<TopicMasteryDto> ComputeTopicMasteries(IReadOnlyDictionary<string, ExerciseProgress> progressMap)
    {
        var result = new List<TopicMasteryDto>();

        foreach (var topic in _exercises.GetTopics())
        {
            var total = topic.Exercises.Count;
            var solved = 0;
            var due = 0;

            foreach (var ex in topic.Exercises)
            {
                if (!progressMap.TryGetValue(ex.Id, out var p)) continue;

                if (p.Status == ExerciseStatus.Solved) solved++;
                else if (p.Status == ExerciseStatus.NeedsReview || p.Status == ExerciseStatus.Hard) due++;
            }

            result.Add(new TopicMasteryDto(
                TopicName: topic.Name,
                TotalExercises: total,
                SolvedCount: solved,
                ReviewDueCount: due
            ));
        }

        return result;
    }

    public List<(Exercise Exercise, ExerciseProgress? Progress, string ReasonBadge)> BuildSmartQueue(
        string mode,
        IReadOnlyDictionary<string, ExerciseProgress> progressMap,
        int limit = 30)
    {
        var all = _exercises.GetAllExercises();

        return mode switch
        {
            "due" or "hard" or "review" => all
                .Where(e => progressMap.TryGetValue(e.Id, out var p) &&
                            (p.Status == ExerciseStatus.NeedsReview || p.Status == ExerciseStatus.Hard))
                .OrderByDescending(e => progressMap[e.Id].UpdatedAtUtc)
                .Take(limit)
                .Select(e => (e, (ExerciseProgress?)progressMap[e.Id], "Atidėta pakartoti"))
                .ToList(),

            "solved" => all
                .Where(e => progressMap.TryGetValue(e.Id, out var p) && p.Status == ExerciseStatus.Solved)
                .OrderByDescending(e => progressMap[e.Id].UpdatedAtUtc)
                .Take(limit)
                .Select(e => (e, (ExerciseProgress?)progressMap[e.Id], "Išspręsta"))
                .ToList(),

            _ => all
                .Where(e => !progressMap.TryGetValue(e.Id, out var p) || p.Status == ExerciseStatus.Unseen)
                .OrderByDescending(e => e.Year)
                .ThenBy(e => e.Session)
                .ThenBy(e => e.Number)
                .Take(limit)
                .Select(e => (e, progressMap.TryGetValue(e.Id, out var p) ? p : null, "Nespręsta"))
                .ToList()
        };
    }
}

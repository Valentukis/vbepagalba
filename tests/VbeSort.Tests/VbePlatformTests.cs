using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VbeSort.Data;
using VbeSort.Infrastructure;
using VbeSort.Models;
using VbeSort.Services;
using Xunit;

namespace VbeSort.Tests;

public class VbePlatformTests
{
    private static string FindRepoRoot()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null && !File.Exists(Path.Combine(dir.FullName, "vbetool.csproj")))
        {
            dir = dir.Parent;
        }
        return dir?.FullName ?? Directory.GetCurrentDirectory();
    }

    [Fact]
    public void PngDimensionReader_ReadsExactPngHeaderDimensions()
    {
        var root = FindRepoRoot();
        var samplePng = Directory.GetFiles(Path.Combine(root, "surusiuotos"), "*.png", SearchOption.AllDirectories).First();
        var dims = PngDimensionReader.TryRead(samplePng);

        Assert.NotNull(dims);
        Assert.True(dims.Value.Width > 50);
        Assert.True(dims.Value.Height > 20);
    }

    [Fact]
    public void ExerciseService_LoadsAllTopicsAndDetectsSharedAnswerRanges()
    {
        var root = FindRepoRoot();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["VbeConfig:BasePath"] = root })
            .Build();

        var service = new ExerciseService(config);
        var topics = service.GetTopics();

        Assert.Equal(17, topics.Count);
        Assert.True(service.GetAllExercises().Count > 300);

        foreach (var ex in service.GetAllExercises().Take(40))
        {
            Assert.NotEmpty(ex.QuestionAssets);
            Assert.All(ex.QuestionAssets, a =>
            {
                Assert.True(a.Width > 0);
                Assert.True(a.Height > 0);
            });
        }

        var sharedAnswerEx = service.GetAllExercises()
            .FirstOrDefault(e => e.AnswerAssets.Any(a => a.IsSharedRange));
        Assert.NotNull(sharedAnswerEx);
    }

    [Fact]
    public async Task StudyProgressService_TracksProgressAndMockExams()
    {
        using var conn = new SqliteConnection("Data Source=:memory:");
        await conn.OpenAsync();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(conn)
            .Options;

        await using var db = new AppDbContext(options);
        DbInitializer.Initialize(db);

        var root = FindRepoRoot();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["VbeConfig:BasePath"] = root })
            .Build();

        var exerciseService = new ExerciseService(config);
        var clock = new FixedClock(new DateTime(2026, 11, 15, 10, 0, 0, DateTimeKind.Utc));
        var progressService = new StudyProgressService(db, exerciseService, clock);

        var student = await db.Users.FirstAsync(u => u.Username == "lukre");
        var firstEx = exerciseService.GetAllExercises().First();

        var prog = await progressService.RecordExerciseUpdateAsync(student.Id, new RecordProgressRequest
        {
            ExerciseId = firstEx.Id,
            Status = ExerciseStatus.Solved,
            PersonalNote = "Svarbi taisyklė!"
        });

        Assert.Equal(ExerciseStatus.Solved, prog.Status);
        Assert.Equal("Svarbi taisyklė!", prog.PersonalNote);

        var secondEx = exerciseService.GetAllExercises().Skip(1).First();
        var attempt = await progressService.RecordExamAttemptAsync(student.Id, new SubmitExamAttemptRequest
        {
            Title = "Bandomasis egzaminas",
            Mode = "custom",
            DurationSeconds = 900,
            Topics = [firstEx.Topic],
            Items =
            [
                new SubmitExamItemDto { ExerciseId = firstEx.Id, Outcome = 2 },
                new SubmitExamItemDto { ExerciseId = secondEx.Id, Outcome = 0 }
            ]
        });

        Assert.Equal(2, attempt.TotalQuestions);
        Assert.Equal(1, attempt.SolvedCorrectly);
    }

    [Fact]
    public void ExamPdfService_GeneratesValidFullSessionPdf()
    {
        var root = FindRepoRoot();
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["VbeConfig:BasePath"] = root })
            .Build();

        var exerciseService = new ExerciseService(config);
        var sessions = exerciseService.GetAvailableExamSessions();
        Assert.NotEmpty(sessions);

        var sessionExercises = exerciseService.GetExercisesByExamSession("2024", "pagr");
        Assert.True(sessionExercises.Count >= 15);
        Assert.All(sessionExercises, ex => Assert.Equal("Home/ExamPdf?year=2024&session=pagr", ex.OfficialPdfPath));

        using var cache = new Microsoft.Extensions.Caching.Memory.MemoryCache(new Microsoft.Extensions.Caching.Memory.MemoryCacheOptions());
        var pdfService = new ExamPdfService(exerciseService, cache, config);
        var pdfBytes = pdfService.BuildSessionExamPdf("2024", "pagr");

        Assert.NotNull(pdfBytes);
        Assert.True(pdfBytes.Length > 10_000);
        var header = System.Text.Encoding.ASCII.GetString(pdfBytes, 0, 8);
        Assert.Equal("%PDF-1.4", header);
    }

    [Fact]
    public void SortImagesNaturally_PlacesBaseImageBeforeNumberedParts()
    {
        var input = new[]
        {
            "surusiuotos/Funkcijos bendrai/2022_pak_24(2).png",
            "surusiuotos/Funkcijos bendrai/2022_pak_24.png"
        };

        var sorted = ExerciseService.SortImagesNaturally(input);
        Assert.Equal("surusiuotos/Funkcijos bendrai/2022_pak_24.png", sorted[0]);
        Assert.Equal("surusiuotos/Funkcijos bendrai/2022_pak_24(2).png", sorted[1]);

        var answerInput = new[]
        {
            "atsakymai/pagr/Screenshotais/2025/2025_pagr_20 (3).png",
            "atsakymai/pagr/Screenshotais/2025/2025_pagr_20 (2).png",
            "atsakymai/pagr/Screenshotais/2025/2025_pagr_20.png"
        };

        var sortedAnswers = ExerciseService.SortImagesNaturally(answerInput);
        Assert.Equal("atsakymai/pagr/Screenshotais/2025/2025_pagr_20.png", sortedAnswers[0]);
        Assert.Equal("atsakymai/pagr/Screenshotais/2025/2025_pagr_20 (2).png", sortedAnswers[1]);
        Assert.Equal("atsakymai/pagr/Screenshotais/2025/2025_pagr_20 (3).png", sortedAnswers[2]);
    }
}

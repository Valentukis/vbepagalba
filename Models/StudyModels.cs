using System.ComponentModel.DataAnnotations;

namespace VbeSort.Models;

public enum ExerciseStatus
{
    Unseen = 0,
    Solved = 1,
    Hard = 2,
    NeedsReview = 3
}

/// <summary>
/// Mokinio pažanga konkrečiai užduočiai: būsena ir asmeninė pastaba.
/// </summary>
public class ExerciseProgress
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(64)]
    public string ExerciseId { get; set; } = string.Empty;

    [MaxLength(80)]
    public string TopicName { get; set; } = string.Empty;

    public ExerciseStatus Status { get; set; } = ExerciseStatus.Unseen;

    public bool IsBookmarked { get; set; }

    [MaxLength(2000)]
    public string PersonalNote { get; set; } = string.Empty;

    public int AttemptCount { get; set; }
    public int CorrectCount { get; set; }
    public int TotalTimeSeconds { get; set; }

    public DateTime? LastAttemptAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}

/// <summary>
/// Bandomojo egzamino bandymo įrašas duomenų bazėje.
/// </summary>
public class ExamAttempt
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [MaxLength(120)]
    public string Title { get; set; } = "Bandomasis egzaminas";

    [MaxLength(40)]
    public string Mode { get; set; } = "custom";

    public int TotalQuestions { get; set; }
    public int SolvedCorrectly { get; set; }
    public int PartiallySolved { get; set; }
    public int TotalPointsEarned { get; set; }
    public int MaxPossiblePoints { get; set; }
    public int DurationSeconds { get; set; }

    [MaxLength(1000)]
    public string TopicsCsv { get; set; } = string.Empty;

    public string DetailsJson { get; set; } = "[]";

    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;

    public double ScorePercentage =>
        TotalQuestions > 0
            ? Math.Round((double)SolvedCorrectly / TotalQuestions * 100.0, 1)
            : 0.0;
}

public sealed class ExamAttemptItemDetail
{
    public string ExerciseId { get; set; } = string.Empty;
    public string Topic { get; set; } = string.Empty;
    public string Year { get; set; } = string.Empty;
    public string Session { get; set; } = string.Empty;
    public int Number { get; set; }
    public intOutcome Outcome { get; set; }
    public int PointsEarned { get; set; }
    public int MaxPoints { get; set; }
}

public enum intOutcome
{
    Incorrect = 0,
    Partial = 1,
    Correct = 2
}

public sealed record TopicMasteryDto(
    string TopicName,
    int TotalExercises,
    int SolvedCount,
    int ReviewDueCount
);

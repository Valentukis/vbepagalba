using System.ComponentModel.DataAnnotations;

namespace VbeSort.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Prisijungimo vardas privalomas")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slaptažodis privalomas")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public bool RememberMe { get; set; } = true;
}

public class RegisterViewModel
{
    [Required(ErrorMessage = "Prisijungimo vardas privalomas")]
    [StringLength(40, MinimumLength = 3, ErrorMessage = "Vardas turi būti 3–40 simbolių")]
    [RegularExpression(@"^[a-zA-Z0-9_.\-]+$", ErrorMessage = "Leidžiamos tik raidės, skaičiai, ., - ir _")]
    public string Username { get; set; } = string.Empty;

    [StringLength(80, ErrorMessage = "Iki 80 simbolių")]
    public string DisplayName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slaptažodis privalomas")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Slaptažodis turi būti bent 4 simbolių")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "Pakartokite slaptažodį")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Slaptažodžiai nesutampa")]
    public string ConfirmPassword { get; set; } = string.Empty;
}

public class CreateUserViewModel
{
    [Required(ErrorMessage = "Vardas privalomas")]
    [StringLength(50, MinimumLength = 3, ErrorMessage = "3–50 simbolių")]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "Slaptažodis privalomas")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Bent 4 simboliai")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;

    public string Role { get; set; } = "Student";

    [StringLength(80)]
    public string DisplayName { get; set; } = string.Empty;
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Įveskite dabartinį slaptažodį")]
    [DataType(DataType.Password)]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Įveskite naują slaptažodį")]
    [StringLength(100, MinimumLength = 4, ErrorMessage = "Bent 4 simboliai")]
    [DataType(DataType.Password)]
    public string NewPassword { get; set; } = string.Empty;

    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "Slaptažodžiai nesutampa")]
    public string ConfirmNewPassword { get; set; } = string.Empty;
}

public class HomeIndexViewModel
{
    public List<Topic> Topics { get; set; } = new();
    public User? CurrentUser { get; set; }
    public Dictionary<string, TopicMasteryDto> TopicMastery { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public int TotalSolvedCount { get; set; }
    public int TotalExercisesCount { get; set; }
    public int DueForReviewCount { get; set; }
}

public class TopicPageViewModel
{
    public Topic Topic { get; set; } = new();
    public TopicMasteryDto? Mastery { get; set; }
    public Dictionary<string, ExerciseProgress> ProgressByExerciseId { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public sealed record ExamSessionOptionDto(
    string Key,
    string Year,
    string Session,
    string Label,
    int ExerciseCount
);

public class MockExamSetupViewModel
{
    public List<string> AvailableTopics { get; set; } = new();
    public List<Topic> TopicDetails { get; set; } = new();
    public List<ExamSessionOptionDto> AvailableExamSessions { get; set; } = new();
    public string SelectedExamSession { get; set; } = string.Empty;
    public Dictionary<string, TopicMasteryDto> MasteryByTopic { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> SelectedTopics { get; set; } = new();
    public int QuestionCount { get; set; } = 10;
    public int TimeLimitMinutes { get; set; } = 45;
    /// <summary>"full_session", "custom", "unseen_only", "review_due".</summary>
    public string ExamMode { get; set; } = "full_session";
    public int ReviewDueTotalCount { get; set; }
    public List<ExamAttempt> RecentAttempts { get; set; } = new();
}

public class MockExamViewModel
{
    public string Title { get; set; } = "Bandomasis egzaminas";
    public string ExamMode { get; set; } = "custom";
    public string? ExamYear { get; set; }
    public string? ExamSession { get; set; }
    public int TimeLimitMinutes { get; set; } = 45;
    public List<string> SelectedTopics { get; set; } = new();
    public List<Exercise> Exercises { get; set; } = new();
    public Dictionary<string, ExerciseProgress> ExistingProgress { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

public class SubmitExamAttemptRequest
{
    public string Title { get; set; } = "Bandomasis egzaminas";
    public string Mode { get; set; } = "custom";
    public int DurationSeconds { get; set; }
    public List<string> Topics { get; set; } = new();
    public List<SubmitExamItemDto> Items { get; set; } = new();
}

public class SubmitExamItemDto
{
    public string ExerciseId { get; set; } = string.Empty;
    public int Outcome { get; set; }
}

public class RecordProgressRequest
{
    [Required]
    public string ExerciseId { get; set; } = string.Empty;
    public ExerciseStatus Status { get; set; } = ExerciseStatus.Solved;
    public string? PersonalNote { get; set; }
}

public class ProfileDashboardViewModel
{
    public User User { get; set; } = new();
    public int TotalExercisesInSystem { get; set; }
    public int SolvedUniqueCount { get; set; }
    public int DueReviewCount { get; set; }
    public List<TopicMasteryDto> TopicMasteries { get; set; } = new();
    public List<ExamAttempt> ExamHistory { get; set; } = new();
    public List<(Exercise Exercise, ExerciseProgress Progress)> NotedExercises { get; set; } = new();
}

public class StatisticsPageViewModel
{
    public List<Topic> Topics { get; set; } = new();
    public int TotalExercises { get; set; }
    public Dictionary<string, int> ExercisesByYear { get; set; } = new();
}

public class PracticeQueueViewModel
{
    public string Mode { get; set; } = "due";
    public List<(Exercise Exercise, ExerciseProgress? Progress, string ReasonBadge)> Queue { get; set; } = new();
    public int DueCount { get; set; }
    public int UnseenCount { get; set; }
    public int SolvedCount { get; set; }
}

public class AdminDashboardViewModel
{
    public List<User> Users { get; set; } = new();
    public Dictionary<int, int> SolvedByUserId { get; set; } = new();
}

public class ErrorViewModel
{
    public string? RequestId { get; set; }
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
}

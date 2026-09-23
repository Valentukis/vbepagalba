using System.ComponentModel.DataAnnotations;

namespace VbeSort.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Prisijungimo vardas privalomas")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Slaptažodis privalomas")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;
    }

    public class CreateUserViewModel
    {
        [Required(ErrorMessage = "Vardas privalomas")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "3-50 simbolių")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Slaptažodis privalomas")]
        [StringLength(100, MinimumLength = 4, ErrorMessage = "Bent 4 simboliai")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        public string Role { get; set; } = "Student";
    }

    public class MockExamSetupViewModel
    {
        public List<string> AvailableTopics { get; set; } = new();
        public List<string> SelectedTopics { get; set; } = new();
        public int QuestionCount { get; set; } = 10;
    }

    public class MockExamViewModel
    {
        public List<Exercise> Exercises { get; set; } = new();
    }

    public class ErrorViewModel
    {
        public string? RequestId { get; set; }
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}

using System.ComponentModel.DataAnnotations;

namespace VbeSort.Models;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Role { get; set; } = "Student";

    [MaxLength(80)]
    public string DisplayName { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string EffectiveDisplayName =>
        string.IsNullOrWhiteSpace(DisplayName) ? Username : DisplayName;
}

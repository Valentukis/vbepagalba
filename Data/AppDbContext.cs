using Microsoft.EntityFrameworkCore;
using VbeSort.Models;

namespace VbeSort.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<ExerciseProgress> ExerciseProgresses => Set<ExerciseProgress>();
    public DbSet<ExamAttempt> ExamAttempts => Set<ExamAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.ToTable("Users");
            entity.HasIndex(u => u.Username).IsUnique();
        });

        modelBuilder.Entity<ExerciseProgress>(entity =>
        {
            entity.ToTable("ExerciseProgresses");
            entity.HasIndex(p => new { p.UserId, p.ExerciseId }).IsUnique();
            entity.HasIndex(p => p.UserId);
        });

        modelBuilder.Entity<ExamAttempt>(entity =>
        {
            entity.ToTable("ExamAttempts");
            entity.HasIndex(a => a.UserId);
            entity.HasIndex(a => a.CompletedAtUtc);
        });
    }
}
